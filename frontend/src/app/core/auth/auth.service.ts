import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { delay, Observable, of, tap } from 'rxjs';
import { LoginErrorBody, LoginErrorType, LoginRequest, LoginResponse, RegisterRequest, User } from './auth.interfaces';
import { Router } from '@angular/router';
import { UserRole } from './userRole';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  // JWT en memoria nada más, no localStorage.
  private token: string | null = null;
  // Signal para mantener el estado del usuario logueado
  // user = signal<User | null>(null);
   
  
  // /Envía la solicitud de registro al endpoint /api/auth/register
  register(data: RegisterRequest): Observable<{ message: string }> {
   // return this.http.post<{ message: string }>('/api/auth/register', data);
    return of({ message: 'Registro exitoso' }).pipe(delay(1000));
  }

  // inicio de sesion

  // login(data: LoginRequest): Observable<LoginResponse> {
  //   return this.http.post<LoginResponse>('/api/auth/login', data).pipe(
  //     tap((res) => {
  //       this.token = res.token;
  //       this.user.set(res.user);
  //     })
  //   );
  // }

  mapError(error: HttpErrorResponse): LoginErrorType {
    if (error.status === 401) {
      return 'credentials';
    }
    const body = error.error as LoginErrorBody | undefined;
    if (error.status === 403 && body?.estado === 'Blocked') {
      return 'blocked';
    }
    if (error.status === 403 && body?.estado === 'Pending') {
      return 'pending';
    }
    return 'unknown';
  }

  // getToken(): string | null {
  // return this.token;
  // }

// 1. Estado inicial simulado: Cámbialo aquí directamente para probar vistas ('Referente', 'Admin', 'User', etc.)
  public user = signal<User | null>({
    id: '1',
    name: 'Juan',
    lastname: 'Pérez',
    email: 'test@ejemplo.com',
    role: 'Referente', // <-- Cambia este valor para probar roles
  }
);

  // 2. Método de login simulado que retorna un Observable matching con tu LoginResponse
  login(credentials: LoginRequest): Observable<LoginResponse> {

    const currentRole = this.user()?.role || 'Referente';

    const mockUser: User = {
      id: '1',
      name: 'Juan',
      lastname: 'Pérez',
      email: credentials.email || 'test@ejemplo.com',
      role: currentRole, // <-- AQUÍ SE ASIGNA LA VARIABLE currentRole// <-- Rol por defecto asignado al loguearse
    };

    const mockResponse: LoginResponse = {
      token: 'jwt-mock-token-xyz-123',
      user: mockUser,
    };

    // Retorna la respuesta simulada con un retardo para imitar la red
    return of(mockResponse).pipe(
      delay(500),
      tap((res) => {
        this.user.set(res.user);
        localStorage.setItem('token', res.token);
      })
    );
  }

  // 3. Método para cerrar sesión
    logout(): void {
  // Limpia el estado reactivo del usuario
  this.user.set(null);
  
  // Elimina la sesión simulada del almacenamiento del navegador
  localStorage.removeItem('token');

  // Redirige automáticamente a la pantalla de login
  this.router.navigate(['/login']);
}

  // 4. Helper opcional para cambiar de rol rápidamente desde el código de pruebas
  setMockRole(role: UserRole | null): void {
    this.user.update((currentUser) =>
      currentUser ? { ...currentUser, role } : null
    );
  }

  

}
