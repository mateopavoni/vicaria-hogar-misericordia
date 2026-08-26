import {Component,EventEmitter,Input,Output,signal} from '@angular/core';
import {ManagedUser} from '../../interfaces/user.interface';
import {UserRole} from './../../../../core/auth/userRole';

// nombres lindos para mostrar en pantalla
const ROLE_LABELS: Record<UserRole, string> = {
  Referente: 'Referente',
  DirectoraDeCasona: 'Directora de Casona',
  Escucha: 'Escucha',
};

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

  roleLabel(role: UserRole): string {
    return ROLE_LABELS[role];
  }


  roles: UserRole[] = [
    'Referente',
    'DirectoraDeCasona',
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