import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { errorOf } from '../../../shared/testing/error-of';
import { todayLocalIso } from '../../../shared/utils/date.util';
import { PsychiatricEvaluationsService } from './psychiatric-evaluations.service';

describe('PsychiatricEvaluationsService', () => {
  let service: PsychiatricEvaluationsService;
  let httpMock: HttpTestingController;
  const actor = { id: 'u1', name: 'Belén Test' };
  const today = todayLocalIso();
  const base = '/api/persons/p1/psychiatric-evaluations';
  const request = (overrides = {}) => ({ personId: 'p1', date: today, professional: 'Dra. Gómez', diagnosis: 'Diagnóstico de prueba', ...overrides });
  const status = (overrides = {}) => ({
    hasEvaluation: true,
    evaluationId: 'e2',
    date: '2026-09-01T00:00:00',
    professional: 'Dra. Gómez',
    diagnosis: 'Actual',
    ...overrides,
  });
  const history = [{ id: 'e1', personId: 'p1', date: '2026-01-10T00:00:00', professional: 'Dr. Paz', diagnosis: 'Anterior', isValid: false, registeredByName: 'Ana' }];

  const flushLoad = (statusDto = status(), historyDto = history) => {
    httpMock.expectOne(`${base}/status`).flush(statusDto);
    httpMock.expectOne(`${base}/history`).flush(historyDto);
  };

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(PsychiatricEvaluationsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('load() deja una sola vigente y el historial como no vigente', () => {
    service.load('p1').subscribe();
    flushLoad();
    expect(service.currentFor('p1')?.id).toBe('e2');
    expect(service.currentFor('p1')?.date).toBe('2026-09-01');
    expect(service.forPerson('p1').map((e) => [e.id, e.vigente])).toEqual([['e2', true], ['e1', false]]);
  });

  it('sin evaluación cargada no hay vigente', () => {
    service.load('p1').subscribe();
    flushLoad(status({ hasEvaluation: false, evaluationId: null, date: null }), []);
    expect(service.currentFor('p1')).toBeNull();
  });

  it('no toca las evaluaciones de otras personas', () => {
    service.load('p1').subscribe();
    flushLoad();
    service.load('p2').subscribe();
    httpMock.expectOne('/api/persons/p2/psychiatric-evaluations/status').flush(status({ evaluationId: 'x1' }));
    httpMock.expectOne('/api/persons/p2/psychiatric-evaluations/history').flush([]);
    expect(service.currentFor('p1')?.id).toBe('e2');
    expect(service.currentFor('p2')?.id).toBe('x1');
  });

  it('register() hace el POST y recarga la vigente', () => {
    let registeredId = '';
    service.register(request(), actor).subscribe((e) => (registeredId = e.id));
    const post = httpMock.expectOne(base);
    expect(post.request.body).toEqual({ date: today, professional: 'Dra. Gómez', diagnosis: 'Diagnóstico de prueba' });
    post.flush({ id: 'e3' }, { status: 201, statusText: 'Created' });
    flushLoad(status({ evaluationId: 'e3', date: `${today}T00:00:00` }));
    expect(registeredId).toBe('e3');
  });

  it('valida fecha, profesional y diagnóstico sin llamar a la API', async () => {
    expect((await errorOf(service.register(request({ date: '' }), actor))).error.message).toContain('obligatoria');
    expect((await errorOf(service.register(request({ date: '2999-01-01' }), actor))).error.message).toContain('futura');
    expect((await errorOf(service.register(request({ professional: 'ab' }), actor))).error.message).toContain('profesional');
    expect((await errorOf(service.register(request({ diagnosis: '   ' }), actor))).error.message).toContain('diagnóstico');
  });

  it('no acepta una fecha anterior a la de la evaluación vigente', async () => {
    service.load('p1').subscribe();
    flushLoad();
    const err = await errorOf(service.register(request({ date: '2026-08-01' }), actor));
    expect(err.error.message).toContain('anterior');
  });
});
