import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {ManagedUser, UserStatus,ApproveUserRequest, RejectUserRequest} from '../interfaces/user.interface';
import { UsersFilters } from '../interfaces/UsersFilters.interface';
import { UserRole } from '../../../core/auth/userRole';

export interface UsersResponse {
  items: ManagedUser[];
  total: number;
  totalPages: number;
}


@Injectable({
  providedIn: 'root'
})
export class UsersService {

  private http = inject(HttpClient);

  private readonly apiUrl = '/api/users';


  getUsers(status: UserStatus,page: number,filters?: UsersFilters): Observable<UsersResponse> {

  let params = new HttpParams()
    .set('status', status)
    .set('page', page);

  if (filters?.dateFrom) {
    params = params.set('dateFrom', filters.dateFrom);
  }

  if (filters?.dateTo) {
    params = params.set('dateTo', filters.dateTo);
  }

  return this.http.get<UsersResponse>(
    this.apiUrl,
    { params }
  );
  
}




  approveUser(userId: string,data: ApproveUserRequest): Observable<void> {

    return this.http.patch<void>(
      `${this.apiUrl}/${userId}/approve`,
      data
    );
  }


  rejectUser(userId: string, data: RejectUserRequest): Observable<void> {

    return this.http.patch<void>(
      `${this.apiUrl}/${userId}/reject`,
      data
    );
  }


  deactivateUser(userId: string): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${userId}/deactivate`, {});
  }

  reactivateUser(userId: string): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${userId}/reactivate`, {});
  }
  
  updateRole(userId: string, role: UserRole): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${userId}/role`, { role });
  }

}