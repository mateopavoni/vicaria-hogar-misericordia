import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type UiButtonVariant = 'primary' | 'secondary' | 'success' | 'danger' | 'warning' | 'info';
export type UiButtonSize = 'sm' | 'md';

const BASE =
  'inline-flex items-center justify-center gap-2 rounded-md font-semibold transition disabled:cursor-not-allowed disabled:opacity-50';

const VARIANTS: Record<UiButtonVariant, string> = {
  primary: 'bg-[#6B8DF5] text-white shadow-sm hover:bg-[#587BE8]',
  secondary: 'border border-[#D5DDF3] bg-white text-slate-700 hover:bg-slate-50',
  success: 'bg-green-500 text-white shadow-sm hover:bg-green-600 active:bg-green-700',
  danger: 'bg-red-500 text-white shadow-sm hover:bg-red-600 active:bg-red-700',
  warning: 'bg-amber-500 text-white shadow-sm hover:bg-amber-600 active:bg-amber-700',
  info: 'bg-blue-600 text-white shadow-sm hover:bg-blue-700 active:bg-blue-800',
};

const SIZES: Record<UiButtonSize, string> = {
  sm: 'px-3 py-1.5 text-[13px]',
  md: 'px-4 py-2 text-[11px] md:text-[13px]',
};

/**
 * Se usa como atributo sobre un <button> nativo: conserva type, disabled, title, aria-*.
 *   <button uiButton variant="danger" [loading]="saving()" (click)="...">Guardar</button>
 */
@Component({
  selector: 'button[uiButton]',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '[class]': 'classes()', '[attr.aria-busy]': 'loading() || null' },
  template: `
    @if (loading()) {
      <span class="h-3.5 w-3.5 animate-spin rounded-full border-2 border-current border-t-transparent" aria-hidden="true"></span>
    }
    <ng-content />
  `,
})
export class UiButtonComponent {
  readonly variant = input<UiButtonVariant>('primary');
  readonly size = input<UiButtonSize>('md');
  readonly loading = input(false);

  protected readonly classes = computed(
    () =>
      `${BASE} ${SIZES[this.size()]} ${VARIANTS[this.variant()]}${this.loading() ? ' pointer-events-none opacity-70' : ''}`,
  );
}
