import { Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { UiBackButtonComponent, UiButtonComponent, UiFormFieldComponent, UiInputDirective } from '../../../../shared/ui';
import { AREA_LABELS, AttendanceActivityDef, AttendanceActivitySource } from '../../interfaces/attendance.interface';
import { AttendanceActivitiesService } from '../../services/attendance-activities.service';

const SOURCE_LABELS: Record<AttendanceActivitySource, string> = {
  system: 'Fija',
  calendar: 'Del calendario',
  custom: 'Creada a mano',
};

/**
 * SCRUM-230: configuración de actividades de asistencia (solo Referente, ver la ruta).
 * Activar y desactivar, crear actividades nuevas del Hogar de día y sincronizar las
 * recurrentes del calendario. Las desactivadas dejan de ofrecerse para registrar pero
 * conservan el historial.
 */
@Component({
  selector: 'app-activity-settings',
  imports: [ReactiveFormsModule, UiBackButtonComponent, UiButtonComponent, UiFormFieldComponent, UiInputDirective],
  templateUrl: './activity-settings.component.html',
})
export class ActivitySettingsComponent implements OnInit {
  private activitiesService = inject(AttendanceActivitiesService);

  readonly activities = this.activitiesService.all;
  readonly areaLabels = AREA_LABELS;

  nameControl = new FormControl('', { nonNullable: true });
  formError = signal<string | null>(null);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  syncing = signal(false);
  private successTimeout: ReturnType<typeof setTimeout> | null = null;

  ngOnInit(): void {
    this.sync(true);
  }

  sourceLabel(activity: AttendanceActivityDef): string {
    return SOURCE_LABELS[activity.source];
  }

  /** @param silent true al abrir la pantalla: no muestra mensaje si no había nada nuevo */
  sync(silent = false): void {
    this.syncing.set(true);
    this.errorMessage.set(null);

    this.activitiesService.syncFromCalendar().subscribe({
      next: (added) => {
        this.syncing.set(false);
        if (added > 0) {
          this.showSuccess(added === 1 ? 'Se sumó 1 actividad del calendario.' : `Se sumaron ${added} actividades del calendario.`);
        } else if (!silent) {
          this.showSuccess('Las actividades ya estaban al día con el calendario.');
        }
      },
      error: () => {
        this.syncing.set(false);
        this.errorMessage.set('No se pudo sincronizar con el calendario.');
      },
    });
  }

  toggle(activity: AttendanceActivityDef): void {
    const result = this.activitiesService.setActive(activity.id, !activity.active);
    if (!result.ok) {
      this.errorMessage.set(result.message);
      return;
    }
    this.errorMessage.set(null);
    this.showSuccess(result.activity.active ? `"${activity.name}" activada.` : `"${activity.name}" desactivada.`);
  }

  create(): void {
    this.formError.set(null);
    const result = this.activitiesService.create(this.nameControl.value);
    if (!result.ok) {
      this.formError.set(result.message);
      return;
    }
    this.nameControl.setValue('');
    this.showSuccess(`Actividad "${result.activity.name}" creada.`);
  }

  private showSuccess(message: string): void {
    this.successMessage.set(message);
    if (this.successTimeout) {
      clearTimeout(this.successTimeout);
    }
    this.successTimeout = setTimeout(() => this.successMessage.set(null), 4000);
  }
}
