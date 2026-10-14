import { daysUntilExpiry, evaluationValidity } from './evaluation-validity.util';

describe('evaluation-validity.util', () => {
  // evaluación del 15/01/2026 → vence el 15/07/2026
  const date = '2026-01-15';

  it('vigente cuando falta más de 30 días', () => {
    expect(evaluationValidity(date, '2026-06-01')).toBe('valid');
  });

  it('por vencer cuando faltan menos de 30 días', () => {
    expect(daysUntilExpiry(date, '2026-06-16')).toBe(29);
    expect(evaluationValidity(date, '2026-06-16')).toBe('expiring');
  });

  it('con exactamente 30 días todavía no alerta', () => {
    expect(daysUntilExpiry(date, '2026-06-15')).toBe(30);
    expect(evaluationValidity(date, '2026-06-15')).toBe('valid');
  });

  it('el día del vencimiento todavía es válida y vence al día siguiente', () => {
    expect(evaluationValidity(date, '2026-07-15')).toBe('expiring');
    expect(evaluationValidity(date, '2026-07-16')).toBe('expired');
  });

  it('días negativos cuando ya venció', () => {
    expect(daysUntilExpiry(date, '2026-07-25')).toBe(-10);
  });
});
