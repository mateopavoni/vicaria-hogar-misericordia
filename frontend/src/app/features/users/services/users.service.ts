import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, Observable, of } from 'rxjs';
import { ManagedUser, UserStatus, ApproveUserRequest, RejectUserRequest } from '../interfaces/user.interface';
import { UsersFilters } from '../interfaces/UsersFilters.interface';
import { UserRole } from '../../../core/auth/userRole';

export interface UsersResponse {
  items: ManagedUser[];
  total: number;
  totalPages: number;
}

// id fijo de cada rol en la base (seed determinístico, ver RolConfiguration en el backend)
const ROLE_IDS: Record<UserRole, string> = {
  Referente: '11111111-1111-1111-1111-111111111111',
  DirectoraDeCasona: '22222222-2222-2222-2222-222222222222',
  Escucha: '33333333-3333-3333-3333-333333333333',
};

// forma cruda que devuelve GET /api/auth/users/pending
interface BackendPendingUser {
  id: string;
  nombre: string;
  apellido: string;
  email: string;
  fechaSolicitud: string;
}

// forma cruda que devuelven GET /api/auth/users/active y /users/inactive
interface BackendManagedUser {
  id: string;
  nombre: string;
  apellido: string;
  email: string;
  rol: UserRole | null;
}

@Injectable({
  providedIn: 'root'
})
export class UsersService {

  private http = inject(HttpClient);

  private readonly apiUrl = '/api/auth';

  getUsers(status: UserStatus, page: number, filters?: UsersFilters): Observable<UsersResponse> {

    if (status === 'Pending') {
      return this.http.get<BackendPendingUser[]>(`${this.apiUrl}/users/pending`).pipe(
        map((users) => {
          const items: ManagedUser[] = users.map((u) => ({
            id: u.id,
            name: u.nombre,
            lastname: u.apellido,
            email: u.email,
            requestDate: u.fechaSolicitud,
            status: 'Pending',
            role: null,
          }));
          return { items, total: items.length, totalPages: 1 };
        })
      );
    }

    // Approved -> activos, Suspended -> inactivos/desactivados
    const endpoint = status === 'Approved' ? 'active' : 'inactive';

    return this.http.get<BackendManagedUser[]>(`${this.apiUrl}/users/${endpoint}`).pipe(
      map((users) => {
        const items: ManagedUser[] = users.map((u) => ({
          id: u.id,
          name: u.nombre,
          lastname: u.apellido,
          email: u.email,
          requestDate: '',
          status,
          role: u.rol,
        }));
        return { items, total: items.length, totalPages: 1 };
      })
    );
  }

  approveUser(userId: string, data: ApproveUserRequest): Observable<void> {
    return this.http.post<void>(
      `${this.apiUrl}/users/${userId}/approve`,
      { rolId: ROLE_IDS[data.role] }
    );
  }

  rejectUser(userId: string, data: RejectUserRequest): Observable<void> {
    return this.http.post<void>(
      `${this.apiUrl}/users/${userId}/reject`,
      { motivo: data.reason }
    );
  }

  deactivateUser(userId: string): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/users/${userId}/deactivate`, {});
  }

  reactivateUser(userId: string): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/users/${userId}/reactivate`, {});
  }

  updateRole(userId: string, role: UserRole): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/users/${userId}/role`, { rolId: ROLE_IDS[role] });
  }

}
