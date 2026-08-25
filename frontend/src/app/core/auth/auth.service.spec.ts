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

  it('mapea 403 con estado Bloqueada a blocked', () => {
    const error = new HttpErrorResponse({ status: 403, error: { estado: 'Bloqueada' } });
    expect(service.mapError(error)).toBe('blocked');
  });

  it('mapea 403 con estado Pending a pending', () => {
    const error = new HttpErrorResponse({ status: 403, error: { estado: 'Pending' } });
    expect(service.mapError(error)).toBe('pending');
  });

  it('mapea cualquier otro caso a unknown', () => {
    const error = new HttpErrorResponse({ status: 500 });
    expect(service.mapError(error)).toBe('unknown');
  });
});
