import { Component, computed, inject, input, output } from '@angular/core';
import { UiFormFieldComponent, UiInputDirective } from '../../../../shared/ui';
import {
  PersonReportData,
  REPORT_FIELD_HINTS,
  REPORT_FIELD_KEYS,
  REPORT_FIELD_LABELS,
  ReportFieldKey,
  ReportFieldValue,
} from '../../interfaces/report-fields.interface';
import { ReportFieldCatalogService } from '../../services/report-field-catalog.service';

/**
 * SCRUM-275: un select por campo del informe de Cáritas (DUMB). Opcionales: se puede dejar en
 * blanco. Ofrece solo los valores activos, más el valor actual si fue desactivado después.
 */
@Component({
  selector: 'app-report-fields-select',
  imports: [UiFormFieldComponent, UiInputDirective],
  template: `
    <div class="grid gap-4 sm:grid-cols-2">
      @for (key of keys; track key) {
        <ui-form-field [label]="labels[key]" [for]="idPrefix() + key" [hint]="hints[key]">
          <select
            [id]="idPrefix() + key"
            uiInput
            [value]="value()[key] ?? ''"
            (change)="pick(key, $any($event.target).value)"
          >
            <option value="">Sin cargar</option>
            @for (option of options()[key]; track option.id) {
              <option [value]="option.id" [selected]="option.id === value()[key]">
                {{ option.label }}{{ option.active ? '' : ' (desactivado)' }}
              </option>
            }
          </select>
        </ui-form-field>
      }
    </div>
  `,
})
export class ReportFieldsSelectComponent {
  private catalog = inject(ReportFieldCatalogService);

  value = input<PersonReportData>({});
  idPrefix = input('report-field-');
  changed = output<PersonReportData>();

  readonly keys = REPORT_FIELD_KEYS;
  readonly labels = REPORT_FIELD_LABELS;
  readonly hints = REPORT_FIELD_HINTS;

  constructor() {
    this.catalog.load().subscribe();
  }

  options = computed(() => {
    const current = this.value();
    const out = {} as Record<ReportFieldKey, ReportFieldValue[]>;
    for (const key of REPORT_FIELD_KEYS) {
      out[key] = this.catalog.values(key).filter((v) => v.active || v.id === current[key]);
    }
    return out;
  });

  pick(key: ReportFieldKey, id: string): void {
    this.changed.emit({ ...this.value(), [key]: id || null });
  }
}
