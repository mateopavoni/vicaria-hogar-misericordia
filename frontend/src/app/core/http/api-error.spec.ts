import { extractApiError } from './api-error';

describe('extractApiError', () => {
  it('usa el message del backend', () => {
    expect(extractApiError({ error: { message: 'Conflicto' } }, 'x')).toBe('Conflicto');
  });

  it('usa el primer mensaje de un ValidationProblem', () => {
    const err = { error: { errors: { Content: ['No se puede cargar información sobre abusos.'] } } };
    expect(extractApiError(err, 'x')).toBe('No se puede cargar información sobre abusos.');
  });

  it('cae al fallback si no hay detalle', () => {
    expect(extractApiError({}, 'Fallo genérico')).toBe('Fallo genérico');
    expect(extractApiError(null, 'Fallo genérico')).toBe('Fallo genérico');
  });
});
