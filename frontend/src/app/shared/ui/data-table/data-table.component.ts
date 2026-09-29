import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy, Component, Directive, TemplateRef, computed, contentChildren, inject, input,
} from '@angular/core';

export interface UiColumn {
  key: string;
  header: string;
  align?: 'left' | 'center';
}

/**
 * Plantilla de celda para una columna, para cuando el valor no es texto plano:
 *   <ng-template uiCell="actions" let-row> ... </ng-template>
 * Las columnas sin plantilla muestran row[key] tal cual.
 */
@Directive({ selector: 'ng-template[uiCell]' })
export class UiCellDirective {
  readonly key = input.required<string>({ alias: 'uiCell' });
  readonly template = inject(TemplateRef);
}

/** Tabla con loading/vacío/filas incorporados: reemplaza la tabla de user-management y la lista de fichas. */
@Component({
  selector: 'ui-data-table',
  imports: [NgTemplateOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="overflow-x-auto">
      <table class="w-full min-w-[700px]">
        <thead class="border-b border-slate-200 bg-[#FAFAFB]">
          <tr>
            @for (col of columns(); track col.key) {
              <th
                class="px-6 py-3 text-[13px] font-bold text-slate-700"
                [class]="col.align === 'center' ? 'text-center' : 'text-left'"
              >
                {{ col.header }}
              </th>
            }
          </tr>
        </thead>

        <tbody class="divide-y divide-slate-100">
          @if (loading()) {
            <tr>
              <td [attr.colspan]="columns().length" class="px-6 py-12 text-center">
                <div class="inline-flex items-center gap-2 text-[14px] text-slate-400">
                  <span class="h-4 w-4 animate-spin rounded-full border-2 border-[#D5DDF3] border-t-[#6B8DF5]"></span>
                  {{ loadingText() }}
                </div>
              </td>
            </tr>
          } @else if (rows().length === 0) {
            <tr>
              <td [attr.colspan]="columns().length" class="px-6 py-16 text-center">
                <div class="flex flex-col items-center justify-center">
                  <div class="mb-3 flex h-10 w-10 items-center justify-center rounded-full bg-[#EDF2FF] text-[#6B8DF5]">
                    <svg class="h-5 w-5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                      <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" />
                      <circle cx="9" cy="7" r="4" />
                      <path d="M19 8v6" /><path d="M22 11h-6" />
                    </svg>
                  </div>
                  <p class="text-[14px] font-semibold text-slate-600">{{ emptyMessage() }}</p>
                </div>
              </td>
            </tr>
          } @else {
            @for (row of rows(); track row) {
              <tr class="transition hover:bg-slate-50">
                @for (col of columns(); track col.key) {
                  <td class="px-6 py-3 text-[13px] text-slate-600" [class.text-center]="col.align === 'center'">
                    @let tpl = templates().get(col.key);
                    @if (tpl) {
                      <ng-container *ngTemplateOutlet="tpl; context: { $implicit: row }" />
                    } @else {
                      {{ $any(row)[col.key] }}
                    }
                  </td>
                }
              </tr>
            }
          }
        </tbody>
      </table>
    </div>
  `,
})
export class UiDataTableComponent<T> {
  readonly columns = input.required<readonly UiColumn[]>();
  readonly rows = input.required<readonly T[]>();
  readonly loading = input(false);
  readonly loadingText = input('Cargando...');
  readonly emptyMessage = input('No hay resultados.');

  private readonly cells = contentChildren(UiCellDirective);
  protected readonly templates = computed(
    () => new Map(this.cells().map((c) => [c.key(), c.template] as const)),
  );
}
