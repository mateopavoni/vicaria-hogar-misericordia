import { todayLocalIso } from '../../../shared/utils/date.util';
import { MedicationStockService } from './medication-stock.service';

describe('MedicationStockService', () => {
  let service: MedicationStockService;
  const actor = { id: 'u1', name: 'Belén Test' };
  const today = todayLocalIso();
  const future = '2999-01-01';
  const entry = (overrides = {}) => ({
    medicationId: 'med-1' as string | null,
    newName: null as string | null,
    quantity: 10,
    expirationDate: future,
    supplier: 'Proveedor Test',
    ...overrides,
  });

  beforeEach(() => {
    service = new MedicationStockService();
  });

  it('registra un ingreso y suma al stock', (done) => {
    service.registerEntry(entry(), actor).subscribe((item) => {
      expect(item.quantity).toBe(50);
      done();
    });
  });

  it('exige cantidad entera mayor a 0, proveedor y vencimiento no pasado', (done) => {
    service.registerEntry(entry({ quantity: 0 }), actor).subscribe({
      error: (e1) => {
        expect(e1.error.message).toContain('cantidad');
        service.registerEntry(entry({ supplier: '  ' }), actor).subscribe({
          error: (e2) => {
            expect(e2.error.message).toContain('proveedor');
            service.registerEntry(entry({ expirationDate: '2020-01-01' }), actor).subscribe({
              error: (e3) => {
                expect(e3.error.message).toContain('anterior a hoy');
                done();
              },
            });
          },
        });
      },
    });
  });

  it('permite dar de alta un medicamento nuevo y rechaza duplicados', (done) => {
    service.registerEntry(entry({ medicationId: null, newName: 'Medicamento nuevo' }), actor).subscribe((item) => {
      expect(item.quantity).toBe(10);
      service.registerEntry(entry({ medicationId: null, newName: 'medicamento NUEVO' }), actor).subscribe({
        error: (err) => {
          expect(err.error.message).toContain('Ya existe');
          done();
        },
      });
    });
  });

  it('consume descuenta una unidad una sola vez por toma y restore la devuelve', () => {
    service.consume('med-1', 's1|' + today, today, actor);
    service.consume('med-1', 's1|' + today, today, actor);
    expect(service.quantityOf('med-1')).toBe(39);
    service.restore('med-1', 's1|' + today, today, actor);
    expect(service.quantityOf('med-1')).toBe(40);
    service.restore('med-1', 's1|' + today, today, actor);
    expect(service.quantityOf('med-1')).toBe(40);
  });

  it('no baja de 0', () => {
    service.consume('med-3', 's3|' + today, today, actor);
    expect(service.quantityOf('med-3')).toBe(0);
  });

  it('guarda el historial de movimientos con ingresos y consumos', (done) => {
    service.consume('med-1', 's1|' + today, today, actor);
    service.getMovements('med-1').subscribe((movs) => {
      expect(movs.map((m) => m.type)).toEqual(['consumption', 'entry']);
      expect(movs[0].quantity).toBe(-1);
      expect(movs[0].balanceAfter).toBe(39);
      expect(movs[1].supplier).toBeTruthy();
      done();
    });
  });

  it('valida el umbral', (done) => {
    service.setThreshold('med-1', -1).subscribe({
      error: () => {
        service.setThreshold('med-1', 20).subscribe((item) => {
          expect(item.lowStockThreshold).toBe(20);
          done();
        });
      },
    });
  });
});
