import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CalendarEvent,
  CalendarScope,
  CreateCalendarEventDto,
  UpdateCalendarEventDto,
} from '../interfaces/calendar-event.interface';

@Injectable({
  providedIn: 'root',
})
export class CalendarEventsService {
  private http = inject(HttpClient);
  private apiUrl = '/api/eventos';

  // rango visible (semana o mes); fechas en formato YYYY-MM-DD (ver SCRUM-186).
  // scope=personal (SCRUM-17): se asume que el backend filtra server-side por el
  // usuario autenticado, nunca por un id que mande el frontend.
  getByRange(desde: string, hasta: string, scope: CalendarScope): Observable<CalendarEvent[]> {
    return this.http.get<CalendarEvent[]>(this.apiUrl, {
      params: new HttpParams().set('desde', desde).set('hasta', hasta).set('scope', scope),
    });
  }

  create(dto: CreateCalendarEventDto): Observable<CalendarEvent> {
    return this.http.post<CalendarEvent>(this.apiUrl, dto);
  }

  update(id: string, dto: UpdateCalendarEventDto): Observable<CalendarEvent> {
    return this.http.put<CalendarEvent>(`${this.apiUrl}/${id}`, dto);
  }

  // SCRUM-16/SCRUM-192: eliminar un evento (con confirmación previa en la UI).
  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  // SCRUM-17 (AC): "el usuario puede convertir un evento personal en general si lo
  // decide posteriormente". Contrato asumido — endpoint dedicado en vez de un PUT
  // completo, porque esto no es "editar el evento", es un cambio de visibilidad que
  // el backend debería auditar aparte (quién lo hizo público y cuándo).
  convertToGeneral(id: string): Observable<CalendarEvent> {
    return this.http.patch<CalendarEvent>(`${this.apiUrl}/${id}/scope`, { scope: 'general' as CalendarScope });
  }
}
