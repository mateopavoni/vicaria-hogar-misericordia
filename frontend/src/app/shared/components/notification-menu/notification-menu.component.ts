import { Component, inject, signal, ElementRef, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Notification } from './../../../core/notification/notification.interface';
import { NotificationService } from './../../../core/notification/notification.service';

@Component({
  selector: 'app-notification-menu',
  imports: [CommonModule],
  templateUrl: './notification-menu.component.html',
  styleUrl: './notification-menu.component.css',
})
export class NotificationMenuComponent {
  public notificationService = inject(NotificationService);
  private elementRef = inject(ElementRef);
  private router = inject(Router);

  isOpen = signal<boolean>(false);

  ngOnInit() {
    this.notificationService.loadNotifications();
  }

  togglePanel() {
    this.isOpen.update(value => !value);
  }

  // al hacer clic: marca como leída y va al recurso (ej. la ficha sin observaciones)
  open(notification: Notification) {
    if (!notification.isRead) {
      this.notificationService.markAsRead(notification.id);
    }
    if (notification.linkUrl) {
      this.isOpen.set(false);
      this.router.navigateByUrl(notification.linkUrl);
    }
  }

  // convierte la fecha cruda del backend en algo legible, tipo "hace 5 min"
  timeAgo(dateStr: string): string {
    const date = new Date(dateStr);
    const seconds = Math.floor((Date.now() - date.getTime()) / 1000);

    if (seconds < 60) return 'hace un momento';
    if (seconds < 3600) return `hace ${Math.floor(seconds / 60)} min`;
    if (seconds < 86400) return `hace ${Math.floor(seconds / 3600)} h`;
    if (seconds < 172800) return 'ayer';

    return date.toLocaleDateString('es-AR', { day: 'numeric', month: 'short' });
  }

  // Cerrar desplegable al hacer clic fuera del componente
  @HostListener('document:click', ['$event'])
  onClickOutside(event: Event) {
    if (!this.elementRef.nativeElement.contains(event.target)) {
      this.isOpen.set(false);
    }
  }
}
