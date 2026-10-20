import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AttendanceActivitiesService } from './attendance-activities.service';

describe('AttendanceActivitiesService', () => {
  let service: AttendanceActivitiesService;
  let httpMock: HttpTestingController;
  const backendList = [
    { id: 'g-1', name: 'Desayuno', isActive: true, isFromCalendar: true, createdAt: '2026-10-01T00:00:00' },
    { id: 'g-2', name: 'Taller de huerta', isActive: false, isFromCalendar: false, createdAt: '2026-10-02T00:00:00' },
  ];

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(AttendanceActivitiesService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('arranca con las dos actividades fijas', () => {
    expect(service.all().map((a) => a.id)).toEqual(['hogar-de-dia', 'casa-convivencia']);
  });

  it('refresh() suma el catálogo del backend a las fijas y marca el origen', () => {
    service.refresh().subscribe();
    const req = httpMock.expectOne((r) => r.url === '/api/actividades');
    expect(req.request.params.get('includeInactive')).toBe('true');
    req.flush(backendList);

    expect(service.all().length).toBe(4);
    expect(service.all().find((a) => a.id === 'g-1')?.source).toBe('calendar');
    expect(service.all().find((a) => a.id === 'g-2')?.source).toBe('custom');
    expect(service.active().map((a) => a.id)).toEqual(['hogar-de-dia', 'casa-convivencia', 'g-1']);
    expect(service.name('g-2')).toBe('Taller de huerta');
  });

  it('create() valida largo y duplicados antes de llamar a la API', () => {
    service.refresh().subscribe();
    httpMock.expectOne((r) => r.url === '/api/actividades').flush(backendList);

    const messages: string[] = [];
    for (const name of ['ab', 'x'.repeat(61), '  desayuno ']) {
      service.create(name).subscribe({ error: (e) => messages.push(e.error.message) });
    }
    expect(messages[0]).toContain('al menos 3');
    expect(messages[1]).toContain('no puede superar');
    expect(messages[2]).toContain('Ya existe');
  });

  it('create() hace POST y devuelve la actividad creada', () => {
    let created: { id: string; name: string } | undefined;
    service.create('  Taller   de música ').subscribe((a) => (created = a));
    const post = httpMock.expectOne('/api/actividades');
    expect(post.request.body).toEqual({ name: 'Taller de música' });
    post.flush({ id: 'g-9' });
    httpMock.expectOne((r) => r.method === 'GET').flush([
      ...backendList,
      { id: 'g-9', name: 'Taller de música', isActive: true, isFromCalendar: false, createdAt: '' },
    ]);
    expect(created?.id).toBe('g-9');
  });

  it('setActive() manda el nombre actual y no deja desactivar una fija', () => {
    service.refresh().subscribe();
    httpMock.expectOne((r) => r.method === 'GET').flush(backendList);

    let message = '';
    service.setActive('hogar-de-dia', false).subscribe({ error: (e) => (message = e.error.message) });
    expect(message).toContain('actividad fija');

    service.setActive('g-1', false).subscribe();
    const put = httpMock.expectOne('/api/actividades/g-1');
    expect(put.request.body).toEqual({ name: 'Desayuno', isActive: false });
    put.flush(null);
    httpMock.expectOne((r) => r.method === 'GET').flush(backendList);
  });

  it('syncFromCalendar() devuelve la cantidad sincronizada por el backend', () => {
    let count = -1;
    service.syncFromCalendar().subscribe((n) => (count = n));
    httpMock.expectOne('/api/actividades/sincronizar-calendario').flush({ syncedCount: 2, syncedActivityNames: ['A', 'B'] });
    httpMock.expectOne((r) => r.method === 'GET').flush(backendList);
    expect(count).toBe(2);
  });
});
