import { Injectable } from '@angular/core';
import { Observable, delay, of, throwError } from 'rxjs';
import { isBeforeToday } from '../../../shared/utils/date.util';
import {
  AttendanceActivity,
  DailyAttendance,
  SaveDailyAttendanceRequest,
} from '../interfaces/attendance.interface';

/**
 * SCRUM-218: asistencia diaria por actividad y fecha.
 *
 * OJO — backend pendiente (SCRUM-54): hoy la API solo tiene `POST /api/attendance`
 * con `{ personId }` (SCRUM-135, asistencia individual para mantener activa la ficha).
 * No existe todavía el endpoint por actividad/fecha con conteo de anónimos, rechazo de
 * duplicados ni cierre de día. Hasta que exista, este servicio guarda en memoria (se
 * pierde al recargar) y aplica las mismas reglas que el AC le va a pedir al backend,
 * con la misma forma de error ({ error: { message } }) que lee extractApiError.
 * Al llegar el endpoint real, solo hay que cambiar el cuerpo de `get` y `save` por
 * llamadas HttpClient: los componentes no se tocan.
 */
@Injectable({ providedIn: 'root' })
export class AttendanceService {
  private readonly store = new Map<string, DailyAttendance>();

  private key(activity: AttendanceActivity, date: string): string {
    return `${activity}|${date}`;
  }

  get(activity: AttendanceActivity, date: string): Observable<DailyAttendance | null> {
    return of(this.store.get(this.key(activity, date)) ?? null).pipe(delay(150));
  }

  save(request: SaveDailyAttendanceRequest): Observable<DailyAttendance> {
    const ids = request.persons.map((p) => p.personId);
    if (new Set(ids).size !== ids.length) {
      return this.fail('Hay personas repetidas: no se puede registrar la misma persona dos veces en la misma actividad y fecha.');
    }

    if (!Number.isInteger(request.anonymousCount) || request.anonymousCount < 0) {
      return this.fail('El conteo de personas sin ficha tiene que ser un número entero mayor o igual a 0.');
    }

    if (isBeforeToday(request.date) && !request.justification?.trim()) {
      return this.fail('Modificar la asistencia de un día cerrado requiere una justificación.');
    }

    const saved: DailyAttendance = {
      activity: request.activity,
      date: request.date,
      persons: request.persons,
      anonymousCount: request.anonymousCount,
      justification: request.justification?.trim() || null,
    };
    this.store.set(this.key(request.activity, request.date), saved);
    return of(saved).pipe(delay(250));
  }

  private fail(message: string): Observable<never> {
    return throwError(() => ({ error: { message } }));
  }
}
