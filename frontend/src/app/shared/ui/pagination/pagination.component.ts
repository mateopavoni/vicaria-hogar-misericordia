import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

const WINDOW = 5;

/** Paginación reutilizable: reemplaza el bloque casi idéntico de user-management y social-record-list. */
@Component({
  selector: 'ui-pagination',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (totalPages() > 1) {
      <nav class="mt-6 flex items-center justify-center gap-3 text-[13px]" aria-label="Paginación">
        <button
          type="button"
          (click)="goTo(currentPage() - 1)"
          [disabled]="currentPage() === 1"
          class="px-2 py-1 font-semibold text-[#4167D9] transition hover:text-[#3155C5] disabled:cursor-not-allowed disabled:text-slate-400 disabled:opacity-40"
        >
          &lt; Anterior
        </button>

        @for (page of pages(); track page) {
          <button
            type="button"
            (click)="goTo(page)"
            [attr.aria-current]="page === currentPage() ? 'page' : null"
            class="flex h-8 w-8 items-center justify-center rounded-md font-semibold transition"
            [class]="page === currentPage() ? 'bg-[#6B8DF5] text-white' : 'text-slate-600 hover:bg-slate-100'"
          >
            {{ page }}
          </button>
        }

        <button
          type="button"
          (click)="goTo(currentPage() + 1)"
          [disabled]="currentPage() === totalPages()"
          class="px-2 py-1 font-semibold text-[#4167D9] transition hover:text-[#3155C5] disabled:cursor-not-allowed disabled:opacity-40"
        >
          Siguiente &gt;
        </button>
      </nav>
    }
  `,
})
export class UiPaginationComponent {
  readonly currentPage = input.required<number>();
  readonly totalPages = input.required<number>();
  readonly pageChange = output<number>();

  protected readonly pages = computed(() => {
    const total = this.totalPages();
    const start = Math.max(1, Math.min(this.currentPage() - 2, total - WINDOW + 1));
    const end = Math.min(total, start + WINDOW - 1);
    return Array.from({ length: end - start + 1 }, (_, i) => start + i);
  });

  protected goTo(page: number): void {
    if (page >= 1 && page <= this.totalPages() && page !== this.currentPage()) {
      this.pageChange.emit(page);
    }
  }
}
