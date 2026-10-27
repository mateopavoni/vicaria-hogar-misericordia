import { firstValueFrom } from 'rxjs';
import { errorOf } from '../../../shared/testing/error-of';
import { todayLocalIso } from '../../../shared/utils/date.util';
import { PsychiatricEvaluationsService } from './psychiatric-evaluations.service';

describe('PsychiatricEvaluationsService', () => {
  let service: PsychiatricEvaluationsService;
  const actor = { id: 'u1', name: 'Belén Test' };
  const today = todayLocalIso();
  const request = (overrides = {}) => ({ personId: 'p1', date: today, professional: 'Dra. Gómez', diagnosis: 'Diagnóstico de prueba', ...overrides });

  beforeEach(() => {
    service = new PsychiatricEvaluationsService();
  });

  it('registra la evaluación con fecha, profesional, diagnóstico y vigente', async () => {
    const evaluation = await firstValueFrom(service.register(request(), actor));
    expect(evaluation.vigente).toBe(true);
    expect(evaluation.professional).toBe('Dra. Gómez');
    expect(evaluation.registeredByName).toBe('Belén Test');
    expect(service.currentFor('p1')?.id).toBe(evaluation.id);
  });

  it('al cargar una nueva, las anteriores pasan a vigente = false y queda una sola vigente', async () => {
    await firstValueFrom(service.register(request({ date: '2026-01-10' }), actor));
    await firstValueFrom(service.register(request({ date: '2026-03-10' }), actor));
    const last = await firstValueFrom(service.register(request({ date: '2026-05-10' }), actor));

    const all = service.forPerson('p1');
    expect(all.length).toBe(3);
    expect(all.filter((e) => e.vigente).length).toBe(1);
    expect(all.find((e) => e.vigente)?.id).toBe(last.id);
  });

  it('no toca las evaluaciones de otras personas', async () => {
    await firstValueFrom(service.register(request({ personId: 'p2' }), actor));
    await firstValueFrom(service.register(request(), actor));
    expect(service.currentFor('p2')).not.toBeNull();
  });

  it('valida fecha, profesional y diagnóstico', async () => {
    expect((await errorOf(service.register(request({ date: '' }), actor))).error.message).toContain('fecha');
    expect((await errorOf(service.register(request({ date: '2999-01-01' }), actor))).error.message).toContain('futura');
    expect((await errorOf(service.register(request({ professional: 'x' }), actor))).error.message).toContain('profesional');
    expect((await errorOf(service.register(request({ diagnosis: '  ' }), actor))).error.message).toContain('diagnóstico');
  });

  it('no acepta una fecha anterior a la de la evaluación vigente', async () => {
    await firstValueFrom(service.register(request({ date: '2026-05-10' }), actor));
    const err = await errorOf(service.register(request({ date: '2026-01-01' }), actor));
    expect(err.error.message).toContain('anterior');
  });
});
