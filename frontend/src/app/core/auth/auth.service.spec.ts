import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { AuthService } from './auth.service';

describe('AuthService.mapearError', () => {
  let service: AuthService;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient()] });
    service = TestBed.inject(AuthService);
  });

  it('mapea 401 a credenciales', () => {
    const error = new HttpErrorResponse({ status: 401 });
    expect(service.mapearError(error)).toBe('credenciales');
  });

  it('mapea 403 con estado Bloqueada a bloqueada', () => {
    const error = new HttpErrorResponse({ status: 403, error: { estado: 'Bloqueada' } });
    expect(service.mapearError(error)).toBe('bloqueada');
  });

  it('mapea 403 con estado Pending a pendiente', () => {
    const error = new HttpErrorResponse({ status: 403, error: { estado: 'Pending' } });
    expect(service.mapearError(error)).toBe('pendiente');
  });

  it('mapea cualquier otro caso a desconocido', () => {
    const error = new HttpErrorResponse({ status: 500 });
    expect(service.mapearError(error)).toBe('desconocido');
  });
});
