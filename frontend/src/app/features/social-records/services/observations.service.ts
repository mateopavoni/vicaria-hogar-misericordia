import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Observation, CreateObservationDto } from '../interfaces/observation.interface';

@Injectable({
  providedIn: 'root'
})
export class ObservationsService {
  private http = inject(HttpClient);
  private apiUrl = '/api/observations'; // Ajustar según el endpoint de tu backend

  /**
   * Registra una nueva observación para una persona/ficha.
   * Fecha, hora y autor son asignados automáticamente por el backend.
   */
  create(dto: CreateObservationDto): Observable<Observation> {
    return this.http.post<Observation>(this.apiUrl, dto);
  }
}