import { Injectable } from '@angular/core';
import { Observable, delay, of, throwError } from 'rxjs';
import { isBeforeToday } from '../../../shared/utils/date.util';
import {
  MedicationStockItem,
  RegisterStockEntryRequest,
  StockActor,
  StockMovement,
} from '../interfaces/medication-stock.interface';

const MAX_QUANTITY = 100000;

/**
 * SCRUM-246: inventario de medicamentos del Centro Barrial (separado del stock de ropa).
 *
 * OJO — backend pendiente (EP-11, SCRUM-71): no existen endpoints de stock de medicamentos.
 * Hasta entonces este servicio vive en memoria (se pierde al recargar) con la misma forma
 * de error ({ error: { message } }) que lee extractApiError. Al llegar los endpoints solo
 * hay que cambiar el cuerpo de los métodos públicos por HttpClient.
 *
 * - Es la fuente del catálogo de medicamentos y del stock que usan los esquemas
 *   (MedicationService). Datos de prueba: 3 medicamentos genéricos, no reales.
 * - Descuento automático: MedicationService llama a consume() al marcar una toma como
 *   administrada y a restore() si esa toma se corrige. Se asume 1 unidad por toma, porque la
 *   dosis es texto libre y no hay equivalencia a unidades.
 * - Si el stock ya está en 0 la toma se registra igual, pero no hay nada que descontar.
 */
@Injectable({ providedIn: 'root' })
export class MedicationStockService {
  private items: MedicationStockItem[] = [];
  private movements: StockMovement[] = [];
  /** Tomas que ya descontaron una unidad (clave esquema|fecha), para poder revertirlas. */
  private deducted = new Set<string>();
  private nextItem = 4;
  private nextMovement = 1;

  constructor() {
    const seedActor: StockActor = { id: 'seed', name: 'Carga inicial' };
    const seed = (id: string, name: string, quantity: number, threshold: number) => {
      this.items.push({ id, name, quantity: 0, lowStockThreshold: threshold });
      if (quantity > 0) {
        this.addEntry(id, quantity, this.inOneYear(), 'Datos de prueba', seedActor);
      }
    };
    seed('med-1', 'Medicamento de prueba A', 40, 10);
    seed('med-2', 'Medicamento de prueba B', 12, 15); // ya arranca en stock bajo
    seed('med-3', 'Medicamento de prueba C (sin stock)', 0, 5);
  }

  // ───────── API para los componentes ─────────

  getItems(): Observable<MedicationStockItem[]> {
    return of(this.snapshot()).pipe(delay(100));
  }

  /** Movimientos de un medicamento, del más reciente al más antiguo. */
  getMovements(medicationId: string): Observable<StockMovement[]> {
    const own = this.movements
      .filter((m) => m.medicationId === medicationId)
      .sort((a, b) => b.date.localeCompare(a.date) || Number(b.id.split('-')[1]) - Number(a.id.split('-')[1]));
    return of(own.map((m) => ({ ...m }))).pipe(delay(100));
  }

  registerEntry(request: RegisterStockEntryRequest, actor: StockActor): Observable<MedicationStockItem> {
    if (!Number.isInteger(request.quantity) || request.quantity < 1 || request.quantity > MAX_QUANTITY) {
      return this.fail('La cantidad debe ser un número entero mayor a 0.');
    }
    if (!request.expirationDate) {
      return this.fail('La fecha de vencimiento es obligatoria.');
    }
    if (isBeforeToday(request.expirationDate)) {
      return this.fail('La fecha de vencimiento no puede ser anterior a hoy.');
    }
    const supplier = request.supplier.trim();
    if (!supplier) {
      return this.fail('El proveedor es obligatorio.');
    }
    if (supplier.length > 100) {
      return this.fail('El proveedor no puede superar los 100 caracteres.');
    }

    let item: MedicationStockItem | undefined;
    if (request.medicationId) {
      item = this.items.find((i) => i.id === request.medicationId);
      if (!item) {
        return this.fail('El medicamento no existe.');
      }
    } else {
      const name = (request.newName ?? '').trim();
      if (name.length < 3 || name.length > 100) {
        return this.fail('El nombre del medicamento debe tener entre 3 y 100 caracteres.');
      }
      if (this.items.some((i) => i.name.toLowerCase() === name.toLowerCase())) {
        return this.fail('Ya existe un medicamento con ese nombre: elegilo de la lista.');
      }
      item = { id: `med-${this.nextItem++}`, name, quantity: 0, lowStockThreshold: 5 };
      this.items.push(item);
    }

    this.addEntry(item.id, request.quantity, request.expirationDate, supplier, actor);
    return of({ ...item }).pipe(delay(150));
  }

