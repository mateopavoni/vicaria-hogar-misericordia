import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { UiFormFieldComponent, UiInputDirective } from '../../../../shared/ui';
import { todayLocalIso } from '../../../../shared/utils/date.util';
import { AttendanceActivity, AttendanceHistoryEntry } from '../../interfaces/attendance.interface';
import { AttendanceActivitiesService } from '../../services/attendance-activities.service';
import { ABSENCE_ALERT_DAYS, computeAttendanceStats, isAbsenceAlert } from '../../utils/attendance-stats.util';

interface ActivityGroup {
  id: AttendanceActivity;
  label: string;
  dates: string[];
}

/**
 * SCRUM-222 (front de SCRUM-55): sección de asistencia en el perfil de la persona (DUMB).
 * Lista las fechas agrupadas por actividad, con filtro por actividad y rango de fechas,
 * tarjetas de indicadores y aviso cuando lleva más de 7 días sin asistir.
 * Los indicadores se calculan sobre TODO el historial; los filtros solo afectan a la lista.
 */
@Component({
  selector: 'app-attendance-history',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, UiFormFieldComponent, UiInputDirective],
  templateUrl: './attendance-history.component.html',
})
export class AttendanceHistoryComponent {
  private activitiesService = inject(AttendanceActivitiesService);

  entries = input.required<AttendanceHistoryEntry[]>();

  // todas, también las desactivadas: la persona pudo asistir cuando estaban activas
  readonly activities = this.activitiesService.all;
  readonly alertDays = ABSENCE_ALERT_DAYS;
  readonly today = todayLocalIso();

  activityFilter = signal<AttendanceActivity | ''>('');
  dateFrom = signal('');
  dateTo = signal('');

  stats = computed(() => computeAttendanceStats(this.entries(), this.today));
  hasAbsenceAlert = computed(() => isAbsenceAlert(this.stats()));

  // rango invertido: se avisa en vez de mostrar una lista vacía sin explicación
  invalidRange = computed(() => !!this.dateFrom() && !!this.dateTo() && this.dateFrom() > this.dateTo());

  groups = computed<ActivityGroup[]>(() => {
    const from = this.dateFrom();
    const to = this.dateTo();
    const activity = this.activityFilter();

    return this.activities()
      .filter((a) => !activity || a.id === activity)
      .map((a) => ({
        id: a.id,
        label: a.name,
        dates: this.entries()
          .filter((e) => e.activity === a.id && (!from || e.date >= from) && (!to || e.date <= to))
          .map((e) => e.date)
          .sort()
          .reverse(),
      }))
      .filter((g) => g.dates.length > 0);
  });

  shownDays = computed(() => new Set(this.groups().flatMap((g) => g.dates)).size);

  hasFilters = computed(() => !!this.activityFilter() || !!this.dateFrom() || !!this.dateTo());

  clearFilters(): void {
    this.activityFilter.set('');
    this.dateFrom.set('');
    this.dateTo.set('');
  }

  // sin DatePipe: la app no registra el locale es-AR y 'EEEE' saldría en inglés
  longDate(dateIso: string): string {
    const label = this.toDate(dateIso).toLocaleDateString('es-AR', {
      weekday: 'long',
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
    });
    return label.charAt(0).toUpperCase() + label.slice(1);
  }

  toDate(dateIso: string): Date {
    const [y, m, d] = dateIso.split('-').map(Number);
    return new Date(y, m - 1, d);
  }
}
