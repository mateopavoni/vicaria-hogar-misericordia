import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import {
  Collaborator,
  CollaboratorType,
  CreateCollaboratorDto,
  UpdateCollaboratorDto,
} from '../interfaces/collaborator.interface';

// forma real de GET api/collaborators (CollaboratorListItemDto)
interface CollaboratorListItem {
  id: string;
  firstName: string;
  lastName: string | null;
  dni: string | null;
  phone: string | null;
  email: string | null;
  type: number;
  workArea: string | null;
  registeredAt: string;
  registeredByName: string | null;
}

// el backend serializa CollaboratorType como número (0 = Volunteer, 1 = Employee)
const TYPE_BY_NUMBER: CollaboratorType[] = [CollaboratorType.Volunteer, CollaboratorType.Employee];

@Injectable({
  providedIn: 'root',
})
export class CollaboratorsService {
  private http = inject(HttpClient);
  private apiUrl = '/api/collaborators';

  // listado completo; la búsqueda por texto y el filtro por tipo se resuelven en pantalla.
  // El backend todavía no modela baja lógica (SCRUM-200), por eso todos se muestran activos.
  getAll(): Observable<Collaborator[]> {
    return this.http
      .get<CollaboratorListItem[]>(this.apiUrl)
      .pipe(map((rows) => rows.map((r) => this.toCollaborator(r))));
  }

  create(dto: CreateCollaboratorDto): Observable<Collaborator> {
    const body = { ...dto, type: dto.type === CollaboratorType.Employee ? 1 : 0 };
    return this.http.post<{ id: string }>(this.apiUrl, body).pipe(
      map(
        ({ id }) =>
          ({ ...dto, id, isActive: true, createdAt: new Date().toISOString(), createdByName: '' }) as Collaborator,
      ),
    );
  }

  // PENDIENTE backend SCRUM-200 (edición y baja lógica): hasta entonces responden 404/405
  update(id: string, dto: UpdateCollaboratorDto): Observable<Collaborator> {
    return this.http.put<Collaborator>(`${this.apiUrl}/${id}`, dto);
  }

  toggleActive(id: string, isActive: boolean): Observable<Collaborator> {
    return this.http.patch<Collaborator>(`${this.apiUrl}/${id}/status`, { isActive });
  }

  private toCollaborator(r: CollaboratorListItem): Collaborator {
    return {
      id: r.id,
      firstName: r.firstName,
      lastName: r.lastName,
      dni: r.dni,
      phone: r.phone,
      email: r.email,
      type: TYPE_BY_NUMBER[r.type] ?? null,
      workArea: r.workArea,
      isActive: true,
      createdAt: r.registeredAt,
      createdByName: r.registeredByName ?? '',
    };
  }
}
