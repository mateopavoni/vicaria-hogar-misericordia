import {Component,EventEmitter,Input,Output,signal} from '@angular/core';
import {ManagedUser} from '../../interfaces/user.interface';
import {UserRole} from './../../../../core/auth/userRole';

// nombres lindos para mostrar en pantalla
// bug reportado 2026-09-23: faltaba CoordinadorDeCasaConvivencia, no se podía aprobar
// una cuenta nueva con ese rol desde este modal.
const ROLE_LABELS: Record<UserRole, string> = {
  Referente: 'Referente',
  DirectoraDeCasona: 'Directora de Casona',
  Escucha: 'Escucha',
  CoordinadorDeCasaConvivencia: 'Coordinador de Casa de Convivencia',
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
    'Escucha',
    'CoordinadorDeCasaConvivencia'
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