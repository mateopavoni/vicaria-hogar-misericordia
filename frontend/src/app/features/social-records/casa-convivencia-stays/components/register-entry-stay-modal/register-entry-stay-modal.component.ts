import { Component, EventEmitter, Output, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { UiButtonComponent, UiFormFieldComponent, UiInputDirective, UiModalComponent } from '../../../../../shared/ui';
import { todayLocalIso } from '../../../../../shared/utils/date.util';

export interface RegisterEntrySubmitData {
  entryDate: string;
}

@Component({
  selector: 'app-register-entry-stay-modal',
  standalone: true,
  imports: [ReactiveFormsModule, UiModalComponent, UiButtonComponent, UiFormFieldComponent, UiInputDirective],
  templateUrl: './register-entry-stay-modal.component.html'
})
export class RegisterEntryStayModalComponent {
  private fb = inject(FormBuilder);

  @Output() cancel = new EventEmitter<void>();
  @Output() confirm = new EventEmitter<RegisterEntrySubmitData>();

  loading = signal(false);

  form = this.fb.group({
    entryDate: [todayLocalIso(), [Validators.required]]
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
