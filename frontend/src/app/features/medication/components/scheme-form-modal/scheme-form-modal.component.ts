import { Component, computed, inject, input, output } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { UiButtonComponent, UiFormFieldComponent, UiInputDirective, UiModalComponent } from '../../../../shared/ui';
import { CreateMedicationSchemeRequest, MedicationCatalogItem } from '../../interfaces/medication.interface';

/**
 * SCRUM-234: alta de un esquema de medicación (DUMB). Selector del catálogo (solo se puede
 * elegir un medicamento que exista), dosis y hora. Si el medicamento elegido no tiene stock
 * en farmacia se alerta, pero se puede guardar: queda "pendiente de stock".
 */
@Component({
  selector: 'app-medication-scheme-form-modal',
  imports: [ReactiveFormsModule, UiModalComponent, UiButtonComponent, UiFormFieldComponent, UiInputDirective],
  templateUrl: './scheme-form-modal.component.html',
})
export class MedicationSchemeFormModalComponent {
  private fb = inject(FormBuilder);

  personId = input.required<string>();
  catalog = input<MedicationCatalogItem[]>([]);
  catalogLoading = input(false);
  saving = input(false);
  errorMessage = input<string | null>(null);

  saved = output<CreateMedicationSchemeRequest>();
  closed = output<void>();

  submitted = false;

  form = this.fb.nonNullable.group({
    medicationId: ['', [Validators.required]],
    dose: ['', [Validators.required, Validators.maxLength(50)]],
    time: ['08:00', [Validators.required]],
  });

  // reactive forms no es signals: para que la alerta de stock aparezca apenas se elige el
  // medicamento, se convierte valueChanges a signal
  private formValue = toSignal(this.form.valueChanges, { initialValue: this.form.getRawValue() });

  selected = computed(() => this.catalog().find((m) => m.id === this.formValue().medicationId) ?? null);
  noStock = computed(() => (this.selected()?.pharmacyStock ?? 1) <= 0);

  get medicationError(): string | null {
    return this.submitted && this.form.controls.medicationId.errors ? 'Elegí un medicamento del catálogo.' : null;
  }

  get doseError(): string | null {
    const errors = this.form.controls.dose.errors;
    if (!this.submitted || !errors) {
      return null;
    }
    return errors['required'] ? 'La dosis es obligatoria.' : 'La dosis no puede superar los 50 caracteres.';
  }

  get timeError(): string | null {
    return this.submitted && this.form.controls.time.errors ? 'La hora es obligatoria.' : null;
  }

  onClose(): void {
    this.closed.emit();
  }

  submit(): void {
    this.submitted = true;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const { medicationId, dose, time } = this.form.getRawValue();
    this.saved.emit({ personId: this.personId(), medicationId, dose: dose.trim(), time });
  }
}
