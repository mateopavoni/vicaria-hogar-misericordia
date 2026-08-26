import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Notification } from './notification.interface';

// título corto según el tipo de evento que manda el backend
const EVENT_TITLES: Record<string, string> = {
  NuevoUsuarioPendiente: 'Nueva solicitud de cuenta',
  CuentaBloqueada: 'Cuenta bloqueada',
};

// forma cruda que devuelve GET /api/notifications
interface BackendNotification {
  id: string;
  description: string;
  eventType: string;
  linkUrl: string | null;
  isRead: boolean;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private http = inject(HttpClient);

  private mapNotification(n: BackendNotification): Notification {
    return {
      id: n.id,
      title: EVENT_TITLES[n.eventType] ?? n.eventType,
      message: n.description,
      isRead: n.isRead,
      createdAt: n.createdAt,
    };
  }

  // Signal con la lista de notificaciones
  private notificationsSignal = signal<Notification[]>([]);

  // Computed signal para el contador de no leídas
  public unreadCount = computed(() => 
    this.notificationsSignal().filter(n => !n.isRead).length
  );

  public notifications = this.notificationsSignal.asReadonly();

  // Carga inicial de notificaciones
  loadNotifications() {
    this.http.get<BackendNotification[]>('/api/notifications').subscribe({
      next: (data) => this.notificationsSignal.set(data.map((n) => this.mapNotification(n))),
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