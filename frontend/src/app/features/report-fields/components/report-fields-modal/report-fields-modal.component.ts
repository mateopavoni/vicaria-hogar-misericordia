import { Component, OnInit, input, output, signal } from '@angular/core';
import { UiButtonComponent, UiModalComponent } from '../../../../shared/ui';
import { PersonReportData } from '../../interfaces/report-fields.interface';
import { ReportFieldsSelectComponent } from '../report-fields-select/report-fields-select.component';

/** SCRUM-275: completar o editar los datos del informe de Cáritas de una ficha (DUMB). */
@Component({
  selector: 'app-report-fields-modal',
  imports: [UiModalComponent, UiButtonComponent, ReportFieldsSelectComponent],
  template: `
    <ui-modal
      title="Datos para el informe de Cáritas"
      subtitle="Son opcionales, pero el informe los necesita para contar a esta persona."
      size="md"
      [closeOnBackdrop]="false"
      (closed)="closed.emit()"
    >
      <app-report-fields-select idPrefix="modal-report-field-" [value]="draft()" (changed)="draft.set($event)" />

      <ng-container ngProjectAs="[modal-footer]">
        <button uiButton variant="secondary" type="button" (click)="closed.emit()">Cancelar</button>
        <button uiButton type="button" (click)="saved.emit(draft())">Guardar</button>
      </ng-container>
    </ui-modal>
  `,
})
export class ReportFieldsModalComponent implements OnInit {
  initial = input<PersonReportData>({});
  saved = output<PersonReportData>();
  closed = output<void>();

  draft = signal<PersonReportData>({});

  // se copia el valor inicial una sola vez al abrir
  ngOnInit(): void {
    this.draft.set({ ...this.initial() });
  }
}