  setThreshold(medicationId: string, threshold: number): Observable<MedicationStockItem> {
    if (!Number.isInteger(threshold) || threshold < 0 || threshold > MAX_QUANTITY) {
      return this.fail('El umbral debe ser un número entero de 0 o más.');
    }
    const item = this.items.find((i) => i.id === medicationId);
    if (!item) {
      return this.fail('El medicamento no existe.');
    }
    item.lowStockThreshold = threshold;
    return of({ ...item }).pipe(delay(100));
  }

  // ───────── uso interno entre servicios (síncrono) ─────────

  quantityOf(medicationId: string): number {
    return this.items.find((i) => i.id === medicationId)?.quantity ?? 0;
  }

  snapshotForCatalog(): MedicationStockItem[] {
    return this.snapshot();
  }

  find(medicationId: string): MedicationStockItem | null {
    const item = this.items.find((i) => i.id === medicationId);
    return item ? { ...item } : null;
  }

  /** Descuenta una unidad por una toma administrada. refKey = esquema|fecha. */
  consume(medicationId: string, refKey: string, date: string, actor: StockActor): void {
    const item = this.items.find((i) => i.id === medicationId);
    if (!item || item.quantity <= 0 || this.deducted.has(refKey)) {
      return;
    }
    item.quantity -= 1;
    this.deducted.add(refKey);
    this.push(item, 'consumption', -1, actor, { detail: `Toma administrada el ${this.fmt(date)}` });
  }

  /** Devuelve la unidad si la toma que la descontó se corrige a "no administrada". */
  restore(medicationId: string, refKey: string, date: string, actor: StockActor): void {
    const item = this.items.find((i) => i.id === medicationId);
    if (!item || !this.deducted.has(refKey)) {
      return;
    }
    item.quantity += 1;
    this.deducted.delete(refKey);
    this.push(item, 'reversal', 1, actor, { detail: `Toma del ${this.fmt(date)} corregida a no administrada` });
  }

  // ───────── helpers ─────────

  private addEntry(id: string, quantity: number, expirationDate: string, supplier: string, actor: StockActor): void {
    const item = this.items.find((i) => i.id === id)!;
    item.quantity += quantity;
    this.push(item, 'entry', quantity, actor, { expirationDate, supplier });
  }

  private push(
    item: MedicationStockItem,
    type: StockMovement['type'],
    quantity: number,
    actor: StockActor,
    extra: Partial<Pick<StockMovement, 'expirationDate' | 'supplier' | 'detail'>>,
  ): void {
    this.movements.push({
      id: `mov-${this.nextMovement++}`,
      medicationId: item.id,
      type,
      quantity,
      balanceAfter: item.quantity,
      date: new Date().toISOString(),
      expirationDate: extra.expirationDate ?? null,
      supplier: extra.supplier ?? null,
      detail: extra.detail ?? null,
      userName: actor.name,
    });
  }

  private snapshot(): MedicationStockItem[] {
    return this.items.map((i) => ({ ...i })).sort((a, b) => a.name.localeCompare(b.name, 'es'));
  }

  private fmt(iso: string): string {
    return iso.split('-').reverse().join('/');
  }

  private inOneYear(): string {
    const d = new Date();
    d.setFullYear(d.getFullYear() + 1);
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
  }

  private fail(message: string): Observable<never> {
    return throwError(() => ({ error: { message } }));
  }
}
