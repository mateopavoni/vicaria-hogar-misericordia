import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

export interface UiTab<T extends string = string> {
  id: T;
  label: string;
  /** Contador opcional; no se muestra si es 0/undefined. */
  count?: number;
  /** 'alert' = badge rojo (ej. pendientes), 'neutral' = badge gris. */
  tone?: 'neutral' | 'alert';
}

/** Barra de pestañas: reemplaza el bloque de botones repetido de user-management y social-record-detail. */
@Component({
  selector: 'ui-tabs',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex border-b border-slate-200 px-4" role="tablist">
      @for (tab of tabs(); track tab.id) {
        <button
          type="button"
          role="tab"
          [attr.aria-selected]="tab.id === active()"
          (click)="tabChange.emit(tab.id)"
          class="relative px-5 py-4 text-[14px] font-semibold transition-colors"
          [class]="tab.id === active() ? 'text-[#4167D9]' : 'text-slate-600'"
        >
          {{ tab.label }}
          @if (tab.count) {
            <span
              class="ml-1.5 rounded-full px-1.5 py-0.5 text-[13px] font-semibold"
              [class]="tab.tone === 'alert' ? 'bg-red-100 text-red-500' : 'bg-slate-200 text-slate-600'"
            >
              {{ tab.count }}
            </span>
          }
          @if (tab.id === active()) {
            <span class="absolute bottom-[-1px] left-0 right-0 h-[2px] bg-[#4167D9]"></span>
          }
        </button>
      }
    </div>
  `,
})
export class UiTabsComponent<T extends string = string> {
  readonly tabs = input.required<readonly UiTab<T>[]>();
  readonly active = input.required<T>();
  readonly tabChange = output<T>();
}
