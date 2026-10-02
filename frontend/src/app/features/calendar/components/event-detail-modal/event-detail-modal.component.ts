import { Component, computed, input, output } from '@angular/core';
import { UiButtonComponent, UiModalComponent } from '../../../../shared/ui';
import { CalendarEvent } from '../../interfaces/calendar-event.interface';

/**
 * SCRUM-187: detalle completo de un evento (título, horario, descripción y autor),
 * al hacer clic en un evento del calendario. Las actividades recurrentes precargadas
 * (isRecurring) no tienen botón "Editar": son fijas, no las creó ningún usuario.
 */
@Component({
  selector: 'app-event-detail-modal',
  imports: [UiModalComponent, UiButtonComponent],
  templateUrl: './event-detail-modal.component.html',
})
export class EventDetailModalComponent {
  event = input.required<CalendarEvent>();

  // false si el rol actual (Escucha) no puede editar eventos
  canEdit = input(false);

  closed = output<void>();
  editRequested = output<CalendarEvent>();

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
