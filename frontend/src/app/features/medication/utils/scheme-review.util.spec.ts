import { daysSinceReview, evaluationExpiryDate, isEvaluationExpired, needsReview } from './scheme-review.util';

describe('scheme-review.util', () => {
  const now = new Date('2026-10-13T12:00:00Z');
  const scheme = (reviewedAt: string | null, active = true) => ({ active, reviewedAt, loadedAt: '2026-08-01T12:00:00Z' });

  it('cuenta los días desde la última revisión', () => {
    expect(daysSinceReview(scheme('2026-10-03T12:00:00Z'), now)).toBe(10);
  });

  it('sin revisión usa la fecha de carga', () => {
    expect(daysSinceReview(scheme(null), now)).toBe(73);
  });

  it('alerta recién a partir de más de 30 días', () => {
    expect(needsReview(scheme('2026-09-13T12:00:00Z'), now)).toBe(false); // 30 días
    expect(needsReview(scheme('2026-09-12T12:00:00Z'), now)).toBe(true); // 31 días
  });

  it('un esquema histórico nunca alerta', () => {
    expect(needsReview(scheme(null, false), now)).toBe(false);
  });

  it('la evaluación vence a los 6 meses', () => {
    expect(evaluationExpiryDate('2026-01-15')).toBe('2026-07-15');
    expect(isEvaluationExpired('2026-01-15', '2026-07-15')).toBe(false);
    expect(isEvaluationExpired('2026-01-15', '2026-07-16')).toBe(true);
  });

  it('ajusta el fin de mes al calcular el vencimiento', () => {
    expect(evaluationExpiryDate('2026-08-31')).toBe('2027-02-28');
  });
});
