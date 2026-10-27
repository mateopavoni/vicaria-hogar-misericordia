import { Component, inject, input, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { UiButtonComponent, UiFormFieldComponent, UiInputDirective, UiModalComponent } from '../../../../shared/ui';
import { todayLocalIso } from '../../../../shared/utils/date.util';
import { RegisterEvaluationRequest } from '../../interfaces/psychiatric-evaluation.interface';

/** SCRUM-260: formulario de carga de una evaluación psiquiátrica (DUMB). */
@Component({
  selector: 'app-evaluation-form-modal',
  imports: [ReactiveFormsModule, UiModalComponent, UiButtonComponent, UiFormFieldComponent, UiInputDirective],
  template: `
    <ui-modal
      title="Registrar evaluación psiquiátrica"
      subtitle="La evaluación anterior pasa al historial. Vence a los 6 meses desde su fecha."
      size="md"
      [closeOnBackdrop]="false"
      (closed)="closed.emit()"
    >
      <form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col gap-4">
        <ui-form-field label="Fecha de la evaluación" for="eval-date" [required]="true" [error]="dateError">
          <input id="eval-date" uiInput type="date" formControlName="date" [attr.max]="today" />
        </ui-form-field>

        <ui-form-field label="Profesional" for="eval-professional" [required]="true" [error]="professionalError">
          <input id="eval-professional" uiInput type="text" formControlName="professional" maxlength="100" />
        </ui-form-field>

        <ui-form-field label="Diagnóstico" for="eval-diagnosis" [required]="true" [error]="diagnosisError">
          <textarea id="eval-diagnosis" uiInput rows="5" formControlName="diagnosis" maxlength="2000"></textarea>
        </ui-form-field>

        @if (errorMessage()) {
          <p class="text-[12px] font-semibold text-rose-500" role="alert">{{ errorMessage() }}</p>
        }
      </form>

      <ng-container ngProjectAs="[modal-footer]">
        <button uiButton variant="secondary" type="button" [disabled]="saving()" (click)="closed.emit()">Cancelar</button>
        <button uiButton type="button" [loading]="saving()" [disabled]="saving()" (click)="submit()">Guardar</button>
      </ng-container>
    </ui-modal>
  `,
})
export class EvaluationFormModalComponent {
  private fb = inject(FormBuilder);

  personId = input.required<string>();
  saving = input(false);
  errorMessage = input<string | null>(null);

  saved = output<RegisterEvaluationRequest>();
  closed = output<void>();

  readonly today = todayLocalIso();
  submitted = false;

  form = this.fb.nonNullable.group({
    date: [todayLocalIso(), [Validators.required]],
    professional: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(100)]],
    diagnosis: ['', [Validators.required, Validators.maxLength(2000)]],
  });

  get dateError(): string | null {
    const value = this.form.controls.date.value;
    if (!this.submitted) return null;
    if (!value) return 'La fecha de la evaluación es obligatoria.';
    return value > this.today ? 'La fecha no puede ser futura.' : null;
  }

  get professionalError(): string | null {
    const errors = this.form.controls.professional.errors;
    if (!this.submitted || !errors) return null;
    return errors['required'] ? 'El profesional es obligatorio.' : 'El profesional debe tener entre 3 y 100 caracteres.';
  }

  get diagnosisError(): string | null {
    const errors = this.form.controls.diagnosis.errors;
    if (!this.submitted || !errors) return null;
    return errors['required'] ? 'El diagnóstico es obligatorio.' : 'El diagnóstico no puede superar los 2000 caracteres.';
  }

  submit(): void {
    this.submitted = true;
    if (this.form.invalid || this.dateError || !this.form.controls.professional.value.trim() || !this.form.controls.diagnosis.value.trim()) {
      this.form.markAllAsTouched();
      return;
    }
    const { date, professional, diagnosis } = this.form.getRawValue();
    this.saved.emit({ personId: this.personId(), date, professional: professional.trim(), diagnosis: diagnosis.trim() });
  }
}
