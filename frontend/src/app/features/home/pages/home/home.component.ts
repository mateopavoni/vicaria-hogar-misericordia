import { Component, computed, inject } from '@angular/core';
import { PermissionService } from '../../../../core/auth/permission.service';
import { MedicationAgendaShortcutComponent } from '../../../medication/components/medication-agenda-shortcut/medication-agenda-shortcut.component';
import { AttendanceStatsPanelComponent } from '../../../attendance/components/attendance-stats-panel/attendance-stats-panel.component';
import { QuickObservationComponent } from '../../components/quick-observation/quick-observation.component';
import { TodayAttendanceCardComponent } from '../../components/today-attendance-card/today-attendance-card.component';
import { TodayEventsCardComponent } from '../../components/today-events-card/today-events-card.component';
import { TodayVisitsCardComponent } from '../../components/today-visits-card/today-visits-card.component';
import { LowStockCardComponent } from '../../components/low-stock-card/low-stock-card.component';

/**
 * Pantalla de inicio (/dashboard). Hasta ahora no existía: el login redirigía acá y el
 * outlet quedaba vacío. Cada rol ve solo los widgets de lo que tiene permiso: observación rápida, asistencia, calendario,
 * visitas de la Casa de Convivencia y stock bajo; el panel de estadísticas (SCRUM-226) es del Referente.
 */
@Component({
  selector: 'app-home',
  imports: [
    AttendanceStatsPanelComponent,
    MedicationAgendaShortcutComponent,
    QuickObservationComponent,
    TodayAttendanceCardComponent,
    TodayEventsCardComponent,
    TodayVisitsCardComponent,
    LowStockCardComponent,
  ],
  template: `
    <div class="min-h-screen w-full bg-[#F5F7FB] px-8 pb-8 pt-4">
      <div class="mb-5">
        <div class="flex items-center gap-3">
          <svg class="h-7 w-7 text-[#4167D9]" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24" aria-hidden="true">
            <path d="m3 10 9-8 9 8"></path>
            <path d="M5 10v10h14V10"></path>
            <path d="M9 20v-6h6v6"></path>
          </svg>
          <h1 class="text-[26px] font-bold text-slate-900">Inicio</h1>
        </div>
      </div>

      @if (canAddObservation()) {
        <app-quick-observation />
      }

      @if (canSeeMedication()) {
        <app-medication-agenda-shortcut />
      }

      <div class="mb-5 grid gap-5 lg:grid-cols-2">
        @if (canSeeAttendance()) {
          <app-today-attendance-card />
        }
        @if (canSeeCalendar()) {
          <app-today-events-card />
        }
        @if (canSeeVisits()) {
          <app-today-visits-card />
        }
        @if (canSeeStock()) {
          <app-low-stock-card />
        }
      </div>

      @if (canSeeStats()) {
        <app-attendance-stats-panel />
      }
    </div>
  `,
})
export class HomeComponent {
  private permissionService = inject(PermissionService);

  canSeeMedication = computed(() => this.permissionService.hasPermission('medicamentos.view'));
  canAddObservation = computed(
    () => this.permissionService.hasPermission('observaciones.create') && this.permissionService.hasPermission('fichas.view'),
  );
  canSeeAttendance = computed(() => this.permissionService.hasPermission('asistencia.view'));
  canSeeCalendar = computed(() => this.permissionService.hasPermission('calendario.view'));
  canSeeVisits = computed(() => this.permissionService.hasPermission('visitas.view'));
  canSeeStock = computed(() => this.permissionService.hasPermission('medicamentos.stock'));
  canSeeStats = computed(() => this.permissionService.hasPermission('asistencia.stats'));
}
