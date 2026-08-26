import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Notification } from './notification.interface';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private http = inject(HttpClient);

  // Signal con la lista de notificaciones
  private notificationsSignal = signal<Notification[]>([]);

  // Computed signal para el contador de no leídas
  public unreadCount = computed(() => 
    this.notificationsSignal().filter(n => !n.isRead).length
  );

  public notifications = this.notificationsSignal.asReadonly();

  // Carga inicial de notificaciones
  loadNotifications() {
    this.http.get<Notification[]>('/api/notifications').subscribe({
      next: (data) => this.notificationsSignal.set(data),
      error: (err) => console.error('Error cargando notificaciones', err)
    });
  }

  // Marcar una notificación individual como leída
  markAsRead(notificationId: string) {
    // Actualización optimista en el estado local
    this.notificationsSignal.update(list =>
      list.map(n => n.id === notificationId ? { ...n, isRead: true } : n)
    );

    this.http.patch(`/api/notifications/${notificationId}/read`, {}).subscribe({
      error: () => this.loadNotifications() // Rollback si falla
    });
  }

  // Marcar todas como leídas
  markAllAsRead() {
    this.notificationsSignal.update(list =>
      list.map(n => ({ ...n, isRead: true }))
    );

    this.http.patch('/api/notifications/read-all', {}).subscribe({
      error: () => this.loadNotifications()
    });
  }
}