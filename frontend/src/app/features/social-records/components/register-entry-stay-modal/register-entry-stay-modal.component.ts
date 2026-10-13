import { Component, EventEmitter, Output, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

export interface RegisterEntrySubmitData {
  entryDate: string;
}

@Component({
  selector: 'app-register-entry-stay-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './register-entry-stay-modal.component.html'
})
export class RegisterEntryStayModalComponent {
  private fb = inject(FormBuilder);

  @Output() cancel = new EventEmitter<void>();
  @Output() confirm = new EventEmitter<RegisterEntrySubmitData>();

  loading = signal(false);

  form = this.fb.group({
    entryDate: [new Date().toISOString().substring(0, 10), [Validators.required]]
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { entryDate } = this.form.getRawValue();

    this.confirm.emit({
      entryDate: entryDate!
    });
  }
}