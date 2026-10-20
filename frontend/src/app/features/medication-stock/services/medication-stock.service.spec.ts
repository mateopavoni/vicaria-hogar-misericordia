import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { MedicationStockService } from './medication-stock.service';

describe('MedicationStockService', () => {
  let service: MedicationStockService;
  let httpMock: HttpTestingController;
  const future = '2999-01-01';
  const entry = (overrides = {}) => ({
    medicationId: 'med-1' as string | null,
    newName: null as string | null,
    quantity: 10,
    expirationDate: future,
    supplier: 'Proveedor Test',
    ...overrides,
  });
  const listDto = [
    { medicationId: 'med-1', medicationName: 'Aspirina', currentStock: 40, minimumStockThreshold: 10, isOutOfStock: false },
  ];

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(MedicationStockService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getItems() mapea el inventario del backend', () => {
    let items: unknown;
    service.getItems().subscribe((r) => (items = r));
    httpMock.expectOne('/api/stock-medicamentos').flush(listDto);
    expect(items).toEqual([{ id: 'med-1', name: 'Aspirina', quantity: 40, lowStockThreshold: 10 }]);
  });

  it('registerEntry() de un medicamento existente manda su nombre y devuelve el stock actualizado', () => {
    service.getItems().subscribe();
    httpMock.expectOne('/api/stock-medicamentos').flush(listDto);

    let result: { quantity: number } | undefined;
    service.registerEntry(entry()).subscribe((r) => (result = r));
    const post = httpMock.expectOne((r) => r.method === 'POST' && r.url === '/api/stock-medicamentos');
    expect(post.request.body).toEqual({
      nombre: 'Aspirina',
      cantidad: 10,
      fechaVencimiento: `${future}T00:00:00`,
      proveedor: 'Proveedor Test',
    });
    post.flush({ medicationId: 'med-1' });
    httpMock.expectOne((r) => r.method === 'GET').flush([{ ...listDto[0], currentStock: 50 }]);
    expect(result?.quantity).toBe(50);
  });

  it('registerEntry() de uno nuevo manda el nombre tipeado', () => {
    service.registerEntry(entry({ medicationId: null, newName: ' Medicamento nuevo ' })).subscribe();
    const post = httpMock.expectOne((r) => r.method === 'POST');
    expect(post.request.body.nombre).toBe('Medicamento nuevo');
    post.flush({ medicationId: 'med-9' });
    httpMock
      .expectOne((r) => r.method === 'GET')
      .flush([{ medicationId: 'med-9', medicationName: 'Medicamento nuevo', currentStock: 10, minimumStockThreshold: 5, isOutOfStock: false }]);
  });

  it('valida cantidad, proveedor y vencimiento antes de llamar a la API', () => {
    const messages: string[] = [];
    const collect = (request: ReturnType<typeof entry>) =>
      service.registerEntry(request).subscribe({ error: (e) => messages.push(e.error.message) });
    collect(entry({ quantity: 0 }));
    collect(entry({ supplier: '  ' }));
    collect(entry({ expirationDate: '2020-01-01' }));
    expect(messages[0]).toContain('cantidad');
    expect(messages[1]).toContain('proveedor');
    expect(messages[2]).toContain('anterior a hoy');
  });

  it('setThreshold() hace PUT al umbral y relee el item', () => {
    let result: { lowStockThreshold: number } | undefined;
    service.setThreshold('med-1', 20).subscribe((r) => (result = r));
    const put = httpMock.expectOne('/api/stock-medicamentos/med-1/umbral');
    expect(put.request.body).toEqual({ minimumStockThreshold: 20 });
    put.flush(null);
    httpMock.expectOne((r) => r.method === 'GET').flush([{ ...listDto[0], minimumStockThreshold: 20 }]);
    expect(result?.lowStockThreshold).toBe(20);
  });

  it('getMovements() da signo al consumo y reconstruye el saldo', () => {
    let movements: { type: string; quantity: number; balanceAfter: number }[] = [];
    service.getMovements('med-1').subscribe((r) => (movements = r));
    httpMock.expectOne('/api/stock-medicamentos/med-1/movimientos').flush({
      currentStock: 9,
      movements: [
        { id: 'a', medicationId: 'med-1', type: 1, quantity: 10, date: '2026-10-19T10:00:00', expirationDate: null, supplier: 'P' },
        { id: 'b', medicationId: 'med-1', type: 2, quantity: 1, date: '2026-10-19T11:00:00', expirationDate: null, supplier: null },
      ],
    });
    expect(movements.map((m) => [m.type, m.quantity, m.balanceAfter])).toEqual([
      ['consumption', -1, 9],
      ['entry', 10, 10],
    ]);
  });
});
