import { Component, inject, input, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { UiButtonComponent, UiFormFieldComponent, UiInputDirective, UiModalComponent } from '../../../../shared/ui';

/**
 * SCRUM-218 (AC de SCRUM-54): la asistencia de un día ya cerrado solo la modifica un
 * Referente, y con justificación obligatoria. Modal chico (DUMB): pide el texto y lo emite.
 */
@Component({
  selector: 'app-attendance-justification-modal',
  imports: [ReactiveFormsModule, DatePipe, UiModalComponent, UiButtonComponent, UiFormFieldComponent, UiInputDirective],
  templateUrl: './justification-modal.component.html',
})
export class AttendanceJustificationModalComponent {
  private fb = inject(FormBuilder);

  /** Fecha del día cerrado, YYYY-MM-DD. */
  date = input.required<string>();
  saving = input(false);
  errorMessage = input<string | null>(null);

  confirmed = output<string>();
  cancelled = output<void>();

  submitted = false;

  form = this.fb.nonNullable.group({
    justification: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(300)]],
  });

  get justificationError(): string | null {
    const errors = this.form.controls.justification.errors;
    if (!this.submitted || !errors) {
      return null;
    }
    if (errors['required']) {
      return 'La justificación es obligatoria.';
    }
    if (errors['minlength']) {
      return 'Explicá el motivo con al menos 10 caracteres.';
    }
    return 'La justificación no puede superar los 300 caracteres.';
  }

  get dateAsDate(): Date {
    const [y, m, d] = this.date().split('-').map(Number);
    return new Date(y, m - 1, d);
  }

  onConfirm(): void {
    this.submitted = true;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.confirmed.emit(this.form.getRawValue().justification.trim());
  }

  onCancel(): void {
    this.cancelled.emit();
  }
}
