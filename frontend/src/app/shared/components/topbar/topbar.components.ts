import { Component, inject } from '@angular/core';
import { AuthService } from '../../../core/auth/auth.service';
import { NotificationMenuComponent } from '../notification-menu/notification-menu.component';

@Component({
  selector: 'app-topbar',
  imports: [NotificationMenuComponent],
  templateUrl: './topbar.component.html',
  styleUrl: './topbar.component.css',
})
export class TopbarComponent {
  public authService = inject(AuthService);
}
