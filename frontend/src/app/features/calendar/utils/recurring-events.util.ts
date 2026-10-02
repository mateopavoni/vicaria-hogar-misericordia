import { CalendarEvent } from '../interfaces/calendar-event.interface';
import { addDays, isoDate } from './calendar-date.util';

/**
 * SCRUM-15 (AC): "el calendario muestra actividades recurrentes precargadas: almuerzo
 * y desayuno de lunes a viernes, merendero martes/miércoles/jueves por la tarde".
 * Se generan en el frontend para el rango visible (no vienen del backend), con un id
 * sintético estable por día para poder abrir su detalle sin confundirse con eventos
 * reales. No se pueden editar/eliminar (ver isRecurring en la interfaz).
 */
interface RecurringTemplate {
  key: string;
  title: string;
  description: string;
  // 0 = domingo .. 6 = sábado (Date.getDay())
  daysOfWeek: number[];
  startHour: number;
  startMinute: number;
  endHour: number;
  endMinute: number;
}

const RECURRING_TEMPLATES: RecurringTemplate[] = [
  {
    key: 'desayuno',
    title: 'Desayuno',
    description: 'Desayuno diario del Centro Barrial.',
    daysOfWeek: [1, 2, 3, 4, 5],
    startHour: 8,
    startMinute: 0,
    endHour: 9,
    endMinute: 0,
  },
  {
    key: 'almuerzo',
    title: 'Almuerzo',
    description: 'Almuerzo diario del Centro Barrial.',
    daysOfWeek: [1, 2, 3, 4, 5],
    startHour: 12,
    startMinute: 0,
    endHour: 13,
    endMinute: 0,
  },
  {
    key: 'merendero',
    title: 'Merendero',
    description: 'Merendero de la tarde.',
    daysOfWeek: [2, 3, 4],
    startHour: 16,
    startMinute: 0,
    endHour: 17,
    endMinute: 30,
  },
];

const RECURRING_AUTHOR = 'Actividad recurrente del hogar';
// no las creó ningún usuario real, así que no tienen un id de autor — se usa un
// valor fijo, nunca va a matchear con AuthService.user()?.id (ver canManageEvent en
// CalendarComponent), que ya las excluye de todos modos por isRecurring.
const RECURRING_AUTHOR_ID = 'recurring';

export function generateRecurringEvents(rangeStart: Date, rangeEnd: Date): CalendarEvent[] {
  const events: CalendarEvent[] = [];

  let cursor = new Date(rangeStart);
  cursor.setHours(0, 0, 0, 0);

  const end = new Date(rangeEnd);
  end.setHours(0, 0, 0, 0);

  while (cursor <= end) {
    const dow = cursor.getDay();
    const dateStr = isoDate(cursor);

    for (const tpl of RECURRING_TEMPLATES) {
      if (!tpl.daysOfWeek.includes(dow)) continue;

      const pad = (n: number) => String(n).padStart(2, '0');
      const start = `${dateStr}T${pad(tpl.startHour)}:${pad(tpl.startMinute)}:00`;
      const finish = `${dateStr}T${pad(tpl.endHour)}:${pad(tpl.endMinute)}:00`;

      events.push({
        id: `recurring-${tpl.key}-${dateStr}`,
        title: tpl.title,
        description: tpl.description,
        start,
        end: finish,
        authorName: RECURRING_AUTHOR,
        authorId: RECURRING_AUTHOR_ID,
        isRecurring: true,
      });
    }

    cursor = addDays(cursor, 1);
  }

  return events;
}
