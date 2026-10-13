import { Component, OnInit, inject, input, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { UiButtonComponent, UiFormFieldComponent, UiInputDirective, UiModalComponent } from '../../../../shared/ui';
import { MedicationStockItem } from '../../interfaces/medication-stock.interface';

/** SCRUM-246: configurar el umbral de stock bajo de un medicamento (DUMB). */
@Component({
  selector: 'app-stock-threshold-modal',
  imports: [ReactiveFormsModule, UiModalComponent, UiButtonComponent, UiFormFieldComponent, UiInputDirective],
  template: `
    <ui-modal title="Umbral de stock bajo" [subtitle]="item().name" size="sm" [closeOnBackdrop]="false" (closed)="closed.emit()">
      <form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col gap-4">
        <ui-form-field label="Avisar cuando queden" for="threshold" [required]="true" [error]="error">
          <input id="threshold" uiInput type="number" min="0" step="1" formControlName="threshold" />
        </ui-form-field>
        <p class="text-[12px] text-slate-400">
          Se muestra la alerta de stock bajo cuando hay esta cantidad o menos. Stock actual: {{ item().quantity }}.
        </p>
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
export class StockThresholdModalComponent implements OnInit {
  private fb = inject(FormBuilder);

  item = input.required<MedicationStockItem>();
  saving = input(false);
  errorMessage = input<string | null>(null);

  saved = output<number>();
  closed = output<void>();

  submitted = false;
  form = this.fb.nonNullable.group({
    threshold: [0, [Validators.required, Validators.min(0), Validators.max(100000)]],
  });

  ngOnInit(): void {
    this.form.controls.threshold.setValue(this.item().lowStockThreshold);
  }

  get error(): string | null {
    return this.submitted && (this.form.invalid || !Number.isInteger(this.form.getRawValue().threshold))
      ? 'El umbral debe ser un número entero de 0 o más.'
      : null;
  }

  submit(): void {
    this.submitted = true;
    if (this.form.invalid || !Number.isInteger(this.form.getRawValue().threshold)) {
      return;
    }
    this.saved.emit(this.form.getRawValue().threshold);
  }
}
