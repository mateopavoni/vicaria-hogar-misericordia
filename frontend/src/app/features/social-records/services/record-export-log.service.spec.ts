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
});
