import { Component, inject } from '@angular/core';
import { AuthService } from '../../../core/auth/auth.service';
import { NotificationMenuComponent } from '../notification-menu/notification-menu.component';
import { UserRole } from '../../../core/auth/userRole';

// nombres lindos para mostrar en pantalla, el código interno sigue siendo el de UserRole
const ROLE_LABELS: Record<UserRole, string> = {
  Referente: 'Referente',
  DirectoraDeCasona: 'Directora de Casona',
  Escucha: 'Escucha',
};

@Component({
  selector: 'app-topbar',
  imports: [NotificationMenuComponent],
  templateUrl: './topbar.component.html',
  styleUrl: './topbar.component.css',
})
export class TopbarComponent {
  public authService = inject(AuthService);

  roleLabel(role: UserRole | null): string {
    return role ? ROLE_LABELS[role] : '';
  }
}
