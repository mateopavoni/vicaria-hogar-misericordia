import { itemsNeedingAttention, stockLevel } from './stock-level.util';

describe('stock-level.util', () => {
  it('sin stock cuando la cantidad es 0', () => {
    expect(stockLevel({ quantity: 0, lowStockThreshold: 5 })).toBe('out');
  });

  it('stock bajo en el umbral o debajo', () => {
    expect(stockLevel({ quantity: 5, lowStockThreshold: 5 })).toBe('low');
    expect(stockLevel({ quantity: 3, lowStockThreshold: 5 })).toBe('low');
  });

  it('ok por encima del umbral', () => {
    expect(stockLevel({ quantity: 6, lowStockThreshold: 5 })).toBe('ok');
  });

  it('con umbral 0 solo alerta al llegar a 0', () => {
    expect(stockLevel({ quantity: 1, lowStockThreshold: 0 })).toBe('ok');
    expect(stockLevel({ quantity: 0, lowStockThreshold: 0 })).toBe('out');
  });

  it('lista los que necesitan atención, los más bajos primero', () => {
    const r = itemsNeedingAttention([
      { quantity: 20, lowStockThreshold: 5 },
      { quantity: 4, lowStockThreshold: 5 },
      { quantity: 0, lowStockThreshold: 5 },
    ]);
    expect(r.map((i) => i.quantity)).toEqual([0, 4]);
  });
});
