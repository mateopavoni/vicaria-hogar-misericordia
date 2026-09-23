import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, map, tap } from 'rxjs';
import { BackendUser, LoginErrorBody, LoginErrorType, LoginRequest, LoginResponse, RegisterRequest, User } from './auth.interfaces';
import { Router } from '@angular/router';
import { UserRole } from './userRole';

// forma cruda que devuelve el backend en /api/auth/login y /api/auth/refresh
interface BackendLoginResponse {
  token: string;
  refreshToken: string;
  user: BackendUser;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  // al crear el service (o sea, al arrancar la app / recargar la página) tratamos
  // de recuperar la sesión leyendo el token guardado, así no te desloguea al refrescar
  public user = signal<User | null>(this.loadUserFromToken());

  private loadUserFromToken(): User | null {
    const token = localStorage.getItem('token');
    if (!token) {
      return null;
    }

    try {
      const payload = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')));

      // exp viene en segundos, Date.now() en milisegundos
      if (payload.exp && payload.exp * 1000 < Date.now()) {
        localStorage.removeItem('token');
        localStorage.removeItem('refreshToken');
        return null;
      }

      const fullName: string = payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ?? '';
      const [name, ...rest] = fullName.split(' ');

      return {
        id: payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'],
        name: name ?? '',
        lastname: rest.join(' '),
        email: payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'],
        role: payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ?? null,
      };
    } catch {
      // token corrupto o con formato inesperado, mejor tratarlo como si no hubiera sesión
      return null;
    }
  }

  // pasa el usuario del backend (campos en español) al shape que usa el resto del front
  private mapUser(backendUser: BackendUser): User {
    return {
      id: backendUser.id,
      name: backendUser.firstName,
      lastname: backendUser.lastName,
      email: backendUser.email,
      role: backendUser.role,
    };
  }

  register(data: RegisterRequest): Observable<{ message: string }> {
    const body = {
      firstName: data.name,
      lastName: data.lastname,
      email: data.email,
      password: data.password,
    };

    return this.http.post<{ message: string }>('/api/auth/register', body);
  }

  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.http.post<BackendLoginResponse>('/api/auth/login', credentials).pipe(
      map((res) => ({
        token: res.token,
        refreshToken: res.refreshToken,
        user: this.mapUser(res.user),
      })),
      tap((res) => {
        this.user.set(res.user);
        localStorage.setItem('token', res.token);
        localStorage.setItem('refreshToken', res.refreshToken);
      })
    );
  }

  logout(): void {
    this.user.set(null);
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    this.router.navigate(['/auth/login']);
  }

  mapError(error: HttpErrorResponse): LoginErrorType {
    if (error.status === 401) {
      return 'credentials';
    }
    const body = error.error as LoginErrorBody | undefined;
    // el backend manda "Bloqueada" (bloqueo por intentos fallidos), no "Blocked"
    if (error.status === 403 && body?.estado === 'Bloqueada') {
      return 'blocked';
    }
    if (error.status === 403 && body?.estado === 'Pending') {
      return 'pending';
    }
    return 'unknown';
  }

  // Helper para cambiar de rol rápido desde pruebas manuales, no toca el backend.
  setMockRole(role: UserRole | null): void {
    this.user.update((currentUser) =>
      currentUser ? { ...currentUser, role } : null
    );
  }
}
