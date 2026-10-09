import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { UiButtonComponent, UiTab, UiTabsComponent } from '../../../../shared/ui';
import { PermissionService } from '../../../../core/auth/permission.service';
import { CancelVisitModalComponent } from '../../components/cancel-visit-modal/cancel-visit-modal.component';
import { VisitDetailModalComponent } from '../../components/visit-detail-modal/visit-detail-modal.component';
import { VisitFormModalComponent } from '../../components/visit-form-modal/visit-form-modal.component';
import { CreateVisitDto, Resident, Visit, VisitStatus } from '../../interfaces/visit.interface';
import { CasaConvivenciaVisitsService } from '../../services/casa-convivencia-visits.service';
import { findConflictingVisits } from '../../utils/visit-conflict.util';
import {
  addDays,
  addMonths,
  endOfWeek,
  isSameDay,
  isoDate,
  monthGridWeeks,
  startOfWeek,
  weekDays,
} from '../../../calendar/utils/calendar-date.util';
import { isBeforeToday } from '../../../../shared/utils/date.util';

// SCRUM-213 (AC): "vista semanal por defecto con posibilidad de ver día o mes" — una
// vista más que el calendario general (que solo tiene semana/mes), por eso no se
// reutiliza literalmente CalendarComponent (está armado para CalendarEvent/scope
// general-personal, nada de eso aplica acá). Lo que SÍ se reutiliza, tal como pide el
// AC ("reutilizando el componente base con otra fuente de datos"), son los helpers de
// grilla de calendar-date.util.ts — mismo cálculo de semana/mes que el calendario
// general, aplicado a otra fuente de datos (visitas en vez de eventos).
type ViewMode = 'day' | 'week' | 'month';

/**
 * SCRUM-74 (SCRUM-213/SCRUM-214): calendario semanal de visitas de la Casa de
 * Convivencia, independiente del calendario general (SCRUM-28). Exclusivo de
 * Referente, DirectoraDeCasona y CoordinadorDeCasaConvivencia (route guard
 * visitas.view; Escucha no tiene ninguno de los permisos visitas.*, ni de lectura).
 * Alta/edición de visitas (residente, visitante, fecha, hora, duración), cambio de
 * estado (pendiente/realizada/cancelada, con motivo opcional al cancelar), y alerta
 * no bloqueante si hay dos visitas al mismo residente en el mismo horario.
 */
@Component({
  selector: 'app-casa-convivencia-visits-calendar',
  imports: [
    DatePipe,
    UiTabsComponent,
    UiButtonComponent,
    VisitFormModalComponent,
    VisitDetailModalComponent,
    CancelVisitModalComponent,
  ],
  templateUrl: './casa-convivencia-visits-calendar.component.html',
})
export class CasaConvivenciaVisitsCalendarComponent implements OnInit, OnDestroy {
  private casaConvivenciaVisitsService = inject(CasaConvivenciaVisitsService);
  private permissionService = inject(PermissionService);

  viewMode = signal<ViewMode>('week');
  referenceDate = signal<Date>(new Date());

  visits = signal<Visit[]>([]);
  loading = signal(false);
  errorMessage = signal<string | null>(null);

  residents = signal<Resident[]>([]);
  residentsLoading = signal(false);

  readonly today = new Date();

  canCreate = computed(() => this.permissionService.hasPermission('visitas.create'));
  canEdit = computed(() => this.permissionService.hasPermission('visitas.edit'));

  viewTabs: UiTab<ViewMode>[] = [
    { id: 'day', label: 'Día' },
    { id: 'week', label: 'Semana' },
    { id: 'month', label: 'Mes' },
  ];

  rangeStart = computed(() => {
    const ref = this.referenceDate();
    switch (this.viewMode()) {
      case 'day':
        return new Date(ref.getFullYear(), ref.getMonth(), ref.getDate());
      case 'week':
        return startOfWeek(ref);
      default:
        return startOfWeek(new Date(ref.getFullYear(), ref.getMonth(), 1));
    }
  });

