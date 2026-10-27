import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MedicationStockService } from '../../../medication-stock/services/medication-stock.service';
import { MedicationStockItem } from '../../../medication-stock/interfaces/medication-stock.interface';
import { lowStockItems } from '../../utils/home-summary.util';

/** Alerta de medicamentos sin stock o con stock bajo. El que lo monta decide el permiso (medicamentos.stock). */
@Component({
  selector: 'app-low-stock-card',
  imports: [RouterLink],
  template: `
    <section class="rounded-2xl border border-[#B8C8FA] bg-white p-5 shadow-[0_8px_30px_rgba(79,115,232,0.08)]">
      <div class="flex items-center justify-between gap-3">
        <p class="text-[16px] font-bold text-slate-900">Stock de medicación</p>
        <a routerLink="/dashboard/medicacion/stock" class="text-[13px] font-semibold text-[#4167D9]">Ver stock</a>
      </div>
      @if (!loaded()) {
        <p class="mt-2 text-[13px] text-slate-500">Cargando...</p>
      } @else if (items().length === 0) {
        <p class="mt-2 text-[13px] text-slate-500">Todo el stock está por encima del mínimo.</p>
      } @else {
        <ul class="mt-3 space-y-2">
          @for (i of items(); track i.id) {
            <li class="flex items-center justify-between gap-3 text-[13px] text-slate-700">
              <span>{{ i.name }}</span>
              @if (i.quantity <= 0) {
                <span class="rounded bg-red-50 px-2 py-0.5 text-[12px] font-semibold text-red-700">Sin stock</span>
              } @else {
                <span class="rounded bg-amber-50 px-2 py-0.5 text-[12px] font-semibold text-amber-700">Quedan {{ i.quantity }}</span>
              }
            </li>
          }
        </ul>
      }
    </section>
  `,
})
export class LowStockCardComponent implements OnInit {
  private service = inject(MedicationStockService);

  items = signal<MedicationStockItem[]>([]);
  loaded = signal(false);

  ngOnInit(): void {
    this.service.getItems().subscribe({
      next: (list) => {
        this.items.set(lowStockItems(list));
        this.loaded.set(true);
      },
      error: () => this.loaded.set(true),
    });
  }
}
