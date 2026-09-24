import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { AuthService } from './auth.service';

describe('AuthService.mapError', () => {
  let service: AuthService;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient()] });
    service = TestBed.inject(AuthService);
  });

  it('mapea 401 a credentials', () => {
    const error = new HttpErrorResponse({ status: 401 });
    expect(service.mapError(error)).toBe('credentials');
  });

  // bug reportado 2026-09-23: este test comparaba contra "estado", el nombre de campo
  // que el frontend (erróneamente) esperaba — no el que el backend realmente manda
  // (AuthController.Login responde { status: ... }). El test pasaba igual porque
  // coincidía con el bug, no con el contrato real; se corrige junto con auth.service.ts.
  it('mapea 403 con status Bloqueada a blocked', () => {
    const error = new HttpErrorResponse({ status: 403, error: { status: 'Bloqueada' } });
    expect(service.mapError(error)).toBe('blocked');
  });

  it('mapea 403 con status Pending a pending', () => {
    const error = new HttpErrorResponse({ status: 403, error: { status: 'Pending' } });
    expect(service.mapError(error)).toBe('pending');
  });

  it('mapea cualquier otro caso a unknown', () => {
    const error = new HttpErrorResponse({ status: 500 });
    expect(service.mapError(error)).toBe('unknown');
  });
});
