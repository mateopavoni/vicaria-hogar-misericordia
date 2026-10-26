import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AttendanceService } from '../../../attendance/services/attendance.service';
import { todayLocalIso } from '../../../../shared/utils/date.util';
import { summarizeAttendance, AttendanceSummary } from '../../utils/home-summary.util';

/** Resumen de la asistencia registrada hoy. El que lo monta decide el permiso (asistencia.view). */
@Component({
  selector: 'app-today-attendance-card',
  imports: [RouterLink],
  template: `
    <section class="rounded-2xl border border-[#B8C8FA] bg-white p-5 shadow-[0_8px_30px_rgba(79,115,232,0.08)]">
      <div class="flex items-start justify-between gap-3">
        <div>
          <p class="text-[16px] font-bold text-slate-900">Asistencia de hoy</p>
          <p class="mt-1 text-[13px] text-slate-500">
            @if (!loaded()) {
              Cargando...
            } @else if (summary().total === 0) {
              Todavía no se registró asistencia hoy.
            } @else {
              {{ summary().total }} {{ summary().total === 1 ? 'persona' : 'personas' }} en
              {{ summary().activities }} {{ summary().activities === 1 ? 'actividad' : 'actividades' }}.
            }
          </p>
        </div>
        <a routerLink="/dashboard/asistencia" class="rounded-md bg-[#4167D9] px-4 py-2 text-[13px] font-semibold text-white">
          Registrar
        </a>
      </div>
    </section>
  `,
})
export class TodayAttendanceCardComponent implements OnInit {
  private attendance = inject(AttendanceService);

  summary = signal<AttendanceSummary>({ total: 0, activities: 0 });
  loaded = signal(false);

  ngOnInit(): void {
    const today = todayLocalIso();
    this.attendance.getAll().subscribe({
      next: (all) => {
        this.summary.set(summarizeAttendance(all, today));
        this.loaded.set(true);
      },
      error: () => this.loaded.set(true),
    });
  }
}
