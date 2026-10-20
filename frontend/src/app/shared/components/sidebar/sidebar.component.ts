import { Component, inject,computed } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { PermissionService } from '../../../core/auth/permission.service';
import { SidebarStateService } from '../../layout/sidebar-state.service';

@Component({
  selector: 'app-sidebar',
  imports: [RouterLink,RouterLinkActive],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.css',
})
export class SidebarComponent {

permissionService = inject(PermissionService);
sidebarState = inject(SidebarStateService);

private authService = inject(AuthService);
onLogout(): void {
    this.sidebarState.close();
    this.authService.logout();
  }
public userRole = computed(() => this.authService.user()?.role);
public isReferente = computed(() => this.authService.user()?.role === 'Referente');

// en mobile, navegar o cerrar sesión cierra el drawer (si no, tapa la pantalla siguiente)
closeOnMobile(): void {
  this.sidebarState.close();
}
}

