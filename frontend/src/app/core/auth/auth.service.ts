import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { LoginErrorBody, LoginErrorType, LoginRequest, LoginResponse, User } from './auth.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);

  // JWT en memoria nada más, no localStorage.
  private token: string | null = null;
  user = signal<User | null>(null);

  login(data: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>('/api/auth/login', data).pipe(
      tap((res) => {
        this.token = res.token;
        this.user.set(res.user);
      })
    );
  }

  // status code -> tipo de error
  mapError(error: HttpErrorResponse): LoginErrorType {
    if (error.status === 401) {
      return 'credentials';
    }
    const body = error.error as LoginErrorBody | undefined;
    if (error.status === 403 && body?.estado === 'Bloqueada') {
      return 'blocked';
    }
    if (error.status === 403 && body?.estado === 'Pending') {
      return 'pending';
    }
    return 'unknown';
  }
}
