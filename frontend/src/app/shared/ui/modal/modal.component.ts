import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

let nextId = 0;

/**
 * Cascarón único para todos los modales de la app (DUMB component).
 * Uso:
 *   <ui-modal title="..." (closed)="...">
 *     contenido
 *     <ng-container ngProjectAs="[modal-footer]"> botones </ng-container>
 *   </ui-modal>
 *
 * Cierra con Escape y con click en el fondo (desactivable con closeOnBackdrop).
 * El output se llama `closed` (no `close`) para no chocar con el evento nativo del DOM.
 */
@Component({
  selector: 'ui-modal',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '(document:keydown.escape)': 'closed.emit()' },
  template: `
    <div
      class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/40 p-4 backdrop-blur-sm"
      (click)="onBackdropClick($event)"
    >
      <div
        role="dialog"
        aria-modal="true"
        [attr.aria-labelledby]="titleId"
        class="w-full rounded-2xl border border-[#E3E8F5] bg-white shadow-[0_20px_50px_rgba(15,23,42,0.15)]"
        [class]="widths[size()]"
      >
        <header class="flex items-start justify-between border-b border-slate-100 px-6 py-5">
          <div>
            <h2 [id]="titleId" class="text-[17px] font-bold text-slate-900">{{ title() }}</h2>
            @if (subtitle()) {
              <p class="mt-1 text-[11px] text-slate-400">{{ subtitle() }}</p>
            }
          </div>
          <button
            type="button"
            (click)="closed.emit()"
            aria-label="Cerrar"
            class="flex h-8 w-8 items-center justify-center rounded-md text-slate-400 transition hover:bg-slate-100 hover:text-slate-600"
          >
            <svg class="h-5 w-5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
              <path d="M6 6l12 12" /><path d="M18 6L6 18" />
            </svg>
          </button>
        </header>

        <div class="px-6 py-6"><ng-content /></div>

        <footer class="flex justify-end gap-3 border-t border-slate-100 px-6 py-4">
          <ng-content select="[modal-footer]" />
        </footer>
      </div>
    </div>
  `,
})
export class UiModalComponent {
  readonly title = input.required<string>();
  readonly subtitle = input<string>();
  readonly size = input<'sm' | 'md' | 'lg'>('md');
  /** En modales con formulario conviene false, para no perder datos por un click accidental. */
  readonly closeOnBackdrop = input(true);

  readonly closed = output<void>();

  protected readonly titleId = `ui-modal-title-${nextId++}`;
  protected readonly widths = { sm: 'max-w-sm', md: 'max-w-md', lg: 'max-w-lg' } as const;

  protected onBackdropClick(event: MouseEvent): void {
    if (this.closeOnBackdrop() && event.target === event.currentTarget) {
      this.closed.emit();
    }
  }
}
