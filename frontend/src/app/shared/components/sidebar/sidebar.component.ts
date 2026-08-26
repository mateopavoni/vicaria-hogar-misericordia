import { Component, inject,computed } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { PermissionService } from '../../../core/auth/permission.service';

@Component({
  selector: 'app-sidebar',
  imports: [RouterLink,RouterLinkActive],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.css',
})
export class SidebarComponent {

permissionService = inject(PermissionService);

private authService = inject(AuthService);
onLogout(): void {
    this.authService.logout();
  }
public userRole = computed(() => this.authService.user()?.role);
public isReferente = computed(() => this.authService.user()?.role === 'Referente');
}

