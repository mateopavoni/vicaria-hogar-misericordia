import { NgTemplateOutlet } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { UiButtonComponent, UiInputDirective } from '../../../../shared/ui';
import {
  REPORT_FIELD_HINTS,
  REPORT_FIELD_KEYS,
  REPORT_FIELD_LABELS,
  ReportFieldKey,
  ReportFieldValue,
} from '../../interfaces/report-fields.interface';
import { ReportFieldCatalogService } from '../../services/report-field-catalog.service';

/**
 * SCRUM-275: configuración de los valores de los campos del informe de Cáritas
 * (/dashboard/configuracion/campos-informe). Solo Referente (permiso informes.config).
 * Agregar, editar y desactivar valores; los desactivados dejan de ofrecerse pero las fichas
 * que ya los tienen los conservan.
 */
@Component({
  selector: 'app-report-fields-settings',
  imports: [NgTemplateOutlet, UiButtonComponent, UiInputDirective],
  templateUrl: './report-fields-settings.component.html',
})
export class ReportFieldsSettingsComponent implements OnInit {
  private catalog = inject(ReportFieldCatalogService);

  readonly keys = REPORT_FIELD_KEYS;
  readonly labels = REPORT_FIELD_LABELS;
  readonly hints = REPORT_FIELD_HINTS;
  readonly catalogs = this.catalog.all;

  newLabel = signal<Record<ReportFieldKey, string>>({ gender: '', housing: '', origin: '', document: '', mainReason: '' });
  fieldErrors = signal<Partial<Record<ReportFieldKey, string>>>({});
  editing = signal<{ key: ReportFieldKey; id: string; label: string } | null>(null);
  editError = signal<string | null>(null);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  private successTimeout: ReturnType<typeof setTimeout> | null = null;

  ngOnInit(): void {
    this.catalog.load().subscribe();
  }

  setNew(key: ReportFieldKey, text: string): void {
    this.newLabel.update((all) => ({ ...all, [key]: text }));
  }

  add(key: ReportFieldKey): void {
    this.catalog.add(key, this.newLabel()[key]).subscribe((result) => {
      if (!result.ok) {
        this.fieldErrors.update((e) => ({ ...e, [key]: result.message }));
        return;
      }
      this.fieldErrors.update((e) => ({ ...e, [key]: undefined }));
      this.setNew(key, '');
      this.showSuccess(`Se agregó "${result.value.label}".`);
    });
  }

  startEdit(key: ReportFieldKey, value: ReportFieldValue): void {
    this.editError.set(null);
    this.editing.set({ key, id: value.id, label: value.label });
  }

  setEditLabel(label: string): void {
    this.editing.update((e) => (e ? { ...e, label } : e));
  }

  saveEdit(): void {
    const edit = this.editing();
    if (!edit) return;
    this.catalog.rename(edit.key, edit.id, edit.label).subscribe((result) => {
      if (!result.ok) {
        this.editError.set(result.message);
        return;
      }
      this.editing.set(null);
      this.showSuccess('Valor actualizado.');
    });
  }

  cancelEdit(): void {
    this.editing.set(null);
    this.editError.set(null);
  }

  toggle(key: ReportFieldKey, value: ReportFieldValue): void {
    this.catalog.setActive(key, value.id, !value.active).subscribe((result) => {
      if (!result.ok) {
        this.errorMessage.set(result.message);
        return;
      }
      this.errorMessage.set(null);
      this.showSuccess(result.value.active ? `"${value.label}" activado.` : `"${value.label}" desactivado.`);
    });
  }

  private showSuccess(message: string): void {
    this.successMessage.set(message);
    if (this.successTimeout) clearTimeout(this.successTimeout);
    this.successTimeout = setTimeout(() => this.successMessage.set(null), 4000);
  }
}
