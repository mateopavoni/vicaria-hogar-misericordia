import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { delay, Observable, of, tap } from 'rxjs';
import { LoginErrorBody, LoginErrorType, LoginRequest, LoginResponse, RegisterRequest, User } from './auth.interfaces';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);

  // JWT en memoria nada más, no localStorage.
  private token: string | null = null;
  // Signal para mantener el estado del usuario logueado
  user = signal<User | null>(null);
   
  
  /**
   * Envía la solicitud de registro al endpoint /api/auth/register
   */
  register(data: RegisterRequest): Observable<{ message: string }> {
   // return this.http.post<{ message: string }>('/api/auth/register', data);
    return of({ message: 'Registro exitoso' }).pipe(delay(1000));
  }

  /**
   * Método de inicio de sesión
*/
  login(data: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>('/api/auth/login', data).pipe(
      tap((res) => {
        this.token = res.token;
        this.user.set(res.user);
      })
    );
  }

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

  getToken(): string | null {
  return this.token;
  }

}