  rangeEnd = computed(() => {
    const ref = this.referenceDate();
    switch (this.viewMode()) {
      case 'day':
        return new Date(ref.getFullYear(), ref.getMonth(), ref.getDate());
      case 'week':
        return endOfWeek(ref);
      default: {
        const lastDayOfMonth = new Date(ref.getFullYear(), ref.getMonth() + 1, 0);
        return endOfWeek(lastDayOfMonth);
      }
    }
  });

  rangeLabel = computed(() => {
    const ref = this.referenceDate();
    const mode = this.viewMode();
    if (mode === 'month') {
      const label = ref.toLocaleDateString('es-AR', { month: 'long', year: 'numeric' });
      return label.charAt(0).toUpperCase() + label.slice(1);
    }
    if (mode === 'day') {
      const label = ref.toLocaleDateString('es-AR', { weekday: 'long', day: '2-digit', month: '2-digit', year: 'numeric' });
      return label.charAt(0).toUpperCase() + label.slice(1);
    }
    const start = this.rangeStart();
    const end = this.rangeEnd();
    const fmt = (d: Date) => d.toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit' });
    return `${fmt(start)} – ${fmt(end)}, ${end.getFullYear()}`;
  });

  weekDays = computed(() => weekDays(this.referenceDate()));
  monthWeeks = computed(() => monthGridWeeks(this.referenceDate()));

  // alta/edición
  showFormModal = signal(false);
  editingVisit = signal<Visit | null>(null);
  formInitialDate = signal(isoDate(new Date()));
  saving = signal(false);
  formError = signal<string | null>(null);

  // detalle
  viewingVisit = signal<Visit | null>(null);

  // cancelación (AC: motivo opcional)
  cancellingVisit = signal<Visit | null>(null);
  cancelling = signal(false);

  successMessage = signal<string | null>(null);
  private successTimeout: ReturnType<typeof setTimeout> | null = null;

  ngOnInit(): void {
    this.loadVisits();
    this.loadResidents();
  }

  ngOnDestroy(): void {
    if (this.successTimeout) {
      clearTimeout(this.successTimeout);
    }
  }

  private showSuccess(message: string): void {
    this.successMessage.set(message);
    if (this.successTimeout) {
      clearTimeout(this.successTimeout);
    }
    this.successTimeout = setTimeout(() => this.successMessage.set(null), 4000);
  }

  isToday(date: Date): boolean {
    return isSameDay(date, this.today);
  }

  isCurrentMonth(date: Date): boolean {
    return date.getMonth() === this.referenceDate().getMonth();
  }

  // Regla pedida: ninguna visita puede quedar con fecha anterior a hoy — acá se usa
  // para no ofrecer "nueva visita" al clickear un día ya pasado en la grilla (el
  // bloqueo real está en el formulario, ver visit-form-modal).
  isPastDay(date: Date): boolean {
    return isBeforeToday(isoDate(date));
  }

  visitsForDay(date: Date): Visit[] {
    return this.visits()
      .filter((v) => isSameDay(new Date(v.start), date))
      .sort((a, b) => a.start.localeCompare(b.start));
  }

  visitTime(visit: Visit): string {
    return new Date(visit.start).toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' });
  }

  // AC de SCRUM-74: alerta (no bloqueo) si hay dos visitas al mismo residente en el
  // mismo horario — acá se usa para marcar el ícono sobre la visita en el calendario,
  // además de la alerta que ya muestra el formulario al crear/editar.
  hasConflict(visit: Visit): boolean {
    return findConflictingVisits(visit, this.visits()).length > 0;
  }

  weekdayLabel(date: Date): string {
    const label = date.toLocaleDateString('es-AR', { weekday: 'short' });
    return label.replace('.', '').charAt(0).toUpperCase() + label.replace('.', '').slice(1);
  }

  changeView(mode: ViewMode): void {
    this.viewMode.set(mode);
    this.loadVisits();
  }

  goToday(): void {
    this.referenceDate.set(new Date());
    this.loadVisits();
  }

