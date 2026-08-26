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

// id fijo de cada rol en la base (seed determinístico, ver RoleConfiguration en el backend)
const ROLE_IDS: Record<UserRole, string> = {
  Referente: '11111111-1111-1111-1111-111111111111',
  DirectoraDeCasona: '22222222-2222-2222-2222-222222222222',
  Escucha: '33333333-3333-3333-3333-333333333333',
};

// forma cruda que devuelve GET /api/auth/users/pending
interface BackendPendingUser {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  requestDate: string;
}

// forma cruda que devuelven GET /api/auth/users/active y /users/inactive
interface BackendManagedUser {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  role: UserRole | null;
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
            name: u.firstName,
            lastname: u.lastName,
            email: u.email,
            requestDate: u.requestDate,
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
          name: u.firstName,
          lastname: u.lastName,
          email: u.email,
          requestDate: '',
          status,
          role: u.role,
        }));
        return { items, total: items.length, totalPages: 1 };
      })
    );
  }

  approveUser(userId: string, data: ApproveUserRequest): Observable<void> {
    return this.http.post<void>(
      `${this.apiUrl}/users/${userId}/approve`,
      { roleId: ROLE_IDS[data.role] }
    );
  }

  rejectUser(userId: string, data: RejectUserRequest): Observable<void> {
    return this.http.post<void>(
      `${this.apiUrl}/users/${userId}/reject`,
      { reason: data.reason }
    );
  }

  deactivateUser(userId: string): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/users/${userId}/deactivate`, {});
  }

  reactivateUser(userId: string): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/users/${userId}/reactivate`, {});
  }

  updateRole(userId: string, role: UserRole): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/users/${userId}/role`, { roleId: ROLE_IDS[role] });
  }

}
