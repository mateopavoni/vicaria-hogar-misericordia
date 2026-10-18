/**
 * SCRUM-246 (front de SCRUM-71): inventario de medicamentos del Centro Barrial.
 * Módulo independiente del stock de ropa. OJO — backend pendiente: ver MedicationStockService.
 */

export interface MedicationStockItem {
  /** Mismo id que el catálogo de medicamentos usado en los esquemas. */
  id: string;
  name: string;
  /** Unidades disponibles. */
  quantity: number;
  /** Se alerta cuando quantity <= lowStockThreshold. */
  lowStockThreshold: number;
}

export type StockLevel = 'ok' | 'low' | 'out';

export type StockMovementType = 'entry' | 'consumption' | 'reversal';

export interface StockMovement {
  id: string;
  medicationId: string;
  type: StockMovementType;
  /** Variación con signo: + ingresos y reversiones, − consumos. */
  quantity: number;
  /** Stock resultante después del movimiento. */
  balanceAfter: number;
  /** Fecha y hora (ISO). */
  date: string;
  /** Solo ingresos. YYYY-MM-DD */
  expirationDate: string | null;
  /** Solo ingresos. */
  supplier: string | null;
  /** Texto libre, ej. "Toma administrada el 2026-10-13". */
  detail: string | null;
  userName: string;
}

export interface RegisterStockEntryRequest {
  /** Medicamento existente… */
  medicationId: string | null;
  /** …o nombre de uno nuevo (se agrega al catálogo). Uno de los dos. */
  newName: string | null;
  quantity: number;
  /** YYYY-MM-DD, no puede ser anterior a hoy. */
  expirationDate: string;
  supplier: string;
}

export interface StockActor {
  id: string;
  name: string;
}
