import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, tap, throwError } from 'rxjs';
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
  isActive: boolean;
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

  // último listado consultado: el PUT es una edición completa y responde 204, así que la baja
  // reenvía los demás campos y devuelve el colaborador actualizado a partir de esto.
  private known = new Map<string, Collaborator>();

  // listado completo; la búsqueda por texto y el filtro por tipo se resuelven en pantalla.
  getAll(): Observable<Collaborator[]> {
    return this.http
      .get<CollaboratorListItem[]>(this.apiUrl)
      .pipe(
        map((rows) => rows.map((r) => this.toCollaborator(r))),
        tap((list) => list.forEach((c) => this.known.set(c.id, c))),
      );
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

  // PUT api/collaborators/{id} (SCRUM-200): edición completa; incluye isActive, que registra
  // la baja o reactivación en la auditoría. Responde 204 sin cuerpo.
  update(id: string, dto: UpdateCollaboratorDto): Observable<Collaborator> {
    const current = this.known.get(id);
    const isActive = dto.isActive ?? current?.isActive ?? true;
    const body = { ...dto, isActive, type: dto.type === CollaboratorType.Employee ? 1 : 0 };
    return this.http.put<void>(`${this.apiUrl}/${id}`, body).pipe(
      map(() => {
        const updated = { ...(current ?? { id }), ...dto, isActive } as Collaborator;
        this.known.set(id, updated);
        return updated;
      }),
    );
  }

  // la baja lógica es un PUT con isActive; se reenvían los demás campos del colaborador
  toggleActive(id: string, isActive: boolean): Observable<Collaborator> {
    const current = this.known.get(id);
    if (!current) {
      return throwError(() => new Error('Colaborador desconocido: recargá el listado.'));
    }
    return this.update(id, { ...current, isActive } as UpdateCollaboratorDto);
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
      isActive: r.isActive,
      createdAt: r.registeredAt,
      createdByName: r.registeredByName ?? '',
    };
  }
}
