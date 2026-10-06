import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';

import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  const logout = vi.fn();

  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    logout.mockReset();

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: { logout } },
        { provide: Router, useValue: { navigate: vi.fn() } },
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('agrega el token a los pedidos de la API', () => {
    localStorage.setItem('token', 'abc');
    http.get('/api/notifications').subscribe();

    const req = httpMock.expectOne('/api/notifications');
    expect(req.request.headers.get('Authorization')).toBe('Bearer abc');
    req.flush([]);
  });

  it('ante un 401 renueva el token y reintenta el pedido', () => {
    localStorage.setItem('token', 'viejo');
    localStorage.setItem('refreshToken', 'r1');
    let result: unknown;
    http.get('/api/notifications').subscribe(r => (result = r));

    httpMock.expectOne('/api/notifications').flush(null, { status: 401, statusText: 'Unauthorized' });

    const refresh = httpMock.expectOne('/api/auth/refresh');
    expect(refresh.request.body).toEqual({ refreshToken: 'r1' });
    refresh.flush({ token: 'nuevo', refreshToken: 'r2' });

    const retry = httpMock.expectOne('/api/notifications');
    expect(retry.request.headers.get('Authorization')).toBe('Bearer nuevo');
    retry.flush(['ok']);

    expect(result).toEqual(['ok']);
    expect(localStorage.getItem('refreshToken')).toBe('r2');
    expect(logout).not.toHaveBeenCalled();
  });

  it('si falla el refresh cierra la sesión', () => {
    localStorage.setItem('token', 'viejo');
    localStorage.setItem('refreshToken', 'r1');
    http.get('/api/notifications').subscribe({ error: () => undefined });

    httpMock.expectOne('/api/notifications').flush(null, { status: 401, statusText: 'Unauthorized' });
    httpMock.expectOne('/api/auth/refresh').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(logout).toHaveBeenCalled();
  });

  it('un 401 de /api/auth/* (credenciales inválidas) no intenta renovar ni cierra sesión', () => {
    localStorage.setItem('token', 'abc');
    http.post('/api/auth/login', {}).subscribe({ error: () => undefined });

    httpMock.expectOne('/api/auth/login').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(logout).not.toHaveBeenCalled();
  });
});
