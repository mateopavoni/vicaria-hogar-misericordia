import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { BackupRecord } from '../interfaces/backup.interface';
import { BackupsService } from './backups.service';

describe('BackupsService', () => {
  let service: BackupsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(BackupsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('lista los últimos 30 respaldos del más nuevo al más viejo y mapea el estado', () => {
    let list: BackupRecord[] = [];
    service.list().subscribe((r) => (list = r));
    const req = httpMock.expectOne((r) => r.url === '/api/admin/backups');
    expect(req.request.params.get('pageSize')).toBe('30');
    req.flush({
      items: [
        { id: 'b1', status: 2, createdAt: '2026-10-20T03:00:00', sizeBytes: 2048, errorMessage: null },
        { id: 'b2', status: 3, createdAt: '2026-10-21T03:00:00', sizeBytes: 0, errorMessage: 'Sin espacio.' },
      ],
      total: 2,
      totalPages: 1,
    });
    expect(list.map((r) => r.id)).toEqual(['b2', 'b1']);
    expect(list[0]).toEqual({ id: 'b2', createdAt: '2026-10-21T03:00:00Z', sizeBytes: null, status: 'Failed', errorMessage: 'Sin espacio.' });
    expect(list[1].status).toBe('Completed');
    expect(list[1].sizeBytes).toBe(2048);
  });

  it('descarga el archivo con el nombre que manda el backend', () => {
    let filename = '';
    service.download('b1').subscribe((r) => (filename = r.filename));
    httpMock
      .expectOne('/api/admin/backups/b1/download')
      .flush(new Blob(['bak']), { headers: { 'Content-Disposition': 'attachment; filename=respaldo-2026-10-20.bak' } });
    expect(filename).toBe('respaldo-2026-10-20.bak');
  });
});
