import { notificationTitle } from './notification-titles';

describe('notificationTitle', () => {
  // los EventType que hoy emite el backend: todos tienen que mostrar un título legible
  it.each([
    ['NuevoUsuarioPendiente', 'Nueva solicitud de cuenta'],
    ['CuentaBloqueada', 'Cuenta bloqueada'],
    ['FichaSinObservaciones', 'Ficha sin seguimiento'],
    ['PersonStatusInactive', 'Persona pasó a Inactiva'],
    ['EsquemaMedicacionSinRevision', 'Esquema de medicación sin revisión'],
    ['EsquemaMedicacionRevisionMensual', 'Revisión mensual de medicación'],
    ['RespaldoFallido', 'Falló el respaldo automático'],
  ])('%s → %s', (eventType, title) => {
    expect(notificationTitle(eventType)).toBe(title);
  });

  it('un tipo sin título conocido se muestra separado en palabras, no pegado', () => {
    expect(notificationTitle('TurnoMedicoProximo')).toBe('Turno medico proximo');
    expect(notificationTitle('stock_bajo')).toBe('Stock bajo');
  });
});
