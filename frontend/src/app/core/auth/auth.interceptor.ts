import { HttpClient, HttpErrorResponse, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, catchError, finalize, map, shareReplay, switchMap, tap, throwError } from 'rxjs';

import { AuthService } from './auth.service';
import { SESSION_NOTICE_KEY } from './idle-timeout.service';

interface RefreshResponse {
  token: string;
  refreshToken: string;
}

// un solo refresh a la vez: si varios pedidos fallan juntos con 401 comparten el mismo
let refreshInFlight: Observable<string> | null = null;

const withToken = (req: HttpRequest<unknown>, token: string) =>
  req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });

// agrega el token guardado en el login a todos los pedidos que van a nuestra propia API.
// Si el token venció (401) intenta renovarlo con el refresh token y reintenta el pedido;
// si no se puede (refresh vencido, cambio de rol, baja) cierra la sesión y vuelve al login.
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = localStorage.getItem('token');

  if (!token || !req.url.startsWith('/api')) {
    return next(req);
  }

  const http = inject(HttpClient);
  const authService = inject(AuthService);

  const expireSession = (error: unknown) => {
    sessionStorage.setItem(SESSION_NOTICE_KEY, 'Tu sesión venció. Iniciá sesión de nuevo.');
    authService.logout(); // limpia la sesión y redirige al login
    return throwError(() => error);
  };

  return next(withToken(req, token)).pipe(
    catchError((error: HttpErrorResponse) => {
      // los 401 de /api/auth/* son credenciales inválidas, no una sesión vencida
      if (error?.status !== 401 || req.url.startsWith('/api/auth/')) {
        return throwError(() => error);
      }

      const refreshToken = localStorage.getItem('refreshToken');
      if (!refreshToken) {
        return expireSession(error);
      }

      refreshInFlight ??= http.post<RefreshResponse>('/api/auth/refresh', { refreshToken }).pipe(
        tap(res => {
          localStorage.setItem('token', res.token);
          localStorage.setItem('refreshToken', res.refreshToken);
        }),
        map(res => res.token),
        finalize(() => (refreshInFlight = null)),
        shareReplay(1)
      );

      return refreshInFlight.pipe(
        // si falla el refresh se cierra la sesión; un error del reintento pasa tal cual
        catchError(() => expireSession(error)),
        switchMap(newToken => retry(req, next, newToken))
      );
    })
  );
};

const retry = (req: HttpRequest<unknown>, next: HttpHandlerFn, token: string) => next(withToken(req, token));
