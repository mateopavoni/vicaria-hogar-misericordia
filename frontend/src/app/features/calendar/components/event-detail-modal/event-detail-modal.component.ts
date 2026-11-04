import { Component, computed, input, output } from '@angular/core';
import { UiButtonComponent, UiModalComponent } from '../../../../shared/ui';
import { CalendarEvent } from '../../interfaces/calendar-event.interface';

/**
 * Detalle de un evento del calendario (título, horario, descripción y autor).
 * Qué botones se muestran lo decide CalendarComponent con `canEdit` y `canConvert`.
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

  // false si no puede convertir ESTE evento personal en general
  // (ver CalendarComponent.canConvertToGeneral)
  canConvert = input(false);

  closed = output<void>();
  editRequested = output<CalendarEvent>();
  deleteRequested = output<CalendarEvent>();
  convertRequested = output<CalendarEvent>();

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

  onConvert(): void {
    this.convertRequested.emit(this.event());
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
