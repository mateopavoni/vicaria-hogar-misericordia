import {
  Component,
  EventEmitter,
  Input,
  Output,
  signal
} from '@angular/core';

import {
  ManagedUser,
  UserRole
} from '../../interfaces/user.interface';

@Component({
  selector: 'app-approve-user-modal',
  imports: [],
  templateUrl: './approve-user-modal.component.html',
})
export class ApproveUserModalComponent {

  @Input() user: ManagedUser | null = null;

  @Output() close = new EventEmitter<void>();

  @Output() approve = new EventEmitter<UserRole>();


  selectedRole = signal<UserRole | null>(null);


  roles: UserRole[] = [
    'Referente',
    'Directora de Casona',
    'Escucha'
  ];


  selectRole(role: UserRole): void {
    this.selectedRole.set(role);
  }


  closeModal(): void {
    this.close.emit();
  }


  approveUser(): void {

    const role = this.selectedRole();

    if (!role) {
      return;
    }

    this.approve.emit(role);
  }

}