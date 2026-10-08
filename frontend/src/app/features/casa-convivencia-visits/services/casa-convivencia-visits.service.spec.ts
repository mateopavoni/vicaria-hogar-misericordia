import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { CasaConvivenciaVisitsService } from './casa-convivencia-visits.service';
import { Visit } from '../interfaces/visit.interface';

// traduce api/casona-visits (estado numérico, date + startTime) a la interfaz Visit del frontend
describe('CasaConvivenciaVisitsService', () => {
  let service: CasaConvivenciaVisitsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(CasaConvivenciaVisitsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  const item = {
    id: 'v1',
    personId: 'p1',
    personName: 'Lucía Fernández',
    visitorName: 'Madre',
    date: '2026-10-12T00:00:00',
    startTime: '16:00:00',
    estimatedDurationMinutes: 45,
    status: 0,
    cancellationReason: null,
  };

  function load(): Visit[] {
    let result: Visit[] = [];
    service.getByRange('2026-10-12', '2026-10-18').subscribe((r) => (result = r));
    httpMock
      .expectOne((r) => r.url === '/api/casona-visits' && r.params.get('from') === '2026-10-12')
      .flush({ items: [item], total: 1, totalPages: 1 });
    return result;
  }

  it('getByRange() mapea estado numérico y fecha/hora', () => {
    const [v] = load();
    expect(v).toEqual({
      id: 'v1',
      residentId: 'p1',
      residentName: 'Lucía Fernández',
      visitorName: 'Madre',
      start: '2026-10-12T16:00:00',
      durationMinutes: 45,
      status: 'pending',
      cancellationReason: null,
    });
  });

  it('create() arma el cuerpo del backend', () => {
    service.create({ residentId: 'p1', visitorName: 'Madre', start: '2026-10-12T16:00:00', durationMinutes: 45 }).subscribe();
    const req = httpMock.expectOne('/api/casona-visits');
    expect(req.request.body).toEqual({
      personId: 'p1',
      visitorName: 'Madre',
      date: '2026-10-12',
      startTime: '16:00:00',
      estimatedDurationMinutes: 45,
    });
    req.flush({ id: 'v2' });
  });

  it('updateStatus() reenvía la visita completa con el estado numérico', () => {
    load();
    service.updateStatus('v1', 'done').subscribe();
    const req = httpMock.expectOne('/api/casona-visits/v1');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toMatchObject({
      personId: 'p1',
      date: '2026-10-12',
      startTime: '16:00:00',
      status: 1,
      cancellationReason: null,
    });
    req.flush(null);
  });

  it('cancelar sin motivo manda un motivo por defecto (el backend lo exige)', () => {
    load();
    service.updateStatus('v1', 'cancelled', '  ').subscribe();
    const req = httpMock.expectOne('/api/casona-visits/v1');
    expect(req.request.body.status).toBe(2);
    expect(req.request.body.cancellationReason).toBe('Sin motivo indicado');
    req.flush(null);
  });

  it('getResidents() pide fichas de residentes activos y arma el nombre', () => {
    let names: string[] = [];
    service.getResidents().subscribe((r) => (names = r.map((x) => x.fullName)));
    const req = httpMock.expectOne(
      (r) => r.url === '/api/social-records/list' && r.params.get('personType') === '1' && r.params.get('status') === '0',
    );
    req.flush({
      items: [
        { personId: 'p1', firstName: 'Lucía', lastName: 'Fernández' },
        { personId: 'p2', firstName: 'Carlos', lastName: null },
      ],
      total: 2,
      totalPages: 1,
    });
    expect(names).toEqual(['Lucía Fernández', 'Carlos']);
  });
});
