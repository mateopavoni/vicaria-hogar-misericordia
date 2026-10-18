import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, map } from 'rxjs';
import { CalendarEventsService } from '../../calendar/services/calendar-events.service';
import { isoDate } from '../../calendar/utils/calendar-date.util';
import {
  AttendanceActivity,
  AttendanceActivityDef,
  AttendanceActivitySource,
  SYSTEM_ACTIVITIES,
} from '../interfaces/attendance.interface';

export type ActivityActionResult = { ok: true; activity: AttendanceActivityDef } | { ok: false; message: string };

const NAME_MIN = 3;
const NAME_MAX = 60;

const normalize = (name: string): string => name.trim().replace(/\s+/g, ' ');
const keyOf = (name: string): string => normalize(name).toLowerCase();

function slugify(name: string): string {
  return (
    normalize(name)
      .toLowerCase()
      .normalize('NFD')
      .replace(/[̀-ͯ]/g, '')
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '') || 'actividad'
  );
}

/**
 * SCRUM-230: catálogo de actividades de asistencia, configurable por el Referente.
 *
 * - "Hogar de día" y "Casa de Convivencia (diaria)" son fijas (source 'system', no se desactivan).
 * - Las actividades recurrentes del calendario general (desayuno, almuerzo, merendero y las
 *   recurrentes que cargue el equipo) se sincronizan solas: `syncFromCalendar()` las agrega al
 *   catálogo. Nunca se borran del catálogo si desaparecen del calendario, para no perder historial.
 * - Las demás se crean a mano (source 'custom') y pertenecen al Hogar de día.
 *
 * OJO — backend pendiente: no existe un endpoint de catálogo de actividades. Igual que
 * AttendanceService, esto vive en memoria (se pierde al recargar) hasta que exista.
 */
@Injectable({ providedIn: 'root' })
export class AttendanceActivitiesService {
  private calendarEventsService = inject(CalendarEventsService);

  private readonly state = signal<AttendanceActivityDef[]>(SYSTEM_ACTIVITIES.map((a) => ({ ...a })));

  /** Todas, activas o no (para mostrar nombres de registros viejos). */
  readonly all = this.state.asReadonly();
  readonly active = computed(() => this.state().filter((a) => a.active));

  name(id: AttendanceActivity): string {
    return this.state().find((a) => a.id === id)?.name ?? id;
  }

  setActive(id: AttendanceActivity, active: boolean): ActivityActionResult {
    const found = this.state().find((a) => a.id === id);
    if (!found) {
      return { ok: false, message: 'La actividad no existe.' };
    }
    if (found.source === 'system' && !active) {
      return { ok: false, message: `"${found.name}" es una actividad fija y no se puede desactivar.` };
    }
    const updated = { ...found, active };
    this.state.update((list) => list.map((a) => (a.id === id ? updated : a)));
    return { ok: true, activity: updated };
  }

  /** Crea una actividad propia del Hogar de día. */
  create(rawName: string): ActivityActionResult {
    const name = normalize(rawName);
    if (name.length < NAME_MIN) {
      return { ok: false, message: `El nombre tiene que tener al menos ${NAME_MIN} caracteres.` };
    }
    if (name.length > NAME_MAX) {
      return { ok: false, message: `El nombre no puede superar los ${NAME_MAX} caracteres.` };
    }
    if (this.exists(name)) {
      return { ok: false, message: 'Ya existe una actividad con ese nombre.' };
    }
    return { ok: true, activity: this.add(name, 'custom') };
  }

  /**
   * Agrega al catálogo las actividades recurrentes del calendario general del mes en curso
   * (las precargadas y las que tienen repetición). Devuelve cuántas actividades nuevas sumó.
   */
  syncFromCalendar(): Observable<number> {
    const now = new Date();
    const from = isoDate(new Date(now.getFullYear(), now.getMonth(), 1));
    const to = isoDate(new Date(now.getFullYear(), now.getMonth() + 1, 0));

    return this.calendarEventsService.getByRange(from, to, 'general').pipe(
      map((events) => {
        let added = 0;
        for (const event of events) {
          const isGroup = event.isRecurring || (event.recurrence && event.recurrence !== 'none');
          if (isGroup && event.title && !this.exists(event.title)) {
            this.add(normalize(event.title), 'calendar');
            added++;
          }
        }
        return added;
      }),
    );
  }

  private exists(name: string): boolean {
    const key = keyOf(name);
    return this.state().some((a) => keyOf(a.name) === key);
  }

  private add(name: string, source: AttendanceActivitySource): AttendanceActivityDef {
    const prefix = source === 'calendar' ? 'cal' : 'custom';
    const base = `${prefix}-${slugify(name)}`;
    let id = base;
    let n = 2;
    while (this.state().some((a) => a.id === id)) {
      id = `${base}-${n++}`;
    }
    const activity: AttendanceActivityDef = { id, name, area: 'hogar-de-dia', active: true, source };
    this.state.update((list) => [...list, activity]);
    return activity;
  }
}
