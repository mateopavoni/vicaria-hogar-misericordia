import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map, switchMap, tap, throwError } from 'rxjs';
import { isBeforeToday } from '../../../shared/utils/date.util';
import {
  MedicationStockItem,
  RegisterStockEntryRequest,
  StockActor,
  StockMovement,
} from '../interfaces/medication-stock.interface';

const MAX_QUANTITY = 100000;

interface StockItemDto {
  medicationId: string;
  medicationName: string;
  currentStock: number;
  minimumStockThreshold: number;
}

/** MedicationStockMovementType del backend: 1 = Ingreso, 2 = Consumo (serializado como número). */
interface StockMovementDto {
  id: string;
  medicationId: string;
  type: number;
  quantity: number;
  date: string;
  expirationDate: string | null;
  supplier: string | null;
}

interface StockHistoryDto {
  currentStock: number;
  movements: StockMovementDto[];
}

/**
 * SCRUM-246: inventario de medicamentos del Centro Barrial (separado del stock de ropa).
 * Usa /api/stock-medicamentos (Referente y Directora).
 *
 * - El stock se descuenta en el backend al marcar una toma como administrada; el front ya no
 *   lleva su propia cuenta.
 * - El backend busca o crea el medicamento por nombre al registrar un ingreso, por eso se
 *   recuerda el nombre de cada medicamento listado.
 */
@Injectable({ providedIn: 'root' })
export class MedicationStockService {
  private readonly http = inject(HttpClient);
  private readonly names = new Map<string, string>();

  getItems(): Observable<MedicationStockItem[]> {
    return this.http.get<StockItemDto[]>('/api/stock-medicamentos').pipe(
      map((dtos) => dtos.map(toItem)),
      tap((items) => items.forEach((i) => this.names.set(i.id, i.name))),
    );
  }

  /** Movimientos de un medicamento, del más reciente al más antiguo. */
  getMovements(medicationId: string): Observable<StockMovement[]> {
    return this.http.get<StockHistoryDto>(`/api/stock-medicamentos/${medicationId}/movimientos`).pipe(
      map((history) => toMovements(history)),
    );
  }

  // `_actor` se conserva por compatibilidad: el backend toma al usuario del token.
  registerEntry(request: RegisterStockEntryRequest, _actor?: StockActor): Observable<MedicationStockItem> {
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

    const name = request.medicationId ? this.names.get(request.medicationId) : (request.newName ?? '').trim();
    if (!name) {
      return this.fail(
        request.medicationId ? 'El medicamento no existe.' : 'El nombre del medicamento es obligatorio.',
      );
    }
    if (!request.medicationId && (name.length < 3 || name.length > 100)) {
      return this.fail('El nombre del medicamento debe tener entre 3 y 100 caracteres.');
    }

    const body = {
      nombre: name,
      cantidad: request.quantity,
      fechaVencimiento: `${request.expirationDate}T00:00:00`,
      proveedor: supplier,
    };
    return this.http
      .post<{ medicationId: string }>('/api/stock-medicamentos', body)
      .pipe(switchMap((created) => this.itemAfterChange(created.medicationId)));
  }

  setThreshold(medicationId: string, threshold: number): Observable<MedicationStockItem> {
    if (!Number.isInteger(threshold) || threshold < 0 || threshold > MAX_QUANTITY) {
      return this.fail('El umbral debe ser un número entero de 0 o más.');
    }
    return this.http
      .put<void>(`/api/stock-medicamentos/${medicationId}/umbral`, { minimumStockThreshold: threshold })
      .pipe(switchMap(() => this.itemAfterChange(medicationId)));
  }

  private itemAfterChange(medicationId: string): Observable<MedicationStockItem> {
    return this.getItems().pipe(
      switchMap((items) => {
        const item = items.find((i) => i.id === medicationId);
        return item ? [item] : throwError(() => ({ error: { message: 'El medicamento no existe.' } }));
      }),
    );
  }

  private fail(message: string): Observable<never> {
    return throwError(() => ({ error: { message } }));
  }
}

function toItem(dto: StockItemDto): MedicationStockItem {
  return {
    id: dto.medicationId,
    name: dto.medicationName,
    quantity: dto.currentStock,
    lowStockThreshold: dto.minimumStockThreshold,
  };
}

/** El backend no manda saldo ni signo: el consumo llega positivo y el saldo se reconstruye hacia atrás. */
function toMovements(history: StockHistoryDto): StockMovement[] {
  const newestFirst = [...history.movements].sort((a, b) => b.date.localeCompare(a.date));
  let balance = history.currentStock;
  return newestFirst.map((m) => {
    const isEntry = m.type === 1;
    const signed = isEntry ? m.quantity : -m.quantity;
    const movement: StockMovement = {
      id: m.id,
      medicationId: m.medicationId,
      type: isEntry ? 'entry' : 'consumption',
      quantity: signed,
      balanceAfter: balance,
      date: m.date,
      expirationDate: m.expirationDate ? m.expirationDate.slice(0, 10) : null,
      supplier: m.supplier,
      detail: isEntry ? null : 'Toma administrada',
      userName: '—',
    };
    balance -= signed;
    return movement;
  });
}
