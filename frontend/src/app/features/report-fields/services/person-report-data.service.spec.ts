import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { PersonReportDataService } from './person-report-data.service';

describe('PersonReportDataService', () => {
  let service: PersonReportDataService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(PersonReportDataService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('toma los campos del detalle de la ficha por persona y devuelve vacío para las demás', () => {
    service.setFromRecord('p1', {
      genderId: 'g1',
      housingSituationId: null,
      originLocalityId: null,
      documentationStatusId: 'd1',
      attendanceReasonId: null,
    });
    expect(service.get('p1').gender).toBe('g1');
    expect(service.get('p1').document).toBe('d1');
    expect(service.get('p1').housing).toBeNull();
    expect(service.get('p2')).toEqual({});
  });

  it('save() manda los cinco campos con los nombres del backend y actualiza el estado', () => {
    service.save('r1', 'p1', { gender: 'g1', mainReason: 'm1' }).subscribe();
    const req = httpMock.expectOne('/api/social-records/r1/report-fields');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({
      genderId: 'g1',
      housingSituationId: null,
      originLocalityId: null,
      documentationStatusId: null,
      attendanceReasonId: 'm1',
    });
    req.flush(null, { status: 204, statusText: 'No Content' });
    expect(service.get('p1').mainReason).toBe('m1');
  });
});
