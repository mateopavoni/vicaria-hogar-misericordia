import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { LifeStoryService } from './life-story.service';

// bug reportado 2026-09-23: guardar una etapa pisaba el contenido anterior en vez de
// sumar una entrada nueva al historial. Este spec fija que el mapeo del frontend
// conserva la lista de entradas que ahora manda el backend (LifeStorySectionDto.Entries).
describe('LifeStoryService', () => {
  let service: LifeStoryService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(LifeStoryService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getByPersonId() mapea las entradas de cada etapa, más reciente primero', () => {
    let result: unknown;
    service.getByPersonId('person-1').subscribe((r) => (result = r));

    const req = httpMock.expectOne('/api/persons/person-1/life-story');
    req.flush({
      id: 'ls-1',
      personId: 'person-1',
      beforeHogar: {
        content: 'segunda entrada',
        isCompleted: true,
        updatedByUserId: 'u1',
        updatedByName: 'Ana Perez',
        updatedAt: '2026-09-23T10:00:00Z',
        entries: [
          { id: 'e2', content: 'segunda entrada', createdByUserId: 'u1', createdByName: 'Ana Perez', createdAt: '2026-09-23T10:00:00Z' },
          { id: 'e1', content: 'primera entrada', createdByUserId: 'u1', createdByName: 'Ana Perez', createdAt: '2026-09-20T10:00:00Z' },
        ],
      },
      inHogar: { content: null, isCompleted: false, updatedByUserId: null, updatedByName: null, updatedAt: null, entries: [] },
      afterHogar: { content: null, isCompleted: false, updatedByUserId: null, updatedByName: null, updatedAt: null, entries: [] },
    });

    const history = result as any;
    expect(history.beforeHome.entries).toHaveLength(2);
    expect(history.beforeHome.entries[0].text).toBe('segunda entrada');
    expect(history.beforeHome.entries[1].text).toBe('primera entrada');
    expect(history.beforeHome.text).toBe('segunda entrada');
  });

  it('updateStage() manda el content al segmento de ruta correcto (no pisa, el backend suma una entrada)', () => {
    service.updateStage('person-1', 'duringHome', 'nueva entrada').subscribe();

    const req = httpMock.expectOne('/api/persons/person-1/life-story/in-hogar');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ content: 'nueva entrada' });
    req.flush({
      id: 'ls-1',
      personId: 'person-1',
      beforeHogar: { content: null, isCompleted: false, updatedByUserId: null, updatedByName: null, updatedAt: null, entries: [] },
      inHogar: { content: 'nueva entrada', isCompleted: true, updatedByUserId: 'u1', updatedByName: 'Ana', updatedAt: '2026-09-23T10:00:00Z', entries: [] },
      afterHogar: { content: null, isCompleted: false, updatedByUserId: null, updatedByName: null, updatedAt: null, entries: [] },
    });
  });
});
