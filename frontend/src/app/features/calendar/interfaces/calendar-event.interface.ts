// SCRUM-28/SCRUM-15: calendario general del Centro Barrial.
// NOTA: el backend de esta feature todavía no existe (se decidió trabajar solo en el
// frontend). Contrato asumido: GET /api/eventos?desde=&hasta= (fechas YYYY-MM-DD),
// tal como lo describe la subtarea SCRUM-186. start/end son datetime-local en texto
// (sin zona horaria, "YYYY-MM-DDTHH:mm:00"), para no mezclar con UTC — mismo criterio
// que todayLocalIso() en shared/utils/date.util.ts.

export interface CalendarEvent {
  id: string;
  title: string;
  description?: string | null;
  start: string;
  end: string;
  // nombre del usuario que creó el evento (el AC de SCRUM-15 pide poder identificar
  // quién lo creó). Se asume que el backend lo completa server-side a partir del
  // usuario autenticado (mismo criterio que createdByName en Colaboradores), nunca
  // elegido a mano en el formulario. Por las dudas el backend no lo mande todavía,
  // CalendarComponent.saveEvent() lo completa con el usuario logueado como fallback.
  authorName: string;
  // true para las actividades recurrentes precargadas (desayuno/almuerzo/merendero,
  // ver AC de SCRUM-15) — se generan en el frontend, no vienen del backend, y no se
  // pueden editar ni eliminar.
  isRecurring?: boolean;
}

export interface CreateCalendarEventDto {
  title: string;
  description?: string | null;
  start: string;
  end: string;
}

export type UpdateCalendarEventDto = CreateCalendarEventDto;