  goPrev(): void {
    this.referenceDate.set(this.shift(-1));
    this.loadVisits();
  }

  goNext(): void {
    this.referenceDate.set(this.shift(1));
    this.loadVisits();
  }

  private shift(direction: 1 | -1): Date {
    switch (this.viewMode()) {
      case 'day':
        return addDays(this.referenceDate(), direction);
      case 'week':
        return addDays(this.referenceDate(), 7 * direction);
      default:
        return addMonths(this.referenceDate(), direction);
    }
  }

  loadVisits(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.casaConvivenciaVisitsService.getByRange(isoDate(this.rangeStart()), isoDate(this.rangeEnd())).subscribe({
      next: (visits) => {
        this.visits.set(visits);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.errorMessage.set('No se pudieron cargar las visitas.');
      },
    });
  }

  private loadResidents(): void {
    this.residentsLoading.set(true);
    this.casaConvivenciaVisitsService.getResidents().subscribe({
      next: (residents) => {
        this.residents.set(residents);
        this.residentsLoading.set(false);
      },
      error: () => {
        this.residentsLoading.set(false);
        this.errorMessage.set('No se pudo cargar la lista de residentes.');
      },
    });
  }

  openCreateModal(date: Date = new Date()): void {
    this.editingVisit.set(null);
    this.formInitialDate.set(isoDate(date));
    this.formError.set(null);
    this.showFormModal.set(true);
  }

  openEditModal(visit: Visit): void {
    this.editingVisit.set(visit);
    this.formError.set(null);
    this.showFormModal.set(true);
  }

  closeFormModal(): void {
    this.showFormModal.set(false);
  }

  saveVisit(dto: CreateVisitDto): void {
    this.saving.set(true);
    this.formError.set(null);

    const editing = this.editingVisit();
    const request$ = editing
      ? this.casaConvivenciaVisitsService.update(editing.id, dto)
      : this.casaConvivenciaVisitsService.create(dto);

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.showFormModal.set(false);
        this.showSuccess(editing ? 'Visita actualizada correctamente.' : 'Visita creada correctamente.');
        this.loadVisits();
      },
      error: (err) => {
        this.saving.set(false);
        this.formError.set(err?.error?.message || 'No se pudo guardar la visita.');
      },
    });
  }

  openDetail(visit: Visit): void {
    this.viewingVisit.set(visit);
  }

  closeDetail(): void {
    this.viewingVisit.set(null);
  }

  editFromDetail(visit: Visit): void {
    this.viewingVisit.set(null);
    this.openEditModal(visit);
  }

  changeStatus(status: VisitStatus): void {
    const visit = this.viewingVisit();
    if (!visit) {
      return;
    }
    this.errorMessage.set(null);
    this.casaConvivenciaVisitsService.updateStatus(visit.id, status).subscribe({
      next: () => {
        this.viewingVisit.set(null);
        this.showSuccess(status === 'done' ? 'Visita marcada como realizada.' : 'Visita marcada como pendiente.');
        this.loadVisits();
      },
      error: (err) => {
        this.errorMessage.set(err?.error?.message || 'No se pudo actualizar el estado de la visita.');
      },
    });
  }

  requestCancel(visit: Visit): void {
    this.viewingVisit.set(null);
    this.errorMessage.set(null);
    this.cancellingVisit.set(visit);
  }

  closeCancelModal(): void {
    this.cancellingVisit.set(null);
  }

  confirmCancel(reason: string | null): void {
    const visit = this.cancellingVisit();
    if (!visit) {
      return;
    }

    this.cancelling.set(true);
    this.casaConvivenciaVisitsService.updateStatus(visit.id, 'cancelled', reason).subscribe({
      next: () => {
        this.cancelling.set(false);
        this.cancellingVisit.set(null);
        this.showSuccess('Visita cancelada.');
        this.loadVisits();
      },
      error: (err) => {
        this.cancelling.set(false);
        this.errorMessage.set(err?.error?.message || 'No se pudo cancelar la visita.');
      },
    });
  }
}
