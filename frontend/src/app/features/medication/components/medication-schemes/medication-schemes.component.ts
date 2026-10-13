import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { AuthService } from '../../../../core/auth/auth.service';
import { PermissionService } from '../../../../core/auth/permission.service';
import { extractApiError } from '../../../../core/http/api-error';
import { UiButtonComponent, UiTab, UiTabsComponent } from '../../../../shared/ui';
import {
  CreateMedicationSchemeRequest,
  MedicationCatalogItem,
  MedicationScheme,
} from '../../interfaces/medication.interface';
import { MedicationHistoryComponent } from '../medication-history/medication-history.component';
import { REVIEW_ALERT_DAYS, daysSinceReview, evaluationExpiryDate, needsReview } from '../../utils/scheme-review.util';
import { MedicationService } from '../../services/medication.service';
import { MedicationSchemeFormModalComponent } from '../scheme-form-modal/scheme-form-modal.component';

/**
 * SCRUM-234: sección "Medicación" del perfil de la persona. Lista el esquema (vigente e
 * histórico) y permite cargar medicamentos. Se monta solo con permiso medicamentos.view
 * (Referente, Directora y Coordinador; nunca Escucha); cargar requiere medicamentos.create
 * (Referente y Directora). Sin evaluación psiquiátrica vigente la carga queda bloqueada.
 */
@Component({
  selector: 'app-medication-schemes',
  imports: [DatePipe, UiButtonComponent, UiTabsComponent, MedicationSchemeFormModalComponent, MedicationHistoryComponent],
  templateUrl: './medication-schemes.component.html',
})
export class MedicationSchemesComponent implements OnInit {
  private medicationService = inject(MedicationService);
  private permissionService = inject(PermissionService);
  private authService = inject(AuthService);

  personId = input.required<string>();
  isResident = input(false);
  personName = input('');
  /** Fecha de la última evaluación psiquiátrica (YYYY-MM-DD), si el perfil la tiene. */
  evaluatedAt = input<string | null>(null);

  schemes = signal<MedicationScheme[]>([]);
  catalog = signal<MedicationCatalogItem[]>([]);
  catalogLoading = signal(false);
  evaluationValid = signal<boolean | null>(null);
  evaluationExpired = signal(false);

  // SCRUM-250: vigentes / esquemas anteriores / historial de tomas
  activeTab = signal<'vigentes' | 'anteriores' | 'tomas'>('vigentes');
  tabs = computed<UiTab<'vigentes' | 'anteriores' | 'tomas'>[]>(() => [
    { id: 'vigentes', label: `Vigentes (${this.activeSchemes().length})` },
    { id: 'anteriores', label: `Esquemas anteriores (${this.historicSchemes().length})` },
    ...(this.canViewHistory() ? [{ id: 'tomas' as const, label: 'Historial de tomas' }] : []),
  ]);
  reviewingId = signal<string | null>(null);
  readonly reviewAlertDays = REVIEW_ALERT_DAYS;

  loading = signal(true);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  private successTimeout: ReturnType<typeof setTimeout> | null = null;

  showForm = signal(false);
  saving = signal(false);
  formError = signal<string | null>(null);

  // SCRUM-242: historial de tomas, solo Referente y Directora
  canViewHistory = computed(() => this.permissionService.hasPermission('medicamentos.history'));
  canCreate = computed(() => this.permissionService.hasPermission('medicamentos.create'));
  // la carga queda bloqueada hasta confirmar que hay una evaluación vigente
  canReview = computed(() => this.permissionService.hasPermission('medicamentos.edit'));
  blocked = computed(() => this.evaluationValid() === false);

  // vigentes con más de 30 días sin revisión
  staleSchemes = computed(() => this.activeSchemes().filter((s) => needsReview(s)));
  activeSchemes = computed(() => this.schemes().filter((s) => s.active));
  historicSchemes = computed(() => this.schemes().filter((s) => !s.active));

