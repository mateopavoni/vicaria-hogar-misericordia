import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { UiButtonComponent, UiFormFieldComponent, UiInputDirective } from '../../../../shared/ui';
import { todayLocalIso } from '../../../../shared/utils/date.util';
import { AttendanceActivity, DailyAttendance } from '../../interfaces/attendance.interface';
import { AttendanceActivitiesService } from '../../services/attendance-activities.service';
import { AttendanceService } from '../../services/attendance.service';
import { INACTIVE_AFTER_DAYS, computeOverview, findInactivePersons } from '../../utils/attendance-overview.util';

/**
 * SCRUM-226 (front de SCRUM-56): panel de estadísticas de asistencia del Centro Barrial.
 * Números destacados y barras simples, filtros de fechas y actividad, listado de personas
 * que dejaron de asistir hace más de 14 días y exportación del resumen mensual a PDF.
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

  // todas, también las desactivadas: tienen registros viejos que se siguen contando
  readonly activities = this.activitiesService.all;
  readonly inactiveAfterDays = INACTIVE_AFTER_DAYS;
  readonly today = todayLocalIso();

  private records = signal<DailyAttendance[]>([]);
  loading = signal(true);
  errorMessage = signal<string | null>(null);

  // por defecto: el mes en curso hasta hoy
  dateFrom = signal(`${todayLocalIso().slice(0, 7)}-01`);
  dateTo = signal(todayLocalIso());
  activityFilter = signal<AttendanceActivity | ''>('');

  // mes elegido para el resumen que se exporta a PDF (YYYY-MM)
  exportMonth = signal(todayLocalIso().slice(0, 7));

  invalidRange = computed(() => !!this.dateFrom() && !!this.dateTo() && this.dateFrom() > this.dateTo());

  overview = computed(() =>
    computeOverview(this.records(), {
      from: this.dateFrom() || '0000-01-01',
      to: this.dateTo() || '9999-12-31',
      activity: this.activityFilter() || null,
    }, this.activities()),
  );

  inactivePersons = computed(() => findInactivePersons(this.records(), this.today));

  // alturas relativas de las barras (0-100) respecto del máximo del período
  maxActivityTotal = computed(() => Math.max(1, ...this.overview().byActivity.map((a) => a.totalPresences)));
  maxDayTotal = computed(() => Math.max(1, ...this.overview().perDay.map((d) => d.total)));

  ngOnInit(): void {
    this.attendanceService.getAll().subscribe({
      next: (records) => {
        this.records.set(records);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.errorMessage.set('No se pudieron cargar las estadísticas de asistencia.');
      },
    });
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

  /**
   * Exportar el resumen mensual a PDF: se fija el rango en el mes elegido y se abre el
   * diálogo de impresión del navegador ("Guardar como PDF"), igual que "Exportar ficha".
   * Los estilos de impresión (styles.css) ocultan menú, barra y los elementos .no-print.
   */
  exportMonthlyPdf(): void {
    const month = this.exportMonth();
    if (!month) {
      return;
    }
    const [y, m] = month.split('-').map(Number);
    const lastDay = new Date(y, m, 0).getDate();
    const end = `${month}-${String(lastDay).padStart(2, '0')}`;

    this.dateFrom.set(`${month}-01`);
    this.dateTo.set(end > this.today ? this.today : end);

    // esperar a que Angular repinte con el nuevo rango antes de imprimir
    setTimeout(() => window.print(), 100);
  }
}
