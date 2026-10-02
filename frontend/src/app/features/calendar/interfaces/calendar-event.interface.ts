// SCRUM-28/SCRUM-15/SCRUM-16: calendario general del Centro Barrial.
// NOTA: el backend de esta feature todavía no existe (se decidió trabajar solo en el
// frontend). Contrato asumido: GET /api/eventos?desde=&hasta= (fechas YYYY-MM-DD),
// tal como lo describe la subtarea SCRUM-186. start/end son datetime-local en texto
// (sin zona horaria, "YYYY-MM-DDTHH:mm:00"), para no mezclar con UTC — mismo criterio
// que todayLocalIso() en shared/utils/date.util.ts.

// SCRUM-16 (AC): se puede marcar un evento como recurrente (diario/semanal/mensual).
// Contrato asumido: el backend guarda la regla de recurrencia una sola vez y expande
// las ocurrencias dentro del rango pedido por GET /api/eventos?desde=&hasta=, cada una
// como un CalendarEvent más (mismo id de la serie). El frontend no vuelve a expandir
// nada — solo manda/lee este campo y muestra lo que el backend ya expandió. Editar una
// ocurrencia edita toda la serie (no hay "solo esta vez"/"a partir de esta", no lo pide
// el AC). Nada que ver con `isRecurring` (abajo), que es para las actividades
// precargadas fijas, no para estas.
export type RecurrenceFrequency = 'none' | 'daily' | 'weekly' | 'monthly';

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
  // CalendarComponent.loadEvents() lo completa con el usuario logueado como fallback.
  authorName: string;
  // id del usuario que creó el evento — necesario para la regla de autorización del
  // AC de SCRUM-16 ("el autor puede editarlo/eliminarlo; cualquier Referente también").
  // Mismo supuesto que authorName: lo completa el backend a partir del usuario
  // autenticado. LIMITACIÓN conocida sin backend real: si no viene, el fallback de
  // CalendarComponent le asigna el id del usuario que está mirando el calendario en
  // ese momento, lo que simula que todo evento es "propio" — no se puede probar de
  // verdad la regla de "cualquier Referente" hasta que exista el backend.
  authorId: string;
  // true para las actividades recurrentes precargadas (desayuno/almuerzo/merendero,
  // ver AC de SCRUM-15) — se generan en el frontend, no vienen del backend, y no se
  // pueden editar ni eliminar. Distinto de `recurrence` (ver arriba).
  isRecurring?: boolean;
  // 'none' si no se definió — ver RecurrenceFrequency arriba.
  recurrence?: RecurrenceFrequency;
}

export interface CreateCalendarEventDto {
  title: string;
  description?: string | null;
  start: string;
  end: string;
  recurrence: RecurrenceFrequency;
}

export type UpdateCalendarEventDto = CreateCalendarEventDto;
