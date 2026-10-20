import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, forkJoin, map, of, switchMap, throwError } from 'rxjs';
import { isBeforeToday } from '../../../shared/utils/date.util';
import {
  AttendanceActivity,
  AttendanceHistoryEntry,
  AttendanceOverview,
  CASA_CONVIVENCIA_ID,
  DailyAttendance,
  HOGAR_DE_DIA_ID,
  InactivePerson,
  SaveDailyAttendanceRequest,
} from '../interfaces/attendance.interface';

/** AttendanceActivity del backend (serializado como número). */
const BACKEND_ACTIVITY: Record<string, number> = {
  [HOGAR_DE_DIA_ID]: 0,
  [CASA_CONVIVENCIA_ID]: 1,
};
const FRONT_ACTIVITY: Record<number, AttendanceActivity> = {
  0: HOGAR_DE_DIA_ID,
  1: CASA_CONVIVENCIA_ID,
};

const UNSUPPORTED_ACTIVITY =
  'Por ahora el sistema solo registra asistencia de "Hogar de día" y "Casa de Convivencia": las actividades configurables todavía no tienen registro propio.';

interface DailyDto {
  anonymousCount: number;
  persons: { attendanceId: string; personId: string; fullName: string; dni: string | null; anonymousCount: number }[];
}

interface HistoryDto {
  groups: { activity: number; attendances: { id: string; date: string }[] }[];
}

interface StatisticsDto {
  totalRecords: number;
  totalAnonymous: number;
  averagePerDay: number;
  totalsByActivity: { activity: number; activityName: string; count: number }[];
  previousPeriodTotal: number;
  comparisonPercentage: number | null;
  weeklyAverages: { averagePerDay: number }[];
  monthlyAverages: { averagePerDay: number }[];
}

interface InactiveDto {
  personId: string;
  firstName: string;
  lastName: string | null;
  lastAttendanceDate: string | null;
  daysSinceLastAttendance: number | null;
}

/** Resumen del panel de estadísticas: lo que calcula el backend (SCRUM-224). */
export interface AttendanceStatistics extends AttendanceOverview {
  previousPeriodTotal: number;
  comparisonPercentage: number | null;
}

/**
 * SCRUM-218: asistencia diaria por actividad y fecha (/api/attendances).
 *
 * El backend guarda una fila por persona, actividad y fecha, con el conteo de personas sin
 * ficha repartido en esas filas. Por eso:
 * - `save` solo agrega personas nuevas (una por llamada) y ajusta el conteo sin ficha sobre una
 *   fila existente; no se puede quitar a una persona ya registrada.
 * - Con día sin personas con ficha no hay fila donde guardar personas sin ficha.
 * - Solo existen las actividades fijas "Hogar de día" y "Casa de Convivencia".
 */
@Injectable({ providedIn: 'root' })
export class AttendanceService {
  private readonly http = inject(HttpClient);

  get(activity: AttendanceActivity, date: string): Observable<DailyAttendance | null> {
    const code = BACKEND_ACTIVITY[activity];
    if (code === undefined) {
      return of(null);
    }
    return this.fetchDay(activity, code, date).pipe(map((result) => (result.attendance.persons.length ? result.attendance : null)));
  }

  // SCRUM-222: fechas en las que una persona asistió, de todas las actividades, de más
  // reciente a más antigua. Los filtros por actividad y rango se aplican en pantalla.
  getPersonHistory(personId: string): Observable<AttendanceHistoryEntry[]> {
    return this.http.get<HistoryDto>(`/api/personas/${personId}/asistencias`).pipe(
      map((dto) => {
        const entries: AttendanceHistoryEntry[] = [];
        for (const group of dto.groups) {
          const activity = FRONT_ACTIVITY[group.activity];
          for (const item of group.attendances) {
            entries.push({ activity, date: item.date.slice(0, 10) });
          }
        }
        return entries.sort((a, b) => b.date.localeCompare(a.date));
      }),
    );
  }

