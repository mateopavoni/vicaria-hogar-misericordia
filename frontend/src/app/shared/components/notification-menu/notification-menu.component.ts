import { Component, inject, signal, ElementRef, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
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

  isOpen = signal<boolean>(false);

  ngOnInit() {
    this.notificationService.loadNotifications();
  }

  togglePanel() {
    this.isOpen.update(value => !value);
  }

  // Cerrar desplegable al hacer clic fuera del componente
  @HostListener('document:click', ['$event'])
  onClickOutside(event: Event) {
    if (!this.elementRef.nativeElement.contains(event.target)) {
      this.isOpen.set(false);
    }
  }
}
