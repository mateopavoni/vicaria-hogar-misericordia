import { Component, inject,computed } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-sidebar',
  imports: [RouterLink,RouterLinkActive],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.css',
})
export class SidebarComponent {

private authService = inject(AuthService);
onLogout(): void {
    this.authService.logout();
  }
public userRole = computed(() => this.authService.user()?.role);
public isReferente = computed(() => this.authService.user()?.role === 'Referente');
}

