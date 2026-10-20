import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, map, switchMap, tap, throwError } from 'rxjs';
import {
  AttendanceActivity,
  AttendanceActivityDef,
  SYSTEM_ACTIVITIES,
} from '../interfaces/attendance.interface';

const NAME_MIN = 3;
const NAME_MAX = 60;

interface ActivityDto {
  id: string;
  name: string;
  isActive: boolean;
  isFromCalendar: boolean;
}

const normalize = (name: string): string => name.trim().replace(/\s+/g, ' ');
const keyOf = (name: string): string => normalize(name).toLowerCase();

/**
 * SCRUM-230: catálogo de actividades de asistencia, configurable por el Referente
 * (/api/actividades).
 *
 * - "Hogar de día" y "Casa de Convivencia (diaria)" son fijas (source 'system', no se desactivan)
 *   y no existen en el catálogo del backend: viven acá como constantes.
 * - Las actividades recurrentes del calendario general se sincronizan con `syncFromCalendar()`
 *   (el backend las agrega y marca `isFromCalendar`). Nunca se borran si desaparecen del calendario.
 * - Las demás se crean a mano (source 'custom') y pertenecen al Hogar de día.
 * - OJO: el registro de asistencia del backend (POST /api/attendances) solo conoce las dos
 *   actividades fijas; ver AttendanceService.
 */
@Injectable({ providedIn: 'root' })
export class AttendanceActivitiesService {
  private readonly http = inject(HttpClient);

  private readonly state = signal<AttendanceActivityDef[]>(SYSTEM_ACTIVITIES.map((a) => ({ ...a })));

  /** Todas, activas o no (para mostrar nombres de registros viejos). */
  readonly all = this.state.asReadonly();
  readonly active = computed(() => this.state().filter((a) => a.active));

  name(id: AttendanceActivity): string {
    return this.state().find((a) => a.id === id)?.name ?? id;
  }

  /** Vuelve a leer el catálogo del backend (incluye las desactivadas). */
  refresh(): Observable<AttendanceActivityDef[]> {
    const params = new HttpParams().set('includeInactive', true);
    return this.http.get<ActivityDto[]>('/api/actividades', { params }).pipe(
      map((dtos) => [...SYSTEM_ACTIVITIES.map((a) => ({ ...a })), ...dtos.map(toDef)]),
      tap((list) => this.state.set(list)),
    );
  }

  setActive(id: AttendanceActivity, active: boolean): Observable<AttendanceActivityDef> {
    const found = this.state().find((a) => a.id === id);
    if (!found) {
      return this.fail('La actividad no existe.');
    }
    if (found.source === 'system') {
      return this.fail(`"${found.name}" es una actividad fija y no se puede desactivar.`);
    }
    return this.http
      .put<void>(`/api/actividades/${id}`, { name: found.name, isActive: active })
      .pipe(switchMap(() => this.refresh()), map((list) => list.find((a) => a.id === id) ?? { ...found, active }));
  }

  /** Crea una actividad propia del Hogar de día. */
  create(rawName: string): Observable<AttendanceActivityDef> {
    const name = normalize(rawName);
    if (name.length < NAME_MIN) {
      return this.fail(`El nombre tiene que tener al menos ${NAME_MIN} caracteres.`);
    }
    if (name.length > NAME_MAX) {
      return this.fail(`El nombre no puede superar los ${NAME_MAX} caracteres.`);
    }
    if (this.state().some((a) => keyOf(a.name) === keyOf(name))) {
      return this.fail('Ya existe una actividad con ese nombre.');
    }
    return this.http.post<{ id: string }>('/api/actividades', { name }).pipe(
      switchMap((created) =>
        this.refresh().pipe(map((list) => list.find((a) => a.id === created.id) as AttendanceActivityDef)),
      ),
    );
  }

  /**
   * Agrega al catálogo las actividades recurrentes del calendario general.
   * Devuelve cuántas actividades nuevas sumó.
   */
  syncFromCalendar(): Observable<number> {
    return this.http
      .post<{ syncedCount: number }>('/api/actividades/sincronizar-calendario', {})
      .pipe(switchMap((result) => this.refresh().pipe(map(() => result.syncedCount))));
  }

  private fail(message: string): Observable<never> {
    return throwError(() => ({ error: { message } }));
  }
}

function toDef(dto: ActivityDto): AttendanceActivityDef {
  return {
    id: dto.id,
    name: dto.name,
    area: 'hogar-de-dia',
    active: dto.isActive,
    source: dto.isFromCalendar ? 'calendar' : 'custom',
  };
}
