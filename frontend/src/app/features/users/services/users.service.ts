import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import {
  ManagedUser,
  UserStatus,
  ApproveUserRequest,
  RejectUserRequest
} from '../interfaces/user.interface';

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


  getUsers(
    status: UserStatus,
    page: number
  ): Observable<UsersResponse> {

    let params = new HttpParams()
      .set('status', status)
      .set('page', page);

    return this.http.get<UsersResponse>(
      this.apiUrl,
      { params }
    );
  }


  approveUser(
    userId: string,
    data: ApproveUserRequest
  ): Observable<void> {

    return this.http.patch<void>(
      `${this.apiUrl}/${userId}/approve`,
      data
    );
  }


  rejectUser(
    userId: string,
    data: RejectUserRequest
  ): Observable<void> {

    return this.http.patch<void>(
      `${this.apiUrl}/${userId}/reject`,
      data
    );
  }

}