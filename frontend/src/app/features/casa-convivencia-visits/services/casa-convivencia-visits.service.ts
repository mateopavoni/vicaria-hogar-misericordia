import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, forkJoin, map, of, switchMap, tap, throwError } from 'rxjs';
import { CreateVisitDto, Resident, UpdateVisitDto, Visit, VisitStatus } from '../interfaces/visit.interface';

interface PagedResult<T> {
  items: T[];
  total: number;
  totalPages: number;
}

// forma real de GET api/casona-visits (CasonaVisitListItemDto); el estado viaja como número
interface VisitListItem {
  id: string;
  personId: string;
  personName: string;
  visitorName: string;
  date: string;
  startTime: string;
  estimatedDurationMinutes: number;
  status: number;
  cancellationReason: string | null;
}

// forma real de GET api/social-records/list (SocialRecordListItemDto)
interface SocialRecordListItem {
  personId: string;
  firstName: string;
  lastName: string | null;
}

// VisitStatus del backend: 0 Pending, 1 Completed, 2 Cancelled
const STATUS_FROM_API: VisitStatus[] = ['pending', 'done', 'cancelled'];
const STATUS_TO_API: Record<VisitStatus, number> = { pending: 0, done: 1, cancelled: 2 };

@Injectable({
  providedIn: 'root',
})
export class CasaConvivenciaVisitsService {
  private http = inject(HttpClient);
  private apiUrl = '/api/casona-visits';

  // se recuerdan las visitas del último rango consultado para devolver la visita actualizada
  // (el PUT y el PATCH de estado responden 204 sin cuerpo).
  private known = new Map<string, Visit>();

  // rango visible (día, semana o mes); fechas YYYY-MM-DD. El backend pagina de a 10.
  getByRange(desde: string, hasta: string): Observable<Visit[]> {
    const page = (n: number) =>
      this.http.get<PagedResult<VisitListItem>>(this.apiUrl, {
        params: new HttpParams().set('from', desde).set('to', hasta).set('page', n),
      });

    return page(1).pipe(
      switchMap((first) => {
        const rest = Array.from({ length: Math.max(0, first.totalPages - 1) }, (_, i) => page(i + 2));
        return rest.length ? forkJoin(rest).pipe(map((pages) => [first, ...pages])) : of([first]);
      }),
      map((pages) => pages.flatMap((p) => p.items).map((v) => this.toVisit(v))),
      tap((visits) => visits.forEach((v) => this.known.set(v.id, v))),
    );
  }

  // SCRUM-214: selector de residente. Reusa el listado de fichas filtrado a residentes activos
  // (personType 1 = Resident, status 0 = Active) hasta que exista un endpoint liviano propio.
  getResidents(): Observable<Resident[]> {
    const page = (n: number) =>
      this.http.get<PagedResult<SocialRecordListItem>>('/api/social-records/list', {
        params: new HttpParams().set('personType', 1).set('status', 0).set('page', n),
      });

    return page(1).pipe(
      switchMap((first) => {
        const rest = Array.from({ length: Math.max(0, first.totalPages - 1) }, (_, i) => page(i + 2));
        return rest.length ? forkJoin(rest).pipe(map((pages) => [first, ...pages])) : of([first]);
      }),
      map((pages) =>
        pages
          .flatMap((p) => p.items)
          .map((r) => ({ id: r.personId, fullName: `${r.firstName} ${r.lastName ?? ''}`.trim() })),
      ),
    );
  }

  create(dto: CreateVisitDto): Observable<Visit> {
    const [date, startTime] = dto.start.split('T');
    const body = {
      personId: dto.residentId,
      visitorName: dto.visitorName,
      date,
      startTime,
      estimatedDurationMinutes: dto.durationMinutes,
      allowOverlap: dto.allowOverlap ?? false,
    };
    return this.http
      .post<{ id: string }>(this.apiUrl, body)
      .pipe(map(({ id }) => ({ id, residentName: '', status: 'pending' as VisitStatus, ...dto }) as Visit));
  }

  update(id: string, dto: UpdateVisitDto): Observable<Visit> {
    const current = this.known.get(id);
    return this.put(id, {
      residentId: dto.residentId,
      visitorName: dto.visitorName,
      start: dto.start,
      durationMinutes: dto.durationMinutes,
      status: current?.status ?? 'pending',
      cancellationReason: current?.cancellationReason ?? null,
    }, dto.allowOverlap ?? false);
  }

  // SCRUM-74 (AC): se puede marcar una visita como realizada, cancelada o pendiente.
  updateStatus(id: string, status: VisitStatus, cancellationReasonInput?: string | null): Observable<Visit> {
    const current = this.known.get(id);
    if (!current) {
      return throwError(() => new Error('Visita desconocida: recargá el calendario.'));
    }
    const cancellationReason =
      status === 'cancelled' ? cancellationReasonInput?.trim() || null : null;
    // PATCH api/casona-visits/{id}/status responde 204 sin cuerpo (SCRUM-211)
    return this.http
      .patch<void>(`${this.apiUrl}/${id}/status`, { status: STATUS_TO_API[status], cancellationReason })
      .pipe(
        map(() => {
          const updated = { ...current, status, cancellationReason } as Visit;
          this.known.set(id, updated);
          return updated;
        }),
      );
  }

  private put(
    id: string,
    v: Pick<Visit, 'residentId' | 'visitorName' | 'start' | 'durationMinutes' | 'status' | 'cancellationReason'>,
    allowOverlap = false,
  ): Observable<Visit> {
    const [date, startTime] = v.start.split('T');
    const body = {
      personId: v.residentId,
      visitorName: v.visitorName,
      date,
      startTime,
      estimatedDurationMinutes: v.durationMinutes,
      status: STATUS_TO_API[v.status],
      cancellationReason: v.cancellationReason ?? null,
      allowOverlap,
    };
    // el PUT responde 204 sin cuerpo
    return this.http.put<void>(`${this.apiUrl}/${id}`, body).pipe(
      map(() => {
        const previous = this.known.get(id);
        const updated = { ...(previous ?? { id, residentName: '' }), id, ...v } as Visit;
        this.known.set(id, updated);
        return updated;
      }),
    );
  }

  private toVisit(v: VisitListItem): Visit {
    return {
      id: v.id,
      residentId: v.personId,
      residentName: v.personName,
      visitorName: v.visitorName,
      start: `${v.date.slice(0, 10)}T${v.startTime.slice(0, 8)}`,
      durationMinutes: v.estimatedDurationMinutes,
      status: STATUS_FROM_API[v.status] ?? 'pending',
      cancellationReason: v.cancellationReason,
    };
  }
}
