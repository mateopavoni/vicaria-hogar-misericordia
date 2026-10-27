import { Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/auth/auth.service';
import { PermissionService } from '../../../../core/auth/permission.service';
import { extractApiError } from '../../../../core/http/api-error';
import { todayLocalIso } from '../../../../shared/utils/date.util';
import { UiButtonComponent } from '../../../../shared/ui';
import { AdministrationStatus, AgendaItem, MedicationAgenda } from '../../interfaces/medication.interface';
import { MedicationService } from '../../services/medication.service';

/**
 * SCRUM-238: agenda diaria de medicamentos (/dashboard/medicacion).
 * Tomas agrupadas por hora según quiénes asistieron ese día. Se ve con medicamentos.view;
 * marcar tomas requiere medicamentos.edit (Referente y Directora) y solo en el día actual.
 * Los días anteriores son de solo lectura, para verificar el historial de administración.
 */
@Component({
  selector: 'app-medication-agenda',
  imports: [DatePipe, RouterLink, UiButtonComponent],
  templateUrl: './medication-agenda.component.html',
})
export class MedicationAgendaComponent {
  private medicationService = inject(MedicationService);
  readonly permissionService = inject(PermissionService);
  private authService = inject(AuthService);

  readonly today = todayLocalIso();
  date = signal(this.today);

  agenda = signal<MedicationAgenda | null>(null);
  loading = signal(true);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  private successTimeout: ReturnType<typeof setTimeout> | null = null;

  /** Observación en edición, por esquema. */
  drafts = signal<Record<string, string>>({});
  /** Esquemas cuya toma ya registrada se está corrigiendo. */
  correcting = signal<ReadonlySet<string>>(new Set());
  savingId = signal<string | null>(null);

  isToday = computed(() => this.date() === this.today);
  canRecord = computed(() => this.isToday() && this.permissionService.hasPermission('medicamentos.edit'));

  longDate = computed(() => {
    const [y, m, d] = this.date().split('-').map(Number);
    return new Date(y, m - 1, d).toLocaleDateString('es-AR', {
      weekday: 'long',
      day: 'numeric',
      month: 'long',
      year: 'numeric',
    });
  });

  total = computed(() => this.agenda()?.slots.reduce((n, s) => n + s.items.length, 0) ?? 0);
  done = computed(
    () => this.agenda()?.slots.reduce((n, s) => n + s.items.filter((i) => i.administration).length, 0) ?? 0,
  );

  constructor() {
    effect(() => {
      const date = this.date();
      untracked(() => this.load(date));
    });
  }

  private load(date: string): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.drafts.set({});
    this.correcting.set(new Set());
    this.medicationService.getAgenda(date).subscribe({
      next: (agenda) => {
        if (date !== this.date()) return; // llegó tarde: ya se cambió de día
        this.agenda.set(agenda);
        this.loading.set(false);
      },
      error: () => {
        if (date !== this.date()) return;
        this.loading.set(false);
        this.errorMessage.set('No se pudo cargar la agenda de medicamentos.');
      },
    });
  }

  onDateChange(value: string): void {
    // sin fecha o futura: no hay asistencia registrada, se vuelve a hoy
    this.date.set(value && value <= this.today ? value : this.today);
  }

  goToToday(): void {
    this.date.set(this.today);
  }

  /** Muestra los botones si la toma está pendiente o se la está corrigiendo. */
  isEditable(item: AgendaItem): boolean {
    return this.canRecord() && (!item.administration || this.correcting().has(item.intakeId));
  }

  draftOf(item: AgendaItem): string {
    return this.drafts()[item.intakeId] ?? item.administration?.observation ?? '';
  }

  setDraft(item: AgendaItem, value: string): void {
    this.drafts.update((d) => ({ ...d, [item.intakeId]: value }));
  }

  startCorrection(item: AgendaItem): void {
    this.correcting.update((set) => new Set(set).add(item.intakeId));
  }

  mark(item: AgendaItem, status: AdministrationStatus): void {
    const user = this.authService.user();
    if (!user || !this.canRecord() || this.savingId()) {
      return;
    }

    this.savingId.set(item.intakeId);
    this.errorMessage.set(null);

    this.medicationService
      .recordAdministration({ intakeId: item.intakeId, date: this.date(), status, observation: this.draftOf(item) || null })
      .subscribe({
        next: () => {
          this.savingId.set(null);
          this.showSuccess(status === 'administered' ? 'Toma registrada como administrada.' : 'Toma registrada como no administrada.');
          this.load(this.date());
        },
        error: (err) => {
          this.savingId.set(null);
          this.errorMessage.set(extractApiError(err, 'No se pudo registrar la toma.'));
        },
      });
  }

  private showSuccess(message: string): void {
    this.successMessage.set(message);
    if (this.successTimeout) clearTimeout(this.successTimeout);
    this.successTimeout = setTimeout(() => this.successMessage.set(null), 4000);
  }
}
