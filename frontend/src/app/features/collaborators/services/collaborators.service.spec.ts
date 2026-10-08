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
        registeredAt: '2026-10-08T10:00:00Z',
        registeredByName: 'Test referente',
      },
    ]);
    expect(result[0].type).toBe(CollaboratorType.Employee);
    expect(result[0].createdByName).toBe('Test referente');
    expect(result[0].isActive).toBe(true);
  });

  it('create() envía el tipo como número', () => {
    service.create({ firstName: 'Ana', type: CollaboratorType.Employee }).subscribe();
    const req = httpMock.expectOne('/api/collaborators');
    expect(req.request.method).toBe('POST');
    expect(req.request.body.type).toBe(1);
    req.flush({ id: 'c2' });
  });
});
