import { MedicationStockItem, StockLevel } from '../interfaces/medication-stock.interface';

/** 'out' = sin stock; 'low' = en o bajo el umbral configurado; 'ok' = resto. */
export function stockLevel(item: Pick<MedicationStockItem, 'quantity' | 'lowStockThreshold'>): StockLevel {
  if (item.quantity <= 0) return 'out';
  return item.quantity <= item.lowStockThreshold ? 'low' : 'ok';
}

/** Medicamentos que requieren atención (sin stock o stock bajo), los sin stock primero. */
export function itemsNeedingAttention<T extends Pick<MedicationStockItem, 'quantity' | 'lowStockThreshold'>>(items: T[]): T[] {
  return items
    .filter((i) => stockLevel(i) !== 'ok')
    .sort((a, b) => a.quantity - b.quantity);
}
