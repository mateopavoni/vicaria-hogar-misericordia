import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
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

// forma cruda que devuelve GET /api/auth/users/pending: { items, total, totalPages }
interface BackendPendingUser {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  requestDate: string;
}

// forma cruda que devuelven GET /api/auth/users/active y /users/inactive: { items, total, totalPages }
interface BackendManagedUser {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  role: UserRole | null;
}

interface BackendPagedResult<T> {
  items: T[];
  total: number;
  totalPages: number;
}

@Injectable({
  providedIn: 'root'
})
export class UsersService {

  private http = inject(HttpClient);

  private readonly apiUrl = '/api/auth';

  getUsers(status: UserStatus, page: number, filters?: UsersFilters): Observable<UsersResponse> {
    let params = new HttpParams().set('page', page);
    if (filters?.dateFrom) params = params.set('dateFrom', filters.dateFrom);
    if (filters?.dateTo) params = params.set('dateTo', filters.dateTo);

    if (status === 'Pending') {
      return this.http.get<BackendPagedResult<BackendPendingUser>>(`${this.apiUrl}/users/pending`, { params }).pipe(
        map((res) => ({
          items: res.items.map((u) => ({
            id: u.id,
            name: u.firstName,
            lastname: u.lastName,
            email: u.email,
            requestDate: u.requestDate,
            status: 'Pending' as const,
            role: null,
          })),
          total: res.total,
          totalPages: res.totalPages,
        }))
      );
    }

    // Approved -> activos, Suspended -> inactivos/desactivados
    const endpoint = status === 'Approved' ? 'active' : 'inactive';

    return this.http.get<BackendPagedResult<BackendManagedUser>>(`${this.apiUrl}/users/${endpoint}`, { params }).pipe(
      map((res) => ({
        items: res.items.map((u) => ({
          id: u.id,
          name: u.firstName,
          lastname: u.lastName,
          email: u.email,
          requestDate: '',
          status,
          role: u.role,
        })),
        total: res.total,
        totalPages: res.totalPages,
      }))
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
