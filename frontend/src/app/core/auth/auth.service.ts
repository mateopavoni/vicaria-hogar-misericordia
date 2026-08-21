import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { LoginErrorBody, LoginErrorTipo, LoginRequest, LoginResponse, Usuario } from './auth.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);

  // JWT en memoria nada más, no localStorage.
  private token: string | null = null;
  usuario = signal<Usuario | null>(null);

  login(data: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>('/api/auth/login', data).pipe(
      tap((res) => {
        this.token = res.token;
        this.usuario.set(res.usuario);
      })
    );
  }

  // Traduce el error HTTP a algo que el componente entienda sin conocer status codes.
  mapearError(error: HttpErrorResponse): LoginErrorTipo {
    if (error.status === 401) {
      return 'credenciales';
    }
    const body = error.error as LoginErrorBody | undefined;
    if (error.status === 403 && body?.estado === 'Bloqueada') {
      return 'bloqueada';
    }
    if (error.status === 403 && body?.estado === 'Pending') {
      return 'pendiente';
    }
    return 'desconocido';
  }
}
