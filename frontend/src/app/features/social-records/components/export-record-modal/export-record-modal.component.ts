import { Component, output, signal } from '@angular/core';
import { UiButtonComponent, UiModalComponent } from '../../../../shared/ui';
import { EXPORT_SECTION_OPTIONS, ExportSections } from '../../utils/record-export.util';

/**
 * SCRUM-253: elegir qué secciones incluir al exportar la ficha (DUMB). Se puede descargar el
 * PDF directo al dispositivo o abrir la vista de impresión.
 */
@Component({
  selector: 'app-export-record-modal',
  imports: [UiModalComponent, UiButtonComponent],
  template: `
    <ui-modal title="Exportar ficha" subtitle="Elegí qué incluir en el archivo" size="md" (closed)="closed.emit()">
      <fieldset class="flex flex-col gap-2">
        <legend class="sr-only">Secciones a incluir</legend>
        @for (option of options; track option.id) {
          <label
            class="flex cursor-pointer items-start gap-3 rounded-lg border px-4 py-3"
            [class]="selected() === option.id ? 'border-[#4167D9] bg-[#EEF2FF]' : 'border-slate-200 bg-white hover:bg-slate-50'"
          >
            <input
              type="radio"
              name="export-sections"
              class="mt-1"
              [value]="option.id"
              [checked]="selected() === option.id"
              (change)="selected.set(option.id)"
            />
            <span>
              <span class="block text-[13px] font-semibold text-slate-800">{{ option.label }}</span>
              <span class="block text-[12px] text-slate-500">{{ option.description }}</span>
            </span>
          </label>
        }
      </fieldset>
      <p class="mt-3 text-[12px] text-slate-400">
        El archivo no incluye datos de otros usuarios del sistema ni del contacto de la ficha.
        Queda registrado quién lo exportó y cuándo.
      </p>

      <ng-container ngProjectAs="[modal-footer]">
        <button uiButton variant="secondary" type="button" (click)="closed.emit()">Cancelar</button>
        <button uiButton variant="secondary" type="button" (click)="print.emit(selected())">Imprimir</button>
        <button uiButton type="button" (click)="download.emit(selected())">Descargar PDF</button>
      </ng-container>
    </ui-modal>
  `,
})
export class ExportRecordModalComponent {
  readonly options = EXPORT_SECTION_OPTIONS;
  selected = signal<ExportSections>('todo');

  download = output<ExportSections>();
  print = output<ExportSections>();
  closed = output<void>();
}
