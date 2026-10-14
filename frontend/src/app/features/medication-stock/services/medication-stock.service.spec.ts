import { firstValueFrom } from 'rxjs';
import { errorOf } from '../../../shared/testing/error-of';
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

  it('registra un ingreso y suma al stock', async () => {
    const item = await firstValueFrom(service.registerEntry(entry(), actor));
    expect(item.quantity).toBe(50);
  });

  it('exige cantidad entera mayor a 0, proveedor y vencimiento no pasado', async () => {
    expect((await errorOf(service.registerEntry(entry({ quantity: 0 }), actor))).error.message).toContain('cantidad');
    expect((await errorOf(service.registerEntry(entry({ supplier: '  ' }), actor))).error.message).toContain('proveedor');
    expect((await errorOf(service.registerEntry(entry({ expirationDate: '2020-01-01' }), actor))).error.message).toContain('anterior a hoy');
  });

  it('permite dar de alta un medicamento nuevo y rechaza duplicados', async () => {
    const item = await firstValueFrom(service.registerEntry(entry({ medicationId: null, newName: 'Medicamento nuevo' }), actor));
    expect(item.quantity).toBe(10);
    const err = await errorOf(service.registerEntry(entry({ medicationId: null, newName: 'medicamento NUEVO' }), actor));
    expect(err.error.message).toContain('Ya existe');
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

  it('guarda el historial de movimientos con ingresos y consumos', async () => {
    service.consume('med-1', 's1|' + today, today, actor);
    const movs = await firstValueFrom(service.getMovements('med-1'));
    expect(movs.map((m) => m.type)).toEqual(['consumption', 'entry']);
    expect(movs[0].quantity).toBe(-1);
    expect(movs[0].balanceAfter).toBe(39);
    expect(movs[1].supplier).toBeTruthy();
  });

  it('valida el umbral', async () => {
    await errorOf(service.setThreshold('med-1', -1));
    const item = await firstValueFrom(service.setThreshold('med-1', 20));
    expect(item.lowStockThreshold).toBe(20);
  });
});
