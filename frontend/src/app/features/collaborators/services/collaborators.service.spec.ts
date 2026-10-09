import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { CollaboratorsService } from './collaborators.service';
import { Collaborator, CollaboratorType } from '../interfaces/collaborator.interface';

describe('CollaboratorsService', () => {
  let service: CollaboratorsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(CollaboratorsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getAll() mapea el tipo numérico y quién lo registró', () => {
    let result: Collaborator[] = [];
    service.getAll().subscribe((r) => (result = r));
    httpMock.expectOne('/api/collaborators').flush([
      {
        id: 'c1',
        firstName: 'Lucía',
        lastName: 'Gómez',
        dni: '30',
        phone: null,
        email: null,
        type: 1,
        workArea: 'Cocina',
        isActive: false,
        registeredAt: '2026-10-08T10:00:00Z',
        registeredByName: 'Test referente',
      },
    ]);
    expect(result[0].type).toBe(CollaboratorType.Employee);
    expect(result[0].createdByName).toBe('Test referente');
    expect(result[0].isActive).toBe(false);
  });

  it('create() envía el tipo como número', () => {
    service.create({ firstName: 'Ana', type: CollaboratorType.Employee }).subscribe();
    const req = httpMock.expectOne('/api/collaborators');
    expect(req.request.method).toBe('POST');
    expect(req.request.body.type).toBe(1);
    req.flush({ id: 'c2' });
  });

  it('toggleActive() hace PUT con el colaborador completo e isActive (baja lógica)', () => {
    service.getAll().subscribe();
    httpMock.expectOne('/api/collaborators').flush([
      {
        id: 'c1', firstName: 'Lucía', lastName: 'Gómez', dni: '30', phone: null, email: null,
        type: 1, workArea: 'Cocina', isActive: true, registeredAt: '2026-10-08T10:00:00Z', registeredByName: null,
      },
    ]);

    let result: Collaborator | undefined;
    service.toggleActive('c1', false).subscribe((c) => (result = c));
    const req = httpMock.expectOne('/api/collaborators/c1');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toMatchObject({ firstName: 'Lucía', dni: '30', type: 1, isActive: false });
    req.flush(null);
    expect(result?.isActive).toBe(false);
  });

  it('toggleActive() sobre un colaborador no listado falla sin llamar al backend', () => {
    let failed = false;
    service.toggleActive('x', false).subscribe({ error: () => (failed = true) });
    expect(failed).toBe(true);
  });
});
