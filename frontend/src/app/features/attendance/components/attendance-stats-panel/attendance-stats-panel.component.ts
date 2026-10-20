import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { extractApiError } from '../../../../core/http/api-error';
import { UiButtonComponent, UiFormFieldComponent, UiInputDirective } from '../../../../shared/ui';
import { todayLocalIso } from '../../../../shared/utils/date.util';
import {
  AttendanceActivity,
  AttendanceOverview,
  InactivePerson,
  SYSTEM_ACTIVITIES,
} from '../../interfaces/attendance.interface';
import { AttendanceActivitiesService } from '../../services/attendance-activities.service';
import { AttendanceService, AttendanceStatistics } from '../../services/attendance.service';
import { INACTIVE_AFTER_DAYS } from '../../utils/attendance-overview.util';

const EMPTY_OVERVIEW: AttendanceStatistics = {
  totalPresences: 0,
  daysWithData: 0,
  dailyAverage: 0,
  weeklyAverage: 0,
  monthlyAverage: 0,
  byActivity: [],
  perDay: [],
  previousPeriodTotal: 0,
  comparisonPercentage: null,
};

/**
 * SCRUM-226 (front de SCRUM-56): panel de estadísticas de asistencia del Centro Barrial.
 * Los números los calcula el backend (/api/attendances/statistics, /inactive y el PDF mensual).
 * Solo para Referente (permiso asistencia.stats): lo protege la ruta y el que lo monta.
 */
@Component({
  selector: 'app-attendance-stats-panel',
  imports: [DatePipe, UiButtonComponent, UiFormFieldComponent, UiInputDirective],
  templateUrl: './attendance-stats-panel.component.html',
})
export class AttendanceStatsPanelComponent implements OnInit {
  private attendanceService = inject(AttendanceService);
  private activitiesService = inject(AttendanceActivitiesService);

  // el backend solo lleva estadísticas de las dos actividades fijas
  readonly activities = signal(SYSTEM_ACTIVITIES);
  readonly inactiveAfterDays = INACTIVE_AFTER_DAYS;
  readonly today = todayLocalIso();

  private stats = signal<AttendanceStatistics>(EMPTY_OVERVIEW);
  inactivePersons = signal<InactivePerson[]>([]);
  loading = signal(true);
  exporting = signal(false);
  errorMessage = signal<string | null>(null);

  // por defecto: el mes en curso hasta hoy
  dateFrom = signal(`${todayLocalIso().slice(0, 7)}-01`);
  dateTo = signal(todayLocalIso());
  activityFilter = signal<AttendanceActivity | ''>('');

  // mes elegido para el resumen que se exporta a PDF (YYYY-MM)
  exportMonth = signal(todayLocalIso().slice(0, 7));

  invalidRange = computed(() => !!this.dateFrom() && !!this.dateTo() && this.dateFrom() > this.dateTo());

  overview = computed<AttendanceOverview>(() => this.stats());
  previousPeriodTotal = computed(() => this.stats().previousPeriodTotal);
  comparisonPercentage = computed(() => this.stats().comparisonPercentage);

  // alturas relativas de las barras (0-100) respecto del máximo del período
  maxActivityTotal = computed(() => Math.max(1, ...this.overview().byActivity.map((a) => a.totalPresences)));

  ngOnInit(): void {
    this.reload();
    this.attendanceService.getInactive(INACTIVE_AFTER_DAYS).subscribe({
      next: (people) => this.inactivePersons.set(people),
      error: (err) => this.errorMessage.set(extractApiError(err, 'No se pudo cargar la lista de personas inactivas.')),
    });
  }

  /** Vuelve a pedir las estadísticas con los filtros actuales. */
  reload(): void {
    if (this.invalidRange() || !this.dateFrom() || !this.dateTo()) {
      return;
    }
    this.loading.set(true);
    this.errorMessage.set(null);
    this.attendanceService
      .getStatistics(this.dateFrom(), this.dateTo(), this.activityFilter() || null)
      .subscribe({
        next: (stats) => {
          this.stats.set(stats);
          this.loading.set(false);
        },
        error: (err) => {
          this.loading.set(false);
          this.errorMessage.set(extractApiError(err, 'No se pudieron cargar las estadísticas de asistencia.'));
        },
      });
  }

  setFrom(value: string): void {
    this.dateFrom.set(value);
    this.reload();
  }

  setTo(value: string): void {
    this.dateTo.set(value);
    this.reload();
  }

  setActivity(value: string): void {
    this.activityFilter.set(value);
    this.reload();
  }

  activityName(id: AttendanceActivity): string {
    return this.activitiesService.name(id);
  }

  toDate(dateIso: string): Date {
    const [y, m, d] = dateIso.split('-').map(Number);
    return new Date(y, m - 1, d);
  }

  pct(value: number, max: number): number {
    return Math.round((value / max) * 100);
  }

  /** Exportar el resumen mensual: el backend genera el PDF y se descarga. */
  exportMonthlyPdf(): void {
    const month = this.exportMonth();
    if (!month || this.exporting()) {
      return;
    }
    const [year, monthNumber] = month.split('-').map(Number);
    this.exporting.set(true);
    this.attendanceService.getMonthlySummaryPdf(year, monthNumber).subscribe({
      next: (blob) => {
        this.exporting.set(false);
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `resumen-mensual-${month}.pdf`;
        link.click();
        URL.revokeObjectURL(url);
      },
      error: () => {
        this.exporting.set(false);
        this.errorMessage.set('No se pudo generar el resumen mensual en PDF.');
      },
    });
  }
}
