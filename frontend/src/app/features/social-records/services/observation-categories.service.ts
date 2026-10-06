import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ObservationCategory, CreateCategoryDto, UpdateCategoryDto } from '../interfaces/observation-category.interface';

@Injectable({
  providedIn: 'root'
})
export class ObservationCategoriesService {
  private http = inject(HttpClient);
  private apiUrl = '/api/observation-categories';

  // Para administradores (Referentes): Trae todas (activas e inactivas).
  // Bug reportado 2026-09-23: llamaba la misma ruta que getActive() sin distinguir,
  // así que el default del backend (onlyActive=true) también le ocultaba las inactivas.
  getAll(): Observable<ObservationCategory[]> {
    return this.http.get<ObservationCategory[]>(this.apiUrl, { params: new HttpParams().set('onlyActive', false) });
  }

  // Para selectores (SCRUM-181): trae solo categorías activas.
  // Bug reportado 2026-09-23: apuntaba a GET .../active, ruta que no existe en el backend
  // (ObservationCategoriesController solo tiene GET / con el query param onlyActive) — el
  // combo de categorías del modal de "crear observación" siempre fallaba en 404.
  getActive(): Observable<ObservationCategory[]> {
    return this.http.get<ObservationCategory[]>(this.apiUrl, { params: new HttpParams().set('onlyActive', true) });
  }

  create(dto: CreateCategoryDto): Observable<ObservationCategory> {
    return this.http.post<ObservationCategory>(this.apiUrl, dto);
  }

  update(id: string, dto: UpdateCategoryDto): Observable<ObservationCategory> {
    return this.http.put<ObservationCategory>(`${this.apiUrl}/${id}`, dto);
  }

  // Bug reportado 2026-09-23: apuntaba a .../toggle-status; la ruta real del backend es .../status
  // (ObservationCategoriesController.ToggleStatus, [HttpPatch("{id:guid}/status")]).
  toggleActive(id: string, isActive: boolean): Observable<ObservationCategory> {
    return this.http.patch<ObservationCategory>(`${this.apiUrl}/${id}/status`, { isActive });
  }
}