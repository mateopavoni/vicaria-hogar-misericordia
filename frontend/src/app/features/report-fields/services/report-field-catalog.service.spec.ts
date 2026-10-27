import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { CatalogActionResult } from '../interfaces/report-fields.interface';
import { ReportFieldCatalogService } from './report-field-catalog.service';

describe('ReportFieldCatalogService', () => {
  let service: ReportFieldCatalogService;
  let httpMock: HttpTestingController;
  const catalog = [
    { id: 'g1', category: 0, name: 'Masculino', isActive: true },
    { id: 'g2', category: 0, name: 'Femenino', isActive: true },
    { id: 'm1', category: 4, name: 'Violencia', isActive: true },
    { id: 'm2', category: 4, name: 'Múltiple', isActive: false },
  ];

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(ReportFieldCatalogService);
    httpMock = TestBed.inject(HttpTestingController);
    service.load().subscribe();
    httpMock.expectOne((r) => r.url === '/api/caritas-catalogs' && r.params.get('onlyActive') === 'false').flush(catalog);
  });

  afterEach(() => httpMock.verify());

  it('agrupa los valores por campo y separa los activos', () => {
    expect(service.values('gender').map((v) => v.label)).toEqual(['Masculino', 'Femenino']);
    expect(service.values('mainReason').length).toBe(2);
    expect(service.active('mainReason').map((v) => v.id)).toEqual(['m1']);
    expect(service.label('mainReason', 'm2')).toBe('Múltiple');
  });

  it('no vuelve a pedir el catálogo si ya está cargado', () => {
    service.load().subscribe();
    httpMock.expectNone('/api/caritas-catalogs');
  });

  it('agrega un valor con su categoría y rechaza duplicados y textos inválidos sin llamar a la API', () => {
    let result: CatalogActionResult | undefined;
    service.add('mainReason', '  Situación   de salud ').subscribe((r) => (result = r));
    const req = httpMock.expectOne((r) => r.method === 'POST' && r.url === '/api/caritas-catalogs');
    expect(req.request.body).toEqual({ category: 4, name: 'Situación de salud' });
    req.flush({ id: 'm3', category: 4, name: 'Situación de salud', isActive: true });
    expect(result?.ok).toBe(true);
    expect(service.label('mainReason', 'm3')).toBe('Situación de salud');

    service.add('mainReason', 'violencia').subscribe((r) => (result = r));
    expect(result?.ok).toBe(false);
    service.add('mainReason', 'a').subscribe((r) => (result = r));
    expect(result?.ok).toBe(false);
  });

  it('edita un valor y muestra el error del backend si falla', () => {
    let result: CatalogActionResult | undefined;
    service.rename('gender', 'g2', 'Mujer').subscribe((r) => (result = r));
    httpMock.expectOne('/api/caritas-catalogs/g2').flush({ id: 'g2', category: 0, name: 'Mujer', isActive: true });
    expect(service.label('gender', 'g2')).toBe('Mujer');

    service.rename('gender', 'g1', 'Varón').subscribe((r) => (result = r));
    httpMock.expectOne('/api/caritas-catalogs/g1').flush({ message: 'Ya existe un valor con ese nombre en esta categoría.' }, { status: 409, statusText: 'Conflict' });
    expect(result).toEqual({ ok: false, message: 'Ya existe un valor con ese nombre en esta categoría.' });
  });

  it('desactiva un valor, pero no el último activo del campo', () => {
    let result: CatalogActionResult | undefined;
    service.setActive('gender', 'g1', false).subscribe((r) => (result = r));
    const req = httpMock.expectOne('/api/caritas-catalogs/g1/status');
    expect(req.request.body).toEqual({ isActive: false });
    req.flush(null, { status: 204, statusText: 'No Content' });
    expect(result?.ok).toBe(true);
    expect(service.active('gender').map((v) => v.id)).toEqual(['g2']);

    service.setActive('mainReason', 'm1', false).subscribe((r) => (result = r));
    expect(result?.ok).toBe(false);
  });
});
