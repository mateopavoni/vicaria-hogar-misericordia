import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ObservationCategory, CreateCategoryDto, UpdateCategoryDto } from '../interfaces/observation-category.interface';

@Injectable({
  providedIn: 'root'
})
export class ObservationCategoriesService {
  private http = inject(HttpClient);
  private apiUrl = '/api/observation-categories';

  // Para administradores (Referentes): Trae todas (activas e inactivas)
  getAll(): Observable<ObservationCategory[]> {
    return this.http.get<ObservationCategory[]>(this.apiUrl);
  }

  // Para selectores (SCRUM-181): Trae solo categorías activas
  getActive(): Observable<ObservationCategory[]> {
    return this.http.get<ObservationCategory[]>(`${this.apiUrl}/active`);
  }

  create(dto: CreateCategoryDto): Observable<ObservationCategory> {
    return this.http.post<ObservationCategory>(this.apiUrl, dto);
  }

  update(id: string, dto: UpdateCategoryDto): Observable<ObservationCategory> {
    return this.http.put<ObservationCategory>(`${this.apiUrl}/${id}`, dto);
  }

  toggleActive(id: string, isActive: boolean): Observable<ObservationCategory> {
    return this.http.patch<ObservationCategory>(`${this.apiUrl}/${id}/toggle-status`, { isActive });
  }
}