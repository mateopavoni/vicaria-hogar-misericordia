import { Component, input, output } from '@angular/core';
import { DatePipe } from '@angular/common';
import { UiButtonComponent, UiModalComponent } from '../../../../shared/ui';
import { MedicationStockItem, StockMovement, StockMovementType } from '../../interfaces/medication-stock.interface';

/** SCRUM-246: historial de movimientos de un medicamento — ingresos, consumos y fechas (DUMB). */
@Component({
  selector: 'app-stock-movements-modal',
  imports: [DatePipe, UiModalComponent, UiButtonComponent],
  template: `
    <ui-modal title="Movimientos" [subtitle]="item().name" size="lg" (closed)="closed.emit()">
      @if (loading()) {
        <p class="text-[13px] text-slate-400">Cargando movimientos...</p>
      } @else if (errorMessage()) {
        <p class="text-[13px] font-semibold text-red-600" role="alert">{{ errorMessage() }}</p>
      } @else {
        <div class="max-h-[55vh] overflow-auto rounded-lg border border-slate-200">
          <table class="w-full text-left text-[12px]">
            <thead class="sticky top-0 bg-slate-50 text-slate-600">
              <tr>
                <th class="px-3 py-2 font-bold">Fecha</th>
                <th class="px-3 py-2 font-bold">Tipo</th>
                <th class="px-3 py-2 text-right font-bold">Cantidad</th>
                <th class="px-3 py-2 text-right font-bold">Stock</th>
                <th class="px-3 py-2 font-bold">Detalle</th>
                <th class="px-3 py-2 font-bold">Usuario</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              @for (m of movements(); track m.id) {
                <tr>
                  <td class="px-3 py-2 text-slate-700">{{ m.date | date: 'dd/MM/yyyy HH:mm' }}</td>
                  <td class="px-3 py-2 font-semibold" [class]="m.type === 'consumption' ? 'text-red-600' : 'text-emerald-700'">
                    {{ typeLabel(m.type) }}
                  </td>
                  <td class="px-3 py-2 text-right font-semibold" [class]="m.quantity < 0 ? 'text-red-600' : 'text-emerald-700'">
                    {{ m.quantity > 0 ? '+' : '' }}{{ m.quantity }}
                  </td>
                  <td class="px-3 py-2 text-right text-slate-700">{{ m.balanceAfter }}</td>
                  <td class="px-3 py-2 text-slate-600">{{ detail(m) }}</td>
                  <td class="px-3 py-2 text-slate-500">{{ m.userName }}</td>
                </tr>
              } @empty {
                <tr><td colspan="6" class="px-3 py-6 text-center text-slate-400">Todavía no hay movimientos.</td></tr>
              }
            </tbody>
          </table>
        </div>
      }

      <ng-container ngProjectAs="[modal-footer]">
        <button uiButton variant="secondary" type="button" (click)="closed.emit()">Cerrar</button>
      </ng-container>
    </ui-modal>
  `,
})
export class StockMovementsModalComponent {
  item = input.required<MedicationStockItem>();
  movements = input<StockMovement[]>([]);
  loading = input(false);
  errorMessage = input<string | null>(null);

  closed = output<void>();

  typeLabel(type: StockMovementType): string {
    return type === 'entry' ? 'Ingreso' : type === 'consumption' ? 'Consumo' : 'Reversión';
  }

  detail(m: StockMovement): string {
    if (m.type === 'entry') {
      const exp = m.expirationDate ? `vence ${m.expirationDate.split('-').reverse().join('/')}` : '';
      return [m.supplier ? `Proveedor: ${m.supplier}` : '', exp].filter(Boolean).join(' · ');
    }
    return m.detail ?? '';
  }
}
