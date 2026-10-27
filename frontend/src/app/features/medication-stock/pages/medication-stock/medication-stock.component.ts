import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { AuthService } from '../../../../core/auth/auth.service';
import { extractApiError } from '../../../../core/http/api-error';
import { UiBackButtonComponent, UiButtonComponent } from '../../../../shared/ui';
import { StockEntryModalComponent } from '../../components/stock-entry-modal/stock-entry-modal.component';
import { StockMovementsModalComponent } from '../../components/stock-movements-modal/stock-movements-modal.component';
import { StockThresholdModalComponent } from '../../components/stock-threshold-modal/stock-threshold-modal.component';
import {
  MedicationStockItem,
  RegisterStockEntryRequest,
  StockLevel,
  StockMovement,
} from '../../interfaces/medication-stock.interface';
import { MedicationStockService } from '../../services/medication-stock.service';
import { itemsNeedingAttention, stockLevel } from '../../utils/stock-level.util';

/**
 * SCRUM-246: inventario de medicamentos del Centro Barrial (/dashboard/medicacion/stock).
 * SMART: listado con stock actual, ingreso, umbral, alerta de stock bajo e historial de
 * movimientos por medicamento. Solo con permiso medicamentos.stock (Referente y Directora).
 * Independiente del stock de ropa.
 */
@Component({
  selector: 'app-medication-stock',
  imports: [UiBackButtonComponent, UiButtonComponent, StockEntryModalComponent, StockThresholdModalComponent, StockMovementsModalComponent],
  templateUrl: './medication-stock.component.html',
})
export class MedicationStockComponent implements OnInit {
  private stockService = inject(MedicationStockService);
  private authService = inject(AuthService);

  items = signal<MedicationStockItem[]>([]);
  loading = signal(true);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  private successTimeout: ReturnType<typeof setTimeout> | null = null;

  attention = computed(() => itemsNeedingAttention(this.items()));

  // modales
  showEntry = signal(false);
  entryPreselected = signal<string | null>(null);
  thresholdItem = signal<MedicationStockItem | null>(null);
  movementsItem = signal<MedicationStockItem | null>(null);
  movements = signal<StockMovement[]>([]);
  movementsLoading = signal(false);
  movementsError = signal<string | null>(null);
  saving = signal(false);
  formError = signal<string | null>(null);

  ngOnInit(): void {
    this.reload();
  }

  level(item: MedicationStockItem): StockLevel {
    return stockLevel(item);
  }

  private reload(): void {
    this.stockService.getItems().subscribe({
      next: (items) => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.errorMessage.set('No se pudo cargar el inventario de medicamentos.');
      },
    });
  }

  private actor() {
    const user = this.authService.user();
    return user ? { id: user.id, name: `${user.name} ${user.lastname}`.trim() } : null;
  }

  // ── ingreso ──
  openEntry(preselectedId: string | null = null): void {
    this.formError.set(null);
    this.entryPreselected.set(preselectedId);
    this.showEntry.set(true);
  }

  saveEntry(request: RegisterStockEntryRequest): void {
    const actor = this.actor();
    if (!actor) return;
    this.saving.set(true);
    this.formError.set(null);
    this.stockService.registerEntry(request, actor).subscribe({
      next: (item) => {
        this.saving.set(false);
        this.showEntry.set(false);
        this.showSuccess(`Ingreso registrado: ${item.name} (stock actual ${item.quantity}).`);
        this.reload();
      },
      error: (err) => {
        this.saving.set(false);
        this.formError.set(extractApiError(err, 'No se pudo registrar el ingreso.'));
      },
    });
  }

  // ── umbral ──
  openThreshold(item: MedicationStockItem): void {
    this.formError.set(null);
    this.thresholdItem.set(item);
  }

  saveThreshold(value: number): void {
    const item = this.thresholdItem();
    if (!item) return;
    this.saving.set(true);
    this.formError.set(null);
    this.stockService.setThreshold(item.id, value).subscribe({
      next: () => {
        this.saving.set(false);
        this.thresholdItem.set(null);
        this.showSuccess('Umbral actualizado.');
        this.reload();
      },
      error: (err) => {
        this.saving.set(false);
        this.formError.set(extractApiError(err, 'No se pudo guardar el umbral.'));
      },
    });
  }

  // ── movimientos ──
  openMovements(item: MedicationStockItem): void {
    this.movementsItem.set(item);
    this.movements.set([]);
    this.movementsError.set(null);
    this.movementsLoading.set(true);
    this.stockService.getMovements(item.id).subscribe({
      next: (movements) => {
        this.movements.set(movements);
        this.movementsLoading.set(false);
      },
      error: () => {
        this.movementsLoading.set(false);
        this.movementsError.set('No se pudieron cargar los movimientos.');
      },
    });
  }

  private showSuccess(message: string): void {
    this.successMessage.set(message);
    if (this.successTimeout) clearTimeout(this.successTimeout);
    this.successTimeout = setTimeout(() => this.successMessage.set(null), 4000);
  }
}
