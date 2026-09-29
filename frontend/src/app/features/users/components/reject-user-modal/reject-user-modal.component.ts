import { Component, EventEmitter, Input, Output, signal } from '@angular/core';

import { ManagedUser } from '../../interfaces/user.interface';
import { UiButtonComponent, UiFormFieldComponent, UiInputDirective, UiModalComponent } from '../../../../shared/ui';

@Component({
  selector: 'app-reject-user-modal',
  imports: [UiModalComponent, UiButtonComponent, UiFormFieldComponent, UiInputDirective],
  templateUrl: './reject-user-modal.component.html',
})
export class RejectUserModalComponent {

  @Input() user: ManagedUser | null = null;

  @Output() close = new EventEmitter<void>();

  @Output() reject = new EventEmitter<string>();


  reason = signal('');


  updateReason(value: string): void {this.reason.set(value);}


  closeModal(): void {this.close.emit();}


  rejectUser(): void {

    const reason = this.reason().trim();

    if (!reason) {
      return;
    }

    this.reject.emit(reason);
  }

}
