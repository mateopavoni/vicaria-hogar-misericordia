import { Component, computed, input, output } from '@angular/core';
import { DatePipe } from '@angular/common';
import { UiButtonComponent, UiModalComponent } from '../../../../shared/ui';
import { Collaborator, CollaboratorType } from '../../interfaces/collaborator.interface';

/**
 * Ficha completa de un colaborador, de solo lectura (SCRUM-207): se abre al hacer
 * clic en un resultado de la búsqueda (SCRUM-19/SCRUM-206) y muestra todos los datos,
 * no solo los que aparecen en la tabla de resultados (nombre, teléfono, email, tipo).
 * Desde acá se puede pasar a edición con el botón "Editar".
 */
@Component({
  selector: 'app-collaborator-detail-modal',
  imports: [DatePipe, UiModalComponent, UiButtonComponent],
  templateUrl: './collaborator-detail-modal.component.html',
})
export class CollaboratorDetailModalComponent {
  collaborator = input.required<Collaborator>();

  closed = output<void>();
  editRequested = output<Collaborator>();

  typeLabel = computed(() => {
    const type = this.collaborator().type;
    if (type === CollaboratorType.Volunteer) return 'Voluntario';
    if (type === CollaboratorType.Employee) return 'Empleado';
    return 'Sin especificar';
  });

  onClose(): void {
    this.closed.emit();
  }

  onEdit(): void {
    this.editRequested.emit(this.collaborator());
  }
}
