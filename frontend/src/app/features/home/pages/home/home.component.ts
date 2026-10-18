import { Component, computed, inject } from '@angular/core';
import { AuthService } from '../../../../core/auth/auth.service';
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
        <h1 class="text-[26px] font-bold text-slate-900">Inicio</h1>
        <p class="mt-1 text-[14px] text-slate-500">Hola, {{ firstName() }}.</p>
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
  private authService = inject(AuthService);
  private permissionService = inject(PermissionService);

  firstName = computed(() => this.authService.user()?.name ?? '');
  canSeeMedication = computed(() => this.permissionService.hasPermission('medicamentos.view'));
  canSeeStats = computed(() => this.permissionService.hasPermission('asistencia.stats'));
}
