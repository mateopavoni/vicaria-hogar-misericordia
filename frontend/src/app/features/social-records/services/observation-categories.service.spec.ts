import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { ObservationCategoriesService } from './observation-categories.service';

// bug reportado 2026-09-23: getActive() apuntaba a GET /api/observation-categories/active
// (ruta inexistente, el modal de "crear observación" fallaba en 404 y el combo de
// categorías quedaba siempre vacío) y toggleActive() apuntaba a .../toggle-status en vez
// de .../status. Este spec fija el contrato real contra el backend.
describe('ObservationCategoriesService', () => {
  let service: ObservationCategoriesService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ObservationCategoriesService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getActive() llama a la ruta base con onlyActive=true, no a /active', () => {
    service.getActive().subscribe();

    const req = httpMock.expectOne(
      (r) => r.url === '/api/observation-categories' && r.params.get('onlyActive') === 'true'
    );
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('getAll() llama a la ruta base con onlyActive=false', () => {
    service.getAll().subscribe();

    const req = httpMock.expectOne(
      (r) => r.url === '/api/observation-categories' && r.params.get('onlyActive') === 'false'
    );
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('toggleActive() llama a .../status, no a .../toggle-status', () => {
    service.toggleActive('cat-1', false).subscribe();

    const req = httpMock.expectOne('/api/observation-categories/cat-1/status');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ isActive: false });
    req.flush({});
  });
});
