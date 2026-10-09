import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { CalendarEventsService } from './calendar-events.service';
import { CalendarEvent } from '../interfaces/calendar-event.interface';

// el servicio traduce el contrato real del backend (api/general-calendar-events y
// api/personal-calendar-events: paginado, TimeSpan y flags de día) a CalendarEvent
describe('CalendarEventsService', () => {
  let service: CalendarEventsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(CalendarEventsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  const occ = (n: number, extra: object = {}) => ({
    eventId: `e${n}`,
    date: '2026-10-12T00:00:00',
    startTime: '09:30:00',
    endTime: '10:00:00',
    title: `Evento ${n}`,
    description: null,
    authorUserId: 'u1',
    authorName: 'Ana Pérez',
    isPreloaded: false,
    ...extra,
  });

  it('getByRange(general) pide todas las páginas y arma start/end', () => {
    let result: CalendarEvent[] = [];
    service.getByRange('2026-10-12', '2026-10-18', 'general').subscribe((r) => (result = r));

    httpMock
      .expectOne((r) => r.url === '/api/general-calendar-events' && r.params.get('page') === '1' && r.params.get('from') === '2026-10-12')
      .flush({ items: [occ(1)], total: 11, totalPages: 2 });
    httpMock
      .expectOne((r) => r.url === '/api/general-calendar-events' && r.params.get('page') === '2')
      .flush({ items: [occ(2)], total: 11, totalPages: 2 });

    expect(result.map((e) => e.id)).toEqual(['e1', 'e2']);
    expect(result[0].start).toBe('2026-10-12T09:30:00');
    expect(result[0].end).toBe('2026-10-12T10:00:00');
    expect(result[0].authorName).toBe('Ana Pérez');
    expect(result[0].scope).toBe('general');
  });

  it('las plantillas precargadas se marcan como no editables y con autor Sistema', () => {
    let result: CalendarEvent[] = [];
    service.getByRange('2026-10-12', '2026-10-12', 'general').subscribe((r) => (result = r));
    httpMock.expectOne((r) => r.url === '/api/general-calendar-events').flush({
      items: [occ(1, { isPreloaded: true, authorName: null, authorUserId: null, startTime: null, endTime: null })],
      total: 1,
      totalPages: 1,
    });

    expect(result[0].isRecurring).toBe(true);
    expect(result[0].authorName).toBe('Sistema');
    expect(result[0].start).toBe('2026-10-12T00:00:00');
    expect(result[0].end).toBe('2026-10-12T23:59:00');
  });

  it('getByRange(personal) usa el endpoint personal', () => {
    service.getByRange('2026-10-12', '2026-10-18', 'personal').subscribe();
    httpMock.expectOne((r) => r.url === '/api/personal-calendar-events').flush({ items: [], total: 0, totalPages: 0 });
  });

  it('create() semanal manda date/startTime/endTime y el flag del día de la semana', () => {
    // 2026-10-12 es lunes => flag 1
    service
      .create({ title: 'T', start: '2026-10-12T11:00:00', end: '2026-10-12T12:00:00', recurrence: 'weekly', scope: 'general' })
      .subscribe();
    const req = httpMock.expectOne('/api/general-calendar-events');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      title: 'T',
      description: null,
      date: '2026-10-12',
      startTime: '11:00:00',
      endTime: '12:00:00',
      recurrenceDays: 1,
    });
    req.flush({ id: 'new' });
  });

  it('create() diario marca los 7 días y el personal va al endpoint personal', () => {
    service
      .create({ title: 'T', start: '2026-10-12T11:00:00', end: '2026-10-12T12:00:00', recurrence: 'daily', scope: 'personal' })
      .subscribe();
    const req = httpMock.expectOne('/api/personal-calendar-events');
    expect(req.request.body.recurrenceDays).toBe(127);
    req.flush({ id: 'new' });
  });

  const sampleDto = (scope: 'general' | 'personal') => ({
    title: 'Reunión',
    description: 'Detalle',
    start: '2026-10-12T09:00:00',
    end: '2026-10-12T10:30:00',
    recurrence: 'weekly' as const,
    scope,
  });

  it('update() hace PUT al endpoint del ámbito con el cuerpo del backend y responde 204', () => {
    let id = '';
    service.update('e1', sampleDto('general')).subscribe((e) => (id = e.id));
    const req = httpMock.expectOne('/api/general-calendar-events/e1');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toMatchObject({
      title: 'Reunión',
      date: '2026-10-12',
      startTime: '09:00:00',
      endTime: '10:30:00',
      recurrenceDays: 1,
    });
    req.flush(null);
    expect(id).toBe('e1');
  });

  it('update() de un evento personal va al endpoint personal', () => {
    service.update('e2', sampleDto('personal')).subscribe();
    const req = httpMock.expectOne('/api/personal-calendar-events/e2');
    expect(req.request.method).toBe('PUT');
    req.flush(null);
  });

  it('delete() usa el endpoint del ámbito (general por defecto)', () => {
    service.delete('e1').subscribe();
    httpMock.expectOne('/api/general-calendar-events/e1').flush(null);
    service.delete('e2', 'personal').subscribe();
    const req = httpMock.expectOne('/api/personal-calendar-events/e2');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('convertToGeneral() hace PUT /publish y devuelve el id del evento general', () => {
    let newId = '';
    service.convertToGeneral('e3').subscribe((r) => (newId = r.id));
    const req = httpMock.expectOne('/api/personal-calendar-events/e3/publish');
    expect(req.request.method).toBe('PUT');
    req.flush({ id: 'g9' });
    expect(newId).toBe('g9');
  });
});
