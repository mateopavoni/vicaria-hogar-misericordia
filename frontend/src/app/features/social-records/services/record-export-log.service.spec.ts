import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { RecordExportLogService } from './record-export-log.service';

describe('RecordExportLogService', () => {
  let service: RecordExportLogService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(RecordExportLogService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('registra la exportación en la auditoría del backend', () => {
    service.log('ficha-1', { sections: 'todo', format: 'pdf' }).subscribe();
    const req = httpMock.expectOne('/api/social-records/ficha-1/exports');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ sections: 'todo', format: 'pdf' });
    req.flush(null, { status: 204, statusText: 'No Content' });
  });

  it('descarga el PDF que genera el backend con las secciones elegidas', () => {
    let size = 0;
    service.downloadPdf('persona-1', 'datos-observaciones').subscribe((blob) => (size = blob.size));
    const req = httpMock.expectOne((r) => r.url === '/api/persons/persona-1/export-pdf');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('sections')).toBe('datos-observaciones');
    expect(req.request.responseType).toBe('blob');
    req.flush(new Blob(['%PDF-1.4']));
    expect(size).toBe(8);
  });
});
