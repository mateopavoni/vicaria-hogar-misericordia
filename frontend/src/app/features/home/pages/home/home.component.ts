import { Component, computed, inject } from '@angular/core';
import { PermissionService } from '../../../../core/auth/permission.service';
import { MedicationAgendaShortcutComponent } from '../../../medication/components/medication-agenda-shortcut/medication-agenda-shortcut.component';
import { AttendanceStatsPanelComponent } from '../../../attendance/components/attendance-stats-panel/attendance-stats-panel.component';

/**
 * Pantalla de inicio (/dashboard). Hasta ahora no existía: el login redirigía acá y el
 * outlet quedaba vacío. Muestra el panel de estadísticas de asistencia (SCRUM-226) solo
 * a quien tiene el permiso asistencia.stats (Referente).
 */
@Component({
  selector: 'app-home',
  imports: [AttendanceStatsPanelComponent, MedicationAgendaShortcutComponent],
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

      @if (canSeeMedication()) {
        <app-medication-agenda-shortcut />
      }

      @if (canSeeStats()) {
        <app-attendance-stats-panel />
      }
    </div>
  `,
})
export class HomeComponent {
  private permissionService = inject(PermissionService);

  canSeeMedication = computed(() => this.permissionService.hasPermission('medicamentos.view'));
  canSeeStats = computed(() => this.permissionService.hasPermission('asistencia.stats'));
}
