// Calendario general del Centro Barrial y calendario personal.
// start/end son fecha y hora local en texto ("YYYY-MM-DDTHH:mm:00"), sin zona horaria.

// el calendario personal solo lo ve quien lo creó; el backend filtra por el usuario autenticado
export type CalendarScope = 'general' | 'personal';

// repetición de un evento. El backend devuelve cada ocurrencia del rango como un evento
// más, con el mismo id de la serie; editar una ocurrencia edita toda la serie.
export type RecurrenceFrequency = 'none' | 'daily' | 'weekly' | 'monthly';

export interface CalendarEvent {
  id: string;
  title: string;
  description?: string | null;
  start: string;
  end: string;
  // nombre de quien creó el evento
  authorName: string;
  // id de quien creó el evento: el autor puede editarlo o eliminarlo
  authorId: string;
  // true para las actividades recurrentes precargadas (desayuno/almuerzo/merendero,
  // ) — se generan en el frontend, no vienen del backend, y no se
  // pueden editar ni eliminar. Distinto de `recurrence` (ver arriba).
  isRecurring?: boolean;
  // 'none' si no se definió — ver RecurrenceFrequency arriba.
  recurrence?: RecurrenceFrequency;
  // 'general' (todo el equipo) o 'personal' (solo el autor). Las actividades
  // precargadas son siempre 'general'.
  scope: CalendarScope;
}

export interface CreateCalendarEventDto {
  title: string;
  description?: string | null;
  start: string;
  end: string;
  recurrence: RecurrenceFrequency;
  scope: CalendarScope;
}

export type UpdateCalendarEventDto = CreateCalendarEventDto;

// lo que devuelve el formulario; el calendario (scope) lo agrega CalendarComponent
export type EventFormValue = Omit<CreateCalendarEventDto, 'scope'>;