  save(request: SaveDailyAttendanceRequest): Observable<DailyAttendance> {
    const ids = request.persons.map((p) => p.personId);
    if (new Set(ids).size !== ids.length) {
      return this.fail('Hay personas repetidas: no se puede registrar la misma persona dos veces en la misma actividad y fecha.');
    }
    if (!Number.isInteger(request.anonymousCount) || request.anonymousCount < 0) {
      return this.fail('El conteo de personas sin ficha tiene que ser un número entero mayor o igual a 0.');
    }
    const justification = request.justification?.trim() || null;
    const closed = isBeforeToday(request.date);
    if (closed && !justification) {
      return this.fail('Modificar la asistencia de un día cerrado requiere una justificación.');
    }
    const code = BACKEND_ACTIVITY[request.activity];
    if (code === undefined) {
      return this.fail(UNSUPPORTED_ACTIVITY);
    }

    return this.fetchDay(request.activity, code, request.date).pipe(
      switchMap(({ raw }) => {
        const existing = new Set(raw.persons.map((p) => p.personId));
        const removed = raw.persons.filter((p) => !ids.includes(p.personId));
        if (removed.length) {
          return this.fail('No se puede quitar a una persona ya registrada: el sistema no permite borrar asistencias.');
        }
        const added = request.persons.filter((p) => !existing.has(p.personId));
        const anchor = raw.persons[0];
        if (!anchor && !added.length && request.anonymousCount > 0) {
          return this.fail('Para registrar personas sin ficha tiene que haber al menos una persona con ficha en el día.');
        }

        const calls: Observable<unknown>[] = added.map((person, index) =>
          this.http.post<void>('/api/attendances', {
            personId: person.personId,
            activity: code,
            date: request.date,
            // el conteo sin ficha viaja en la primera fila del día
            anonymousCount: !anchor && index === 0 ? request.anonymousCount : 0,
          }),
        );

        if (anchor) {
          const anonymousChanged = request.anonymousCount !== raw.anonymousCount;
          if (anonymousChanged || (closed && justification)) {
            calls.push(
              this.http.put<void>(`/api/attendances/${anchor.attendanceId}`, {
                anonymousCount: anchor.anonymousCount + (request.anonymousCount - raw.anonymousCount),
                justification: closed ? justification : null,
              }),
            );
          }
        }

        const done$ = calls.length ? forkJoin(calls) : of([]);
        return done$.pipe(switchMap(() => this.fetchDay(request.activity, code, request.date)));
      }),
      map(({ attendance }) => ({ ...attendance, justification })),
    );
  }

  // SCRUM-226: estadísticas calculadas por el backend para el rango y la actividad elegidos.
  getStatistics(from: string, to: string, activity: AttendanceActivity | null): Observable<AttendanceStatistics> {
    let params = new HttpParams().set('from', from).set('to', to);
    if (activity !== null) {
      const code = BACKEND_ACTIVITY[activity];
      params = params.set('activity', code === undefined ? activity : this.activityName(code));
    }
    return this.http.get<StatisticsDto>('/api/attendances/statistics', { params }).pipe(
      map((dto) => {
        const average = (list: { averagePerDay: number }[]) =>
          list.length ? round1(list.reduce((sum, i) => sum + i.averagePerDay, 0) / list.length) : 0;
        const totalPresences = dto.totalRecords + dto.totalAnonymous;
        return {
          totalPresences,
          daysWithData: totalPresences > 0 ? 1 : 0,
          dailyAverage: round1(dto.averagePerDay),
          weeklyAverage: average(dto.weeklyAverages),
          monthlyAverage: average(dto.monthlyAverages),
          byActivity: dto.totalsByActivity.map((t) => ({
            activity: FRONT_ACTIVITY[t.activity] ?? String(t.activity),
            label: t.activityName,
            totalPresences: t.count,
            daysWithData: 0,
            averagePerDay: 0,
          })),
          perDay: [],
          previousPeriodTotal: dto.previousPeriodTotal,
          comparisonPercentage: dto.comparisonPercentage,
        };
      }),
    );
  }

  /** Personas sin asistir hace más de `days` días (SCRUM-225). */
  getInactive(days: number): Observable<InactivePerson[]> {
    const params = new HttpParams().set('days', days);
    return this.http.get<InactiveDto[]>('/api/attendances/inactive', { params }).pipe(
      map((dtos) =>
        dtos.map((dto) => ({
          personId: dto.personId,
          fullName: `${dto.firstName} ${dto.lastName ?? ''}`.trim(),
          lastAttendance: (dto.lastAttendanceDate ?? '').slice(0, 10),
          daysSince: dto.daysSinceLastAttendance ?? 0,
        })),
      ),
    );
  }

  /** Resumen mensual en PDF generado por el backend (SCRUM-225). */
  getMonthlySummaryPdf(year: number, month: number): Observable<Blob> {
    const params = new HttpParams().set('year', year).set('month', month);
    return this.http.get('/api/attendances/monthly-summary/pdf', { params, responseType: 'blob' });
  }

  private fetchDay(
    activity: AttendanceActivity,
    code: number,
    date: string,
  ): Observable<{ raw: DailyDto; attendance: DailyAttendance }> {
    const params = new HttpParams().set('activity', code).set('date', date);
    return this.http.get<DailyDto>('/api/attendances', { params }).pipe(
      map((raw) => ({
        raw,
        attendance: {
          activity,
          date,
          persons: raw.persons.map((p) => ({ personId: p.personId, fullName: p.fullName, dni: p.dni })),
          anonymousCount: raw.anonymousCount,
          justification: null,
        },
      })),
    );
  }

  // la estadística filtra por nombre de actividad (string), igual que el resumen mensual
  private activityName(code: number): string {
    return code === 0 ? 'DayCenter' : 'CasaConvivencia';
  }

  private fail(message: string): Observable<never> {
    return throwError(() => ({ error: { message } }));
  }
}

const round1 = (value: number): number => Math.round(value * 10) / 10;
