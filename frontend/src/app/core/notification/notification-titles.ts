// Títulos legibles de las notificaciones según el EventType que manda el backend.
const EVENT_TITLES: Record<string, string> = {
  NuevoUsuarioPendiente: 'Nueva solicitud de cuenta',
  CuentaBloqueada: 'Cuenta bloqueada',
  FichaSinObservaciones: 'Ficha sin seguimiento',
  // Aviso a referentes cuando una persona pasa a Inactivo (manual o por 30 días sin asistir);
  // el backend manda "PersonStatusInactive" (antes acá figuraba "PersonaInactiva" y no coincidía)
  PersonStatusInactive: 'Persona pasó a Inactiva',
  EsquemaMedicacionSinRevision: 'Esquema de medicación sin revisión',
  EsquemaMedicacionRevisionMensual: 'Revisión mensual de medicación',
  RespaldoFallido: 'Falló el respaldo automático',
};

/**
 * Título de una notificación. Si el backend agrega un tipo nuevo sin título acá, se muestra
 * separado en palabras ("EsquemaMedicacionSinRevision" → "Esquema medicacion sin revision")
 * en vez del código pegado.
 */
export function notificationTitle(eventType: string): string {
  const known = EVENT_TITLES[eventType];
  if (known) return known;
  const words = eventType
    .replace(/[_-]+/g, ' ')
    .replace(/([a-záéíóúñ0-9])([A-ZÁÉÍÓÚÑ])/g, '$1 $2')
    .trim()
    .toLowerCase();
  return words.charAt(0).toUpperCase() + words.slice(1);
}