  ngOnInit(): void {
    this.reload();

    this.medicationService.getEvaluationStatus(this.personId(), this.isResident(), this.evaluatedAt()).subscribe({
      next: (status) => {
        this.evaluationValid.set(status.valid);
        this.evaluationExpired.set(status.expired);
      },
      // si no se puede confirmar, se bloquea por precaución
      error: () => this.evaluationValid.set(false),
    });
  }

  private reload(): void {
    this.loading.set(true);
    this.medicationService.getSchemes(this.personId()).subscribe({
      next: (schemes) => {
        this.schemes.set(schemes);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.errorMessage.set('No se pudo cargar el esquema de medicación.');
      },
    });
  }

  openForm(): void {
    if (!this.canCreate() || this.blocked()) {
      return;
    }
    this.formError.set(null);
    this.showForm.set(true);

    if (this.catalog().length === 0) {
      this.catalogLoading.set(true);
      this.medicationService.getCatalog().subscribe({
        next: (catalog) => {
          this.catalog.set(catalog);
          this.catalogLoading.set(false);
        },
        error: () => {
          this.catalogLoading.set(false);
          this.formError.set('No se pudo cargar el catálogo de medicamentos.');
        },
      });
    }
  }

  closeForm(): void {
    this.showForm.set(false);
  }

  save(request: CreateMedicationSchemeRequest): void {
    const user = this.authService.user();
    if (!user) {
      return;
    }

    this.saving.set(true);
    this.formError.set(null);

    this.medicationService
      .create(request, this.isResident(), { id: user.id, name: `${user.name} ${user.lastname}`.trim() }, this.evaluatedAt())
      .subscribe({
        next: (scheme) => {
          this.saving.set(false);
          this.showForm.set(false);
          this.showSuccess(
            scheme.pendingStock
              ? 'Esquema cargado. Quedó pendiente de stock: no entra a la agenda hasta que haya en farmacia.'
              : 'Esquema de medicación cargado correctamente.',
          );
          this.reload();
        },
        error: (err) => {
          this.saving.set(false);
          this.formError.set(extractApiError(err, 'No se pudo cargar el esquema.'));
        },
      });
  }

  isStale(scheme: MedicationScheme): boolean {
    return needsReview(scheme);
  }

  daysWithoutReview(scheme: MedicationScheme): number {
    return daysSinceReview(scheme);
  }

  expiredOn(): string {
    const date = this.evaluatedAt();
    return date ? evaluationExpiryDate(date).split('-').reverse().join('/') : '';
  }

  /** "Marcar como revisado": guarda revisado_en y revisado_por (usuario logueado). */
  markReviewed(scheme: MedicationScheme): void {
    const user = this.authService.user();
    if (!user || !this.canReview() || this.reviewingId()) {
      return;
    }
    this.reviewingId.set(scheme.id);
    this.errorMessage.set(null);
    this.medicationService.markReviewed(scheme.id, { id: user.id, name: `${user.name} ${user.lastname}`.trim() }).subscribe({
      next: () => {
        this.reviewingId.set(null);
        this.showSuccess('Esquema marcado como revisado.');
        this.reload();
      },
      error: (err) => {
        this.reviewingId.set(null);
        this.errorMessage.set(extractApiError(err, 'No se pudo marcar como revisado.'));
      },
    });
  }

  toggleActive(scheme: MedicationScheme): void {
    if (!this.canCreate()) {
      return;
    }
    this.errorMessage.set(null);
    this.medicationService.setActive(scheme.id, !scheme.active).subscribe({
      next: () => this.reload(),
      error: (err) => this.errorMessage.set(extractApiError(err, 'No se pudo actualizar el esquema.')),
    });
  }

  private showSuccess(message: string): void {
    this.successMessage.set(message);
    if (this.successTimeout) {
      clearTimeout(this.successTimeout);
    }
    this.successTimeout = setTimeout(() => this.successMessage.set(null), 5000);
  }
}
