import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of } from 'rxjs';
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
    expect(result.ok).toBe(false);
    expect(service.active().length).toBe(2);
  });

  it('crea una actividad propia del Hogar de día y la puede desactivar y reactivar', () => {
    const created = service.create('  Taller   de costura ');
    expect(created.ok).toBe(true);
    const id = created.ok ? created.activity.id : '';
    expect(service.name(id)).toBe('Taller de costura');

    service.setActive(id, false);
    expect(service.active().some((a) => a.id === id)).toBe(false);
    service.setActive(id, true);
    expect(service.active().some((a) => a.id === id)).toBe(true);
  });

  it('rechaza nombres cortos, largos o repetidos', () => {
    expect(service.create('ab').ok).toBe(false);
    expect(service.create('x'.repeat(61)).ok).toBe(false);
    service.create('Taller de costura');
    expect(service.create('taller DE costura').ok).toBe(false);
    expect(service.create('hogar de día').ok).toBe(false);
  });

  it('sincroniza las actividades recurrentes del calendario sin duplicarlas', async () => {
    calendarEvents = [
      event('Desayuno', { isRecurring: true }),
      event('Desayuno', { isRecurring: true }), // misma serie, otro día
      event('Taller de música', { recurrence: 'weekly' }),
      event('Reunión de equipo', { recurrence: 'none' }), // evento puntual: no es una actividad
    ];

    const added = await firstValueFrom(service.syncFromCalendar());
    expect(added).toBe(2);
    expect(service.all().filter((a) => a.source === 'calendar').map((a) => a.name)).toEqual(['Desayuno', 'Taller de música']);

    const again = await firstValueFrom(service.syncFromCalendar());
    expect(again).toBe(0);
  });
});
