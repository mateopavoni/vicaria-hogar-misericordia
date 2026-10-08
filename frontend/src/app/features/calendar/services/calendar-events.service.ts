import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, forkJoin, map, of, switchMap } from 'rxjs';
import {
  CalendarEvent,
  CalendarScope,
  CreateCalendarEventDto,
  RecurrenceFrequency,
  UpdateCalendarEventDto,
} from '../interfaces/calendar-event.interface';

// forma real de GET api/{general|personal}-calendar-events (PagedResult<CalendarEventOccurrenceDto>)
interface OccurrenceDto {
  eventId: string;
  date: string;
  startTime: string | null;
  endTime: string | null;
  title: string;
  description: string | null;
  authorUserId: string | null;
  authorName: string | null;
  isPreloaded: boolean;
}

interface PagedResult<T> {
  items: T[];
  total: number;
  totalPages: number;
}

// WeekDays del backend (flags): Monday=1 ... Sunday=64
const ALL_WEEK_DAYS = 127;
const WEEK_DAY_FLAGS = [64, 1, 2, 4, 8, 16, 32]; // indexado por Date.getDay() (0 = domingo)

@Injectable({
  providedIn: 'root',
})
export class CalendarEventsService {
  private http = inject(HttpClient);

  private baseUrl(scope: CalendarScope): string {
    return scope === 'personal' ? '/api/personal-calendar-events' : '/api/general-calendar-events';
  }

  // rango visible (semana o mes); fechas YYYY-MM-DD. El backend expande las ocurrencias
  // recurrentes y pagina de a 10, así que se piden todas las páginas del rango.
  // scope=personal: el backend filtra por el usuario del token, nunca por un id del cliente.
  getByRange(desde: string, hasta: string, scope: CalendarScope): Observable<CalendarEvent[]> {
    const page = (n: number) =>
      this.http.get<PagedResult<OccurrenceDto>>(this.baseUrl(scope), {
        params: new HttpParams().set('from', desde).set('to', hasta).set('page', n),
      });

    return page(1).pipe(
      switchMap((first) => {
        const rest = Array.from({ length: Math.max(0, first.totalPages - 1) }, (_, i) => page(i + 2));
        return rest.length ? forkJoin(rest).pipe(map((pages) => [first, ...pages])) : of([first]);
      }),
      map((pages) => pages.flatMap((p) => p.items).map((o) => this.toEvent(o, scope))),
    );
  }

  create(dto: CreateCalendarEventDto): Observable<CalendarEvent> {
    const [date, startTime] = dto.start.split('T');
    const endTime = dto.end.split('T')[1];
    const body = {
      title: dto.title,
      description: dto.description ?? null,
      date,
      startTime,
      endTime,
      recurrenceDays: this.toRecurrenceDays(dto.recurrence, date),
    };
    return this.http
      .post<{ id: string }>(this.baseUrl(dto.scope), body)
      .pipe(map(({ id }) => ({ ...dto, id, authorName: '', authorId: '' }) as CalendarEvent));
  }

  // PENDIENTE backend SCRUM-190 (edición/eliminación con auditoría) y SCRUM-195 (conversión
  // personal -> general): hasta que existan estas rutas, el backend responde 404/405.
  update(id: string, dto: UpdateCalendarEventDto): Observable<CalendarEvent> {
    return this.http.put<CalendarEvent>(`${this.baseUrl(dto.scope)}/${id}`, dto);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl('general')}/${id}`);
  }

  convertToGeneral(id: string): Observable<CalendarEvent> {
    return this.http.post<CalendarEvent>(`${this.baseUrl('personal')}/${id}/convert-to-general`, {});
  }

  private toEvent(o: OccurrenceDto, scope: CalendarScope): CalendarEvent {
    const day = o.date.slice(0, 10);
    const start = `${day}T${(o.startTime ?? '00:00:00').slice(0, 8)}`;
    const end = `${day}T${(o.endTime ?? '23:59:00').slice(0, 8)}`;
    return {
      id: o.eventId,
      title: o.title,
      description: o.description,
      start,
      end,
      authorName: o.authorName ?? (o.isPreloaded ? 'Sistema' : ''),
      authorId: o.authorUserId ?? '',
      // las plantillas precargadas (desayuno, almuerzo...) las crea el sistema y no se editan
      isRecurring: o.isPreloaded,
      recurrence: 'none',
      scope,
    };
  }

  // el backend solo modela recurrencia por días de la semana; "mensual" no existe todavía
  private toRecurrenceDays(recurrence: RecurrenceFrequency, date: string): number {
    if (recurrence === 'daily') {
      return ALL_WEEK_DAYS;
    }
    if (recurrence === 'weekly') {
      return WEEK_DAY_FLAGS[new Date(`${date}T12:00:00`).getDay()];
    }
    return 0;
  }
}
