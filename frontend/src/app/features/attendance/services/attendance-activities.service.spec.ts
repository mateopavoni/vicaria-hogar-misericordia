import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { CalendarEventsService } from '../../calendar/services/calendar-events.service';
import { CalendarEvent } from '../../calendar/interfaces/calendar-event.interface';
import { AttendanceActivitiesService } from './attendance-activities.service';

describe('AttendanceActivitiesService', () => {
  let service: AttendanceActivitiesService;
  let calendarEvents: CalendarEvent[];

  const event = (title: string, extra: Partial<CalendarEvent> = {}): CalendarEvent => ({
    id: title,
    title,
    start: '2026-10-05T08:00:00',
    end: '2026-10-05T09:00:00',
    authorName: '',
    authorId: '',
    scope: 'general',
    ...extra,
  });

  beforeEach(() => {
    calendarEvents = [];
    TestBed.configureTestingModule({
      providers: [{ provide: CalendarEventsService, useValue: { getByRange: () => of(calendarEvents) } }],
    });
    service = TestBed.inject(AttendanceActivitiesService);
  });

  it('arranca con las dos actividades fijas, activas', () => {
    expect(service.all().map((a) => a.id)).toEqual(['hogar-de-dia', 'casa-convivencia']);
    expect(service.active().length).toBe(2);
  });

  it('no deja desactivar las actividades fijas', () => {
    const result = service.setActive('casa-convivencia', false);
    expect(result.ok).toBeFalse();
    expect(service.active().length).toBe(2);
  });

  it('crea una actividad propia del Hogar de día y la puede desactivar y reactivar', () => {
    const created = service.create('  Taller   de costura ');
    expect(created.ok).toBeTrue();
    const id = created.ok ? created.activity.id : '';
    expect(service.name(id)).toBe('Taller de costura');

    service.setActive(id, false);
    expect(service.active().some((a) => a.id === id)).toBeFalse();
    service.setActive(id, true);
    expect(service.active().some((a) => a.id === id)).toBeTrue();
  });

  it('rechaza nombres cortos, largos o repetidos', () => {
    expect(service.create('ab').ok).toBeFalse();
    expect(service.create('x'.repeat(61)).ok).toBeFalse();
    service.create('Taller de costura');
    expect(service.create('taller DE costura').ok).toBeFalse();
    expect(service.create('hogar de día').ok).toBeFalse();
  });

  it('sincroniza las actividades recurrentes del calendario sin duplicarlas', (done) => {
    calendarEvents = [
      event('Desayuno', { isRecurring: true }),
      event('Desayuno', { isRecurring: true }), // misma serie, otro día
      event('Taller de música', { recurrence: 'weekly' }),
      event('Reunión de equipo', { recurrence: 'none' }), // evento puntual: no es una actividad
    ];

    service.syncFromCalendar().subscribe((added) => {
      expect(added).toBe(2);
      expect(service.all().filter((a) => a.source === 'calendar').map((a) => a.name)).toEqual(['Desayuno', 'Taller de música']);

      service.syncFromCalendar().subscribe((again) => {
        expect(again).toBe(0);
        done();
      });
    });
  });
});
