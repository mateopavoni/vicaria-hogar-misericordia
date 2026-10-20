import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { todayLocalIso } from '../../../shared/utils/date.util';
import { AttendanceService } from './attendance.service';

describe('AttendanceService', () => {
  let service: AttendanceService;
  let httpMock: HttpTestingController;
  const today = todayLocalIso();
  const person = (id: string) => ({ personId: id, fullName: `Persona ${id}`, dni: null });
  const request = (overrides = {}) => ({
    activity: 'hogar-de-dia',
    date: today,
    persons: [person('p1')],
    anonymousCount: 0,
    justification: null as string | null,
    ...overrides,
  });
  const day = (persons: { id: string; anonymous?: number }[]) => ({
    anonymousCount: persons.reduce((sum, p) => sum + (p.anonymous ?? 0), 0),
    persons: persons.map((p) => ({
      attendanceId: `a-${p.id}`,
      personId: p.id,
      fullName: `Persona ${p.id}`,
      dni: null,
      anonymousCount: p.anonymous ?? 0,
    })),
  });
  const dayGet = () => httpMock.expectOne((r) => r.method === 'GET' && r.url === '/api/attendances');

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(AttendanceService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('get() lee el día con el código numérico de la actividad', () => {
    let result: unknown;
    service.get('casa-convivencia', today).subscribe((r) => (result = r));
    const req = dayGet();
    expect(req.request.params.get('activity')).toBe('1');
    expect(req.request.params.get('date')).toBe(today);
    req.flush(day([{ id: 'p1', anonymous: 2 }]));
    expect(result).toEqual({
      activity: 'casa-convivencia',
      date: today,
      persons: [person('p1')],
      anonymousCount: 2,
      justification: null,
    });
  });

  it('get() devuelve null sin llamar a la API para una actividad configurable', () => {
    let result: unknown = 'sin-respuesta';
    service.get('guid-de-actividad', today).subscribe((r) => (result = r));
    expect(result).toBeNull();
  });

  it('save() registra una persona por llamada y relee el día', () => {
    let saved: { persons: unknown[] } | undefined;
    service.save(request({ persons: [person('p1'), person('p2')], anonymousCount: 3 })).subscribe((r) => (saved = r));
    dayGet().flush(day([]));

    const posts = httpMock.match((r) => r.method === 'POST' && r.url === '/api/attendances');
    expect(posts.map((p) => p.request.body)).toEqual([
      { personId: 'p1', activity: 0, date: today, anonymousCount: 3 },
      { personId: 'p2', activity: 0, date: today, anonymousCount: 0 },
    ]);
    posts.forEach((p) => p.flush(null));
    dayGet().flush(day([{ id: 'p1', anonymous: 3 }, { id: 'p2' }]));
    expect(saved?.persons.length).toBe(2);
  });

  it('save() ajusta el conteo sin ficha sobre la fila existente', () => {
    service.save(request({ anonymousCount: 5 })).subscribe();
    dayGet().flush(day([{ id: 'p1', anonymous: 2 }]));
    const put = httpMock.expectOne('/api/attendances/a-p1');
    expect(put.request.body).toEqual({ anonymousCount: 5, justification: null });
    put.flush(null);
    dayGet().flush(day([{ id: 'p1', anonymous: 5 }]));
  });

  it('save() de un día cerrado exige justificación y la manda en el PUT', () => {
    const closed = '2020-01-01';
    let message = '';
    service.save(request({ date: closed })).subscribe({ error: (e) => (message = e.error.message) });
    expect(message).toContain('justificación');

    service.save(request({ date: closed, justification: ' error de carga ' })).subscribe();
    dayGet().flush(day([{ id: 'p1' }]));
    const put = httpMock.expectOne('/api/attendances/a-p1');
    expect(put.request.body).toEqual({ anonymousCount: 0, justification: 'error de carga' });
    put.flush(null);
    dayGet().flush(day([{ id: 'p1' }]));
  });

  it('save() rechaza repetidos, conteo inválido, actividades configurables y quitar personas', () => {
    const messages: string[] = [];
    const collect = (req: ReturnType<typeof request>) =>
      service.save(req).subscribe({ error: (e) => messages.push(e.error.message) });
    collect(request({ persons: [person('p1'), person('p1')] }));
    collect(request({ anonymousCount: -1 }));
    collect(request({ activity: 'guid-de-actividad' }));
    expect(messages[0]).toContain('repetidas');
    expect(messages[1]).toContain('sin ficha');
    expect(messages[2]).toContain('Hogar de día');

    collect(request({ persons: [person('p2')] }));
    dayGet().flush(day([{ id: 'p1' }]));
    expect(messages[3]).toContain('No se puede quitar');
  });

  it('getPersonHistory() aplana los grupos por actividad, de más reciente a más antiguo', () => {
    let entries: unknown;
    service.getPersonHistory('p1').subscribe((r) => (entries = r));
    httpMock.expectOne('/api/personas/p1/asistencias').flush({
      personId: 'p1',
      indicators: {},
      groups: [
        { activity: 0, activityName: 'Hogar de día', attendances: [{ id: 'a', date: '2026-10-10T00:00:00', anonymousCount: 0 }] },
        { activity: 1, activityName: 'Casa', attendances: [{ id: 'b', date: '2026-10-12T00:00:00', anonymousCount: 0 }] },
      ],
    });
    expect(entries).toEqual([
      { activity: 'casa-convivencia', date: '2026-10-12' },
      { activity: 'hogar-de-dia', date: '2026-10-10' },
    ]);
  });

  it('getStatistics() manda el rango y el nombre de la actividad y arma el resumen', () => {
    let overview: { totalPresences: number; weeklyAverage: number; byActivity: { label: string }[] } | undefined;
    service.getStatistics('2026-10-01', '2026-10-19', 'hogar-de-dia').subscribe((r) => (overview = r));
    const req = httpMock.expectOne((r) => r.url === '/api/attendances/statistics');
    expect(req.request.params.get('activity')).toBe('DayCenter');
    expect(req.request.params.get('from')).toBe('2026-10-01');
    req.flush({
      totalRecords: 10,
      totalAnonymous: 4,
      averagePerDay: 0.74,
      totalsByActivity: [{ activity: 0, activityName: 'Hogar de día', count: 10 }],
      previousPeriodTotal: 7,
      comparisonPercentage: 100,
      weeklyAverages: [{ averagePerDay: 2 }, { averagePerDay: 3 }],
      monthlyAverages: [{ averagePerDay: 1 }],
    });
    expect(overview?.totalPresences).toBe(14);
    expect(overview?.weeklyAverage).toBe(2.5);
    expect(overview?.byActivity[0].label).toBe('Hogar de día');
  });

  it('getInactive() arma el nombre completo y los días', () => {
    let people: unknown;
    service.getInactive(14).subscribe((r) => (people = r));
    httpMock.expectOne((r) => r.url === '/api/attendances/inactive').flush([
      { personId: 'p1', firstName: 'Ana', lastName: 'Pérez', lastAttendanceDate: '2026-09-01T00:00:00', daysSinceLastAttendance: 48 },
    ]);
    expect(people).toEqual([{ personId: 'p1', fullName: 'Ana Pérez', lastAttendance: '2026-09-01', daysSince: 48 }]);
  });
});
