import { Component, computed, input, output } from '@angular/core';
import { UiButtonComponent, UiModalComponent } from '../../../../shared/ui';
import { Visit, VisitStatus } from '../../interfaces/visit.interface';

/**
 * Detalle de una visita — residente, visitante, horario, duración y
 * estado. Permite cambiar el estado (pendiente/realizada/cancelada) y editar.
 * Una visita cancelada queda cerrada: no se edita ni cambia de estado.
 */
@Component({
  selector: 'app-visit-detail-modal',
  imports: [UiModalComponent, UiButtonComponent],
  templateUrl: './visit-detail-modal.component.html',
})
export class VisitDetailModalComponent {
  visit = input.required<Visit>();

  // false si el rol actual no puede editar/cambiar el estado de las visitas
  // (ver permissions.ts, visitas.edit)
  canEdit = input(false);

  closed = output<void>();
  editRequested = output<Visit>();
  statusRequested = output<VisitStatus>();
  cancelRequested = output<Visit>();

  schedule = computed(() => {
    const v = this.visit();
    const start = new Date(v.start);
    const end = new Date(start.getTime() + v.durationMinutes * 60_000);
    const date = start.toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit', year: 'numeric' });
    const fmt = (d: Date) => d.toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' });
    return `${date} · ${fmt(start)} – ${fmt(end)} (${v.durationMinutes} min)`;
  });

  statusLabel = computed(() => {
    switch (this.visit().status) {
      case 'done':
        return 'Realizada';
      case 'cancelled':
        return 'Cancelada';
      default:
        return 'Pendiente';
    }
  });

  canChangeStatus = computed(() => this.canEdit() && this.visit().status !== 'cancelled');

  onClose(): void {
    this.closed.emit();
  }

  onEdit(): void {
    this.editRequested.emit(this.visit());
  }

  markPending(): void {
    this.statusRequested.emit('pending');
  }

  markDone(): void {
    this.statusRequested.emit('done');
  }

  onCancel(): void {
    this.cancelRequested.emit(this.visit());
  }
}
