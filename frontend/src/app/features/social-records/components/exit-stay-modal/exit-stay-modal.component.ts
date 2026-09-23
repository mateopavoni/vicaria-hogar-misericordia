import { Component, EventEmitter, Output, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ExitReason } from './../../interfaces/stay.interface';

export interface ExitStaySubmitData {
  exitDate: string;
  exitReason: ExitReason;
  exitDetail?: string | null;
}

@Component({
  selector: 'app-exit-stay-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './exit-stay-modal.component.html'
})
export class ExitStayModalComponent {
  private fb = inject(FormBuilder);

  @Output() cancel = new EventEmitter<void>();
  @Output() confirm = new EventEmitter<ExitStaySubmitData>();

  loading = signal(false);

  form = this.fb.group({
    exitDate: [new Date().toISOString().substring(0, 10), [Validators.required]],
    exitReason: ['Alta voluntaria' as ExitReason, [Validators.required]],
    exitDetail: ['']
  });

  constructor() {
    // Validación condicional: Si selecciona 'Otro', el detalle pasa a ser obligatorio
    this.form.controls.exitReason.valueChanges.subscribe(reason => {
      const detailControl = this.form.controls.exitDetail;
      if (reason === 'Otro') {
        detailControl.setValidators([Validators.required, Validators.minLength(3)]);
      } else {
        detailControl.clearValidators();
        detailControl.setValue('');
      }
      detailControl.updateValueAndValidity();
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { exitDate, exitReason, exitDetail } = this.form.getRawValue();

    this.confirm.emit({
      exitDate: exitDate!,
      exitReason: exitReason as ExitReason,
      exitDetail: exitReason === 'Otro' ? exitDetail : null
    });
  }
}