import { Component, inject, input, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { UiButtonComponent, UiFormFieldComponent, UiInputDirective, UiModalComponent } from '../../../../shared/ui';

export const STATUS_REASON_MAX = 300;

/** SCRUM-264: cambio manual de estado Activo/Inactivo con motivo obligatorio (DUMB). */
@Component({
  selector: 'app-status-change-modal',
  imports: [ReactiveFormsModule, UiModalComponent, UiButtonComponent, UiFormFieldComponent, UiInputDirective],
  template: `
    <ui-modal
      [title]="'Marcar como ' + targetLabel()"
      subtitle="El cambio queda en el historial de estados con fecha, usuario y motivo."
      size="md"
      [closeOnBackdrop]="false"
      (closed)="closed.emit()"
    >
      <form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col gap-4">
        <ui-form-field label="Motivo del cambio" for="status-reason" [required]="true" [error]="reasonError">
          <textarea
            id="status-reason"
            uiInput
            rows="4"
            formControlName="reason"
            [attr.maxlength]="max"
            placeholder="Ej.: internación, viaje, vuelve a asistir…"
          ></textarea>
        </ui-form-field>

        @if (errorMessage()) {
          <p class="text-[12px] font-semibold text-rose-500" role="alert">{{ errorMessage() }}</p>
        }
      </form>

      <ng-container ngProjectAs="[modal-footer]">
        <button uiButton variant="secondary" type="button" [disabled]="saving()" (click)="closed.emit()">Cancelar</button>
        <button uiButton type="button" [loading]="saving()" [disabled]="saving()" (click)="submit()">Confirmar</button>
      </ng-container>
    </ui-modal>
  `,
})
export class StatusChangeModalComponent {
  private fb = inject(FormBuilder);

  targetLabel = input.required<string>();
  saving = input(false);
  errorMessage = input<string | null>(null);

  confirmed = output<string>();
  closed = output<void>();

  readonly max = STATUS_REASON_MAX;
  submitted = false;

  form = this.fb.nonNullable.group({
    reason: ['', [Validators.required, Validators.maxLength(STATUS_REASON_MAX)]],
  });

  get reasonError(): string | null {
    if (!this.submitted) return null;
    const value = this.form.controls.reason.value.trim();
    if (!value) return 'El motivo es obligatorio.';
    if (value.length < 5) return 'Describí el motivo con al menos 5 caracteres.';
    return null;
  }

  submit(): void {
    this.submitted = true;
    if (this.reasonError) {
      this.form.markAllAsTouched();
      return;
    }
    this.confirmed.emit(this.form.controls.reason.value.trim());
  }
}
