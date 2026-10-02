import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Collaborator, CreateCollaboratorDto, UpdateCollaboratorDto } from '../interfaces/collaborator.interface';

@Injectable({
  providedIn: 'root',
})
export class CollaboratorsService {
  private http = inject(HttpClient);
  private apiUrl = '/api/collaborators';

  // trae todos (activos e inactivos) — la pantalla de gestión necesita ambos grupos
  getAll(): Observable<Collaborator[]> {
    return this.http.get<Collaborator[]>(this.apiUrl, { params: new HttpParams().set('onlyActive', false) });
  }

  create(dto: CreateCollaboratorDto): Observable<Collaborator> {
    return this.http.post<Collaborator>(this.apiUrl, dto);
  }

  update(id: string, dto: UpdateCollaboratorDto): Observable<Collaborator> {
    return this.http.put<Collaborator>(`${this.apiUrl}/${id}`, dto);
  }

  toggleActive(id: string, isActive: boolean): Observable<Collaborator> {
    return this.http.patch<Collaborator>(`${this.apiUrl}/${id}/status`, { isActive });
  }
}
