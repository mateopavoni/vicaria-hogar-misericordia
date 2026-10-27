import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { errorOf } from '../../../shared/testing/error-of';
import { CaritasReportService } from './caritas-report.service';

describe('CaritasReportService', () => {
  let service: CaritasReportService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(CaritasReportService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('pide el informe del período a la API', () => {
    service.generate('trimestral', '2026-09-30').subscribe();
    const req = httpMock.expectOne((r) => r.url === '/api/caritas-reports');
    expect(req.request.params.get('type')).toBe('trimestral');
    expect(req.request.params.get('to')).toBe('2026-09-30');
    req.flush({});
  });

  it('rechaza una fecha de corte inválida sin llamar a la API', async () => {
    const err = await errorOf(service.generate('trimestral', '30/09/2026'));
    expect(err.error.message).toContain('fecha de corte');
  });
});
