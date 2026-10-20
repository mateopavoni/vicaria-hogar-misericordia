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
 * SCRUM-17/SCRUM-197 (AC): "el usuario puede convertir un evento personal en general
 * si lo decide posteriormente" — botón "Hacer general", solo para eventos con
 * scope 'personal' y cuando `canConvert` lo habilita (también lo decide el padre).
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
