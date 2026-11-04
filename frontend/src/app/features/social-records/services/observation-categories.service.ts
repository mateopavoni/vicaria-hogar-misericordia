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

  // para la pantalla de configuración (Referente): trae activas e inactivas
  getAll(): Observable<ObservationCategory[]> {
    return this.http.get<ObservationCategory[]>(this.apiUrl, { params: new HttpParams().set('onlyActive', false) });
  }

  // para los selectores: trae solo las categorías activas
  getActive(): Observable<ObservationCategory[]> {
    return this.http.get<ObservationCategory[]>(this.apiUrl, { params: new HttpParams().set('onlyActive', true) });
  }

  create(dto: CreateCategoryDto): Observable<ObservationCategory> {
    return this.http.post<ObservationCategory>(this.apiUrl, dto);
  }

  update(id: string, dto: UpdateCategoryDto): Observable<ObservationCategory> {
    return this.http.put<ObservationCategory>(`${this.apiUrl}/${id}`, dto);
  }

  // activa o desactiva una categoría; el backend responde 204 sin cuerpo
  toggleActive(id: string, isActive: boolean): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/status`, { isActive });
  }

  // Solo se puede eliminar una categoría sin observaciones; si tiene, el backend
  // responde 409 con { message } ("Debe desactivarla").
  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}