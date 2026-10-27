import { Component, computed, inject, input, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { AuthService } from '../../../../core/auth/auth.service';
import { PermissionService } from '../../../../core/auth/permission.service';
import { extractApiError } from '../../../../core/http/api-error';
import { UiButtonComponent } from '../../../../shared/ui';
import { todayLocalIso } from '../../../../shared/utils/date.util';
import { PsychiatricEvaluation, RegisterEvaluationRequest } from '../../interfaces/psychiatric-evaluation.interface';
import { PsychiatricEvaluationsService } from '../../services/psychiatric-evaluations.service';
import { daysUntilExpiry, evaluationExpiryDate, evaluationValidity } from '../../utils/evaluation-validity.util';
import { EvaluationFormModalComponent } from '../evaluation-form-modal/evaluation-form-modal.component';

/**
 * SCRUM-260: pestaña "Evaluaciones" del perfil (SMART). Muestra la evaluación vigente con su
 * vencimiento (badge de alerta a menos de 30 días, indicador de vencida), el formulario de carga
 * y el historial de evaluaciones anteriores (vigente = false).
 * Ver: evaluaciones.view; cargar: evaluaciones.create (Referente y Directora).
 */
@Component({
  selector: 'app-psychiatric-evaluations',
  imports: [DatePipe, UiButtonComponent, EvaluationFormModalComponent],
  templateUrl: './psychiatric-evaluations.component.html',
})
export class PsychiatricEvaluationsComponent {
  private service = inject(PsychiatricEvaluationsService);
  private authService = inject(AuthService);
  private permissionService = inject(PermissionService);

  personId = input.required<string>();

  readonly today = todayLocalIso();

  current = computed(() => this.service.currentFor(this.personId()));
  previous = computed(() => this.service.forPerson(this.personId()).filter((e) => !e.vigente));

  validity = computed(() => {
    const current = this.current();
    return current ? evaluationValidity(current.date, this.today) : null;
  });
  daysLeft = computed(() => {
    const current = this.current();
    return current ? daysUntilExpiry(current.date, this.today) : 0;
  });
  expiryDate = computed(() => {
    const current = this.current();
    return current ? evaluationExpiryDate(current.date) : '';
  });

  canCreate = computed(() => this.permissionService.hasPermission('evaluaciones.create'));

  showForm = signal(false);
  saving = signal(false);
  formError = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  private successTimeout: ReturnType<typeof setTimeout> | null = null;

  validityOf(evaluation: PsychiatricEvaluation) {
    return evaluationValidity(evaluation.date, this.today);
  }

  openForm(): void {
    this.formError.set(null);
    this.showForm.set(true);
  }

  save(request: RegisterEvaluationRequest): void {
    const user = this.authService.user();
    if (!user || !this.canCreate()) return;

    this.saving.set(true);
    this.formError.set(null);
    this.service.register(request, { id: user.id, name: `${user.name} ${user.lastname}`.trim() }).subscribe({
      next: () => {
        this.saving.set(false);
        this.showForm.set(false);
        this.successMessage.set('Evaluación registrada. La anterior pasó al historial.');
        if (this.successTimeout) clearTimeout(this.successTimeout);
        this.successTimeout = setTimeout(() => this.successMessage.set(null), 5000);
      },
      error: (err) => {
        this.saving.set(false);
        this.formError.set(extractApiError(err, 'No se pudo registrar la evaluación.'));
      },
    });
  }
}
