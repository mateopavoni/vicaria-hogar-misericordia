import { Component, inject, input, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { UiButtonComponent, UiFormFieldComponent, UiInputDirective, UiModalComponent } from '../../../../shared/ui';
import { Visit } from '../../interfaces/visit.interface';

/**
 * SCRUM-74 (AC): "las visitas canceladas quedan registradas con motivo opcional" —
 * un ui-confirm-dialog genérico no alcanza acá porque necesita un campo de texto, así
 * que es un modal propio y chico.
 */
@Component({
  selector: 'app-cancel-visit-modal',
  imports: [ReactiveFormsModule, UiModalComponent, UiButtonComponent, UiFormFieldComponent, UiInputDirective],
  templateUrl: './cancel-visit-modal.component.html',
})
export class CancelVisitModalComponent {
  private fb = inject(FormBuilder);

  visit = input.required<Visit>();
  saving = input(false);

  confirmed = output<string | null>();
  cancelled = output<void>();

  form = this.fb.nonNullable.group({
    reason: [''],
  });

  onConfirm(): void {
    const reason = this.form.getRawValue().reason.trim();
    this.confirmed.emit(reason || null);
  }

  onCancel(): void {
    this.cancelled.emit();
  }
}
