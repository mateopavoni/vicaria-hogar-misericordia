import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { todayLocalIso } from '../../../shared/utils/date.util';
import { MedicationService } from './medication.service';

describe('MedicationService', () => {
  let service: MedicationService;
  let httpMock: HttpTestingController;
  const today = todayLocalIso();
  const schemeDto = (overrides = {}) => ({
    id: 's1',
    personId: 'p1',
    medicationId: 'm1',
    medicationName: 'Aspirina',
    dose: '1 comprimido',
    time: '08:00',
    loadedAt: '2026-10-19T10:00:00',
    loadedByUserId: 'u1',
    loadedByName: 'Ref Test',
    reviewedAt: null,
    reviewedByUserId: null,
    reviewedByName: null,
    isActive: true,
    pendingStock: false,
    stockAlert: null,
    ...overrides,
  });

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(MedicationService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getCatalog() lee el catálogo con stock de farmacia', () => {
    let catalog: unknown;
    service.getCatalog().subscribe((r) => (catalog = r));
    httpMock.expectOne('/api/medication-schemes/catalog').flush([{ id: 'm1', name: 'Aspirina', pharmacyStock: 4 }]);
    expect(catalog).toEqual([{ id: 'm1', name: 'Aspirina', pharmacyStock: 4 }]);
  });

  it('getEvaluationStatus() anticipa vigencia según residente y vencimiento a 6 meses', () => {
    const results: boolean[] = [];
    service.getEvaluationStatus('p1', true).subscribe((s) => results.push(s.valid));
    service.getEvaluationStatus('p1', false).subscribe((s) => results.push(s.valid));
    service.getEvaluationStatus('p1', true, '2020-01-01').subscribe((s) => results.push(s.valid));
    expect(results).toEqual([true, false, false]);
  });

  it('getSchemes() renombra isActive a active', () => {
    let schemes: { active: boolean }[] = [];
    service.getSchemes('p1').subscribe((r) => (schemes = r));
    httpMock.expectOne('/api/persons/p1/medication-schemes').flush([schemeDto(), schemeDto({ id: 's2', isActive: false })]);
    expect(schemes.map((s) => s.active)).toEqual([true, false]);
  });

  it('create() manda dosis y hora a la persona y valida antes de llamar', () => {
    const base = { personId: 'p1', medicationId: 'm1', dose: ' 1 comprimido ', time: '08:00' };
    let created: { pendingStock: boolean } | undefined;
    service.create(base).subscribe((r) => (created = r));
    const post = httpMock.expectOne('/api/persons/p1/medication-schemes');
    expect(post.request.body).toEqual({ medicationId: 'm1', dose: '1 comprimido', time: '08:00' });
    post.flush(schemeDto({ pendingStock: true }), { status: 200, statusText: 'OK' });
    expect(created?.pendingStock).toBe(true);

    const messages: string[] = [];
    service.create({ ...base, dose: '  ' }).subscribe({ error: (e) => messages.push(e.error.message) });
    service.create({ ...base, time: '25:99' }).subscribe({ error: (e) => messages.push(e.error.message) });
    expect(messages).toEqual(['La dosis es obligatoria.', 'La hora no es válida.']);
  });

  it('markReviewed() hace PATCH y devuelve el esquema releído', () => {
    let scheme: { reviewedAt: string | null } | undefined;
    service.markReviewed('p1', 's1').subscribe((r) => (scheme = r));
    httpMock.expectOne('/api/persons/p1/medication-schemes/s1/review').flush(null, { status: 204, statusText: 'No Content' });
    httpMock.expectOne('/api/persons/p1/medication-schemes').flush([schemeDto({ reviewedAt: '2026-10-19T12:00:00' })]);
    expect(scheme?.reviewedAt).toBe('2026-10-19T12:00:00');
  });

  it('deactivate() pasa el esquema a histórico', () => {
    let scheme: { active: boolean } | undefined;
    service.deactivate('p1', 's1').subscribe((r) => (scheme = r));
    httpMock.expectOne('/api/persons/p1/medication-schemes/s1/deactivate').flush(null, { status: 204, statusText: 'No Content' });
    httpMock.expectOne('/api/persons/p1/medication-schemes').flush([schemeDto({ isActive: false })]);
    expect(scheme?.active).toBe(false);
  });

  it('getAgenda() agrupa por hora y traduce el estado de cada toma', () => {
    let agenda: { slots: { time: string; items: { intakeId: string; time: string; administration: { status: string } | null }[] }[] } | undefined;
    service.getAgenda(today).subscribe((r) => (agenda = r));
    const req = httpMock.expectOne((r) => r.url === '/api/medicamentos/agenda');
    expect(req.request.params.get('fecha')).toBe(today);
    const intake = (id: string, status: number) => ({
      id,
      personId: 'p1',
      personFullName: 'Ana',
      medicationName: 'Aspirina',
      dose: '1',
      status,
      observation: null,
      administeredAt: status === 1 ? null : `${today}T09:00:00`,
      administeredByUserId: status === 1 ? null : 'u1',
    });
    req.flush({ date: today, groups: [{ time: '08:00', intakes: [intake('t1', 1), intake('t2', 2), intake('t3', 3)] }] });

    const items = agenda!.slots[0].items;
    expect(items.map((i) => i.intakeId)).toEqual(['t1', 't2', 't3']);
    expect(items.every((i) => i.time === '08:00')).toBe(true);
    expect(items.map((i) => i.administration?.status ?? null)).toEqual([null, 'administered', 'not-administered']);
  });

  it('recordAdministration() manda el estado numérico a la toma, solo del día actual', () => {
    service.recordAdministration({ intakeId: 't1', date: today, status: 'administered', observation: ' ok ' }).subscribe();
    const patch = httpMock.expectOne('/api/medicamentos/agenda/t1');
    expect(patch.request.body).toEqual({ status: 2, observation: 'ok' });
    patch.flush(null, { status: 204, statusText: 'No Content' });

    service.recordAdministration({ intakeId: 't1', date: today, status: 'not-administered', observation: null }).subscribe();
    expect(httpMock.expectOne('/api/medicamentos/agenda/t1').request.body.status).toBe(3);

    const messages: string[] = [];
    service
      .recordAdministration({ intakeId: 't1', date: '2020-01-01', status: 'administered', observation: null })
      .subscribe({ error: (e) => messages.push(e.error.message) });
    service
      .recordAdministration({ intakeId: 't1', date: today, status: 'administered', observation: 'x'.repeat(501) })
      .subscribe({ error: (e) => messages.push(e.error.message) });
    expect(messages[0]).toContain('día actual');
    expect(messages[1]).toContain('500');
  });

  it('getPersonHistory() devuelve lo que manda el backend', () => {
    let history: unknown;
    service.getPersonHistory('p1').subscribe((r) => (history = r));
    httpMock.expectOne('/api/persons/p1/medication-history').flush([{ date: today, status: 'administered' }]);
    expect(history).toEqual([{ date: today, status: 'administered' }]);
  });
});
