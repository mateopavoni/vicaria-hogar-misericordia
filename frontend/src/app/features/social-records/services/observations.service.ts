import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { Observation, CreateObservationDto } from '../interfaces/observation.interface';

// forma real de la respuesta del backend (ObservationResponseDto): personId en vez de
// socialRecordId, y authorName en vez de createdBy
interface ObservationResponse {
  id: string;
  personId: string;
  content: string;
  categoryId: string | null;
  categoryName: string | null;
  authorUserId: string;
  authorName: string;
  createdAt: string;
}

interface ObservationsTimelineResponse {
  items: ObservationResponse[];
  totalCount: number;
}

function toObservation(response: ObservationResponse): Observation {
  return {
    id: response.id,
    socialRecordId: response.personId,
    categoryId: response.categoryId ?? '',
    categoryName: response.categoryName ?? '',
    content: response.content,
    createdBy: response.authorName,
    createdAt: response.createdAt,
  };
}

@Injectable({
  providedIn: 'root'
})
export class ObservationsService {
  private http = inject(HttpClient);

  /**
   * Registra una nueva observación para una persona/ficha.
   * Fecha, hora y autor son asignados automáticamente por el backend.
   */
  create(personId: string, dto: CreateObservationDto): Observable<Observation> {
    return this.http
      .post<ObservationResponse>(`/api/persons/${personId}/observations`, {
        content: dto.content,
        categoryId: dto.categoryId,
      })
      .pipe(map(toObservation));
  }

  // timeline de observaciones de una persona (SCRUM-10)
  getByPersonId(personId: string): Observable<Observation[]> {
    return this.http
      .get<ObservationsTimelineResponse>(`/api/persons/${personId}/observations`)
      .pipe(map((response) => response.items.map(toObservation)));
  }
}
