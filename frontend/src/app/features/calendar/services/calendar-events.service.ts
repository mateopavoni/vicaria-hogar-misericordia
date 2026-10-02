import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CalendarEvent, CreateCalendarEventDto, UpdateCalendarEventDto } from '../interfaces/calendar-event.interface';

@Injectable({
  providedIn: 'root',
})
export class CalendarEventsService {
  private http = inject(HttpClient);
  private apiUrl = '/api/eventos';

  // rango visible (semana o mes); fechas en formato YYYY-MM-DD (ver SCRUM-186)
  getByRange(desde: string, hasta: string): Observable<CalendarEvent[]> {
    return this.http.get<CalendarEvent[]>(this.apiUrl, {
      params: new HttpParams().set('desde', desde).set('hasta', hasta),
    });
  }

  create(dto: CreateCalendarEventDto): Observable<CalendarEvent> {
    return this.http.post<CalendarEvent>(this.apiUrl, dto);
  }

  update(id: string, dto: UpdateCalendarEventDto): Observable<CalendarEvent> {
    return this.http.put<CalendarEvent>(`${this.apiUrl}/${id}`, dto);
  }
}
