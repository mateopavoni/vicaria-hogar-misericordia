import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { errorOf } from '../../../shared/testing/error-of';
import { MAX_ATTACHMENT_BYTES } from '../utils/attachment-validation.util';
import { PersonAttachmentsService } from './person-attachments.service';

describe('PersonAttachmentsService', () => {
  let service: PersonAttachmentsService;
  let httpMock: HttpTestingController;
  const actor = { id: 'u1', name: 'Belén Test' };
  const file = (name: string, type: string, size = 1000) => ({ name, type, size }) as File;
  const dto = (id: string, fileName: string, contentType: string, uploadedAt: string) => ({
    id,
    personId: 'p1',
    fileName,
    contentType,
    fileSizeBytes: 1000,
    fileTypeIcon: contentType === 'application/pdf' ? 'pdf' : 'image',
    uploadedAt,
    uploadedByUserId: 'u1',
    uploadedByName: 'Belén Test',
  });

  beforeEach(() => {
    // jsdom no trae createObjectURL/revokeObjectURL
    (URL as unknown as Record<string, unknown>)['createObjectURL'] = () => 'blob:test';
    (URL as unknown as Record<string, unknown>)['revokeObjectURL'] = () => undefined;
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(PersonAttachmentsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getByPerson() trae el listado y cada archivo como blob, del más reciente al más antiguo', () => {
    let list: { name: string; kind: string; url: string; uploadedByName: string }[] = [];
    service.getByPerson('p1').subscribe((r) => (list = r));
    httpMock.expectOne('/api/persons/p1/attachments').flush([
      dto('a1', 'recibo.pdf', 'application/pdf', '2026-10-20T10:00:00'),
      dto('a2', 'dni.png', 'image/png', '2026-10-21T10:00:00'),
    ]);
    httpMock.expectOne('/api/persons/p1/attachments/a1').flush(new Blob(['%PDF']));
    httpMock.expectOne('/api/persons/p1/attachments/a2').flush(new Blob(['png']));

    expect(list.map((a) => a.name)).toEqual(['dni.png', 'recibo.pdf']);
    expect(list.map((a) => a.kind)).toEqual(['image', 'pdf']);
    expect(list[0].url).toBe('blob:test');
    expect(list[0].uploadedByName).toBe('Belén Test');
  });

  it('upload() manda el archivo como multipart en el campo files', () => {
    service.upload('p1', new File(['png'], 'dni.png', { type: 'image/png' }), actor).subscribe();
    const req = httpMock.expectOne((r) => r.method === 'POST' && r.url === '/api/persons/p1/attachments');
    expect(req.request.body instanceof FormData).toBe(true);
    expect((req.request.body as FormData).has('files')).toBe(true);
    req.flush([dto('a3', 'dni.png', 'image/png', '2026-10-22T10:00:00')], { status: 201, statusText: 'Created' });
  });

  it('rechaza archivos de más de 10 MB sin llamar a la API', async () => {
    const err = await errorOf(service.upload('p1', file('grande.pdf', 'application/pdf', MAX_ATTACHMENT_BYTES + 1), actor));
    expect(err.error.message).toContain('10 MB');
  });

  it('rechaza tipos que no son JPG, PNG o PDF sin llamar a la API', async () => {
    const err = await errorOf(service.upload('p1', file('a.docx', 'application/msword'), actor));
    expect(err.error.message).toContain('solo se aceptan');
  });

  it('delete() llama al DELETE del adjunto de la persona', () => {
    service.delete('p1', 'a1').subscribe();
    const req = httpMock.expectOne('/api/persons/p1/attachments/a1');
    expect(req.request.method).toBe('DELETE');
    req.flush(null, { status: 204, statusText: 'No Content' });
  });
});
