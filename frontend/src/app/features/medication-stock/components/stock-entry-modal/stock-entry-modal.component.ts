import { Component, OnInit, computed, inject, input, output } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { UiButtonComponent, UiFormFieldComponent, UiInputDirective, UiModalComponent } from '../../../../shared/ui';
import { isBeforeToday, todayLocalIso } from '../../../../shared/utils/date.util';
import { MedicationStockItem, RegisterStockEntryRequest } from '../../interfaces/medication-stock.interface';

const NEW = '__new__';

function notBeforeToday(control: AbstractControl): ValidationErrors | null {
  return control.value && isBeforeToday(control.value) ? { pastDate: true } : null;
}

/**
 * SCRUM-246: registrar el ingreso de un medicamento (DUMB). Se elige uno existente o se da de
 * alta uno nuevo; cantidad, vencimiento (no puede ser anterior a hoy) y proveedor.
 */
@Component({
  selector: 'app-stock-entry-modal',
  imports: [ReactiveFormsModule, UiModalComponent, UiButtonComponent, UiFormFieldComponent, UiInputDirective],
  templateUrl: './stock-entry-modal.component.html',
})
export class StockEntryModalComponent implements OnInit {
  private fb = inject(FormBuilder);

  items = input<MedicationStockItem[]>([]);
  /** Medicamento preseleccionado (al registrar desde una fila). */
  preselectedId = input<string | null>(null);
  saving = input(false);
  errorMessage = input<string | null>(null);

  saved = output<RegisterStockEntryRequest>();
  closed = output<void>();

  readonly newOption = NEW;
  readonly today = todayLocalIso();
  submitted = false;

  form = this.fb.nonNullable.group({
    medication: ['', [Validators.required]],
    newName: ['', [Validators.maxLength(100)]],
    quantity: [1, [Validators.required, Validators.min(1), Validators.max(100000)]],
    expirationDate: ['', [Validators.required, notBeforeToday]],
    supplier: ['', [Validators.required, Validators.maxLength(100)]],
  });

  private formValue = toSignal(this.form.valueChanges, { initialValue: this.form.getRawValue() });
  isNew = computed(() => this.formValue().medication === NEW);

  ngOnInit(): void {
    const id = this.preselectedId();
    if (id) {
      this.form.controls.medication.setValue(id);
    }
  }

  get medicationError(): string | null {
    return this.submitted && this.form.controls.medication.errors ? 'Elegí un medicamento o dalo de alta.' : null;
  }

  get newNameError(): string | null {
    if (!this.submitted || !this.isNew()) return null;
    const name = this.form.controls.newName.value.trim();
    return name.length < 3 ? 'El nombre debe tener al menos 3 caracteres.' : null;
  }

  get quantityError(): string | null {
    const c = this.form.controls.quantity;
    if (!this.submitted || !c.errors) return null;
    return 'La cantidad debe ser un número entero mayor a 0.';
  }

  get expirationError(): string | null {
    const errors = this.form.controls.expirationDate.errors;
    if (!this.submitted || !errors) return null;
    return errors['required'] ? 'La fecha de vencimiento es obligatoria.' : 'La fecha de vencimiento no puede ser anterior a hoy.';
  }

  get supplierError(): string | null {
    const errors = this.form.controls.supplier.errors;
    if (!this.submitted || !errors) return null;
    return errors['required'] ? 'El proveedor es obligatorio.' : 'El proveedor no puede superar los 100 caracteres.';
  }

  onClose(): void {
    this.closed.emit();
  }

  submit(): void {
    this.submitted = true;
    const value = this.form.getRawValue();
    const quantityOk = Number.isInteger(value.quantity);
    if (this.form.invalid || !quantityOk || this.newNameError) {
      this.form.markAllAsTouched();
      return;
    }
    const isNew = value.medication === NEW;
    this.saved.emit({
      medicationId: isNew ? null : value.medication,
      newName: isNew ? value.newName.trim() : null,
      quantity: value.quantity,
      expirationDate: value.expirationDate,
      supplier: value.supplier.trim(),
    });
  }
}
