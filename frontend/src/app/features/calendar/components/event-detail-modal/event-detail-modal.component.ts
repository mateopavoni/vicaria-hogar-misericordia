import { Component, computed, input, output } from '@angular/core';
import { UiButtonComponent, UiModalComponent } from '../../../../shared/ui';
import { CalendarEvent } from '../../interfaces/calendar-event.interface';

/**
 * SCRUM-187: detalle completo de un evento (título, horario, descripción y autor),
 * al hacer clic en un evento del calendario. Las actividades recurrentes precargadas
 * (isRecurring) no tienen botón "Editar"/"Eliminar": son fijas, no las creó ningún
 * usuario. SCRUM-16 (AC): el autor del evento puede editarlo/eliminarlo, y cualquier
 * Referente también — esa cuenta ya la hace el padre (CalendarComponent) evento por
 * evento; `canEdit` acá es el resultado de esa cuenta, no solo el permiso de rol.
 */
@Component({
  selector: 'app-event-detail-modal',
  imports: [UiModalComponent, UiButtonComponent],
  templateUrl: './event-detail-modal.component.html',
})
export class EventDetailModalComponent {
  event = input.required<CalendarEvent>();

  // false si el usuario actual no puede editar/eliminar ESTE evento puntual
  // (ver CalendarComponent.canManageEvent)
  canEdit = input(false);

  closed = output<void>();
  editRequested = output<CalendarEvent>();
  deleteRequested = output<CalendarEvent>();

  schedule = computed(() => {
    const e = this.event();
    return `${this.formatDateTime(e.start)} – ${this.formatTime(e.end)}`;
  });

  onClose(): void {
    this.closed.emit();
  }

  onEdit(): void {
    this.editRequested.emit(this.event());
  }

  onDelete(): void {
    this.deleteRequested.emit(this.event());
  }

  private formatDateTime(value: string): string {
    const d = new Date(value);
    const date = d.toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit', year: 'numeric' });
    return `${date} ${this.formatTime(value)}`;
  }

  private formatTime(value: string): string {
    const d = new Date(value);
    return d.toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' });
  }
}
