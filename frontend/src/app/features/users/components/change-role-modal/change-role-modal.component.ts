import { Component, EventEmitter, Input, OnInit, Output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ManagedUser } from '../../interfaces/user.interface';
import { UserRole } from '../../../../core/auth/userRole';

@Component({
  selector: 'app-change-role-modal',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './change-role-modal.component.html', // O el template inline
})
export class ChangeRoleModalComponent implements OnInit {
  @Input() user: ManagedUser | null = null;
  @Output() close = new EventEmitter<void>();
  @Output() changeRole = new EventEmitter<UserRole>();

  availableRoles: UserRole[] = ['Referente', 'DirectoradeCasona', 'Escucha'];
  selectedRole = signal<UserRole>('Escucha');

  ngOnInit(): void {
    if (this.user?.role) {
      this.selectedRole.set(this.user.role as UserRole);
    }
  }

  onClose(): void {
    this.close.emit();
  }

  onConfirm(): void {
    this.changeRole.emit(this.selectedRole());
  }
}