import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PermissionService } from '../../../../core/auth/permission.service';
import { Permission } from '../../../../core/auth/permissions';

interface QuickLink {
  label: string;
  hint: string;
  route: string;
  permission: Permission;
}

const LINKS: QuickLink[] = [
  { label: 'Fichas', hint: 'Buscar y ver fichas sociales', route: '/dashboard/fichas', permission: 'fichas.view' },
  { label: 'Asistencia', hint: 'Registrar la asistencia del día', route: '/dashboard/asistencia', permission: 'asistencia.create' },
  { label: 'Medicación', hint: 'Agenda de tomas de hoy', route: '/dashboard/medicacion', permission: 'medicamentos.view' },
  { label: 'Calendario', hint: 'Eventos y visitas', route: '/dashboard/calendario', permission: 'calendario.view' },
];

/** Accesos rápidos del inicio: solo se muestran los de pantallas a las que el rol tiene permiso. */
@Component({
  selector: 'app-quick-links',
  imports: [RouterLink],
  template: `
    @if (links().length > 0) {
      <div class="mb-5 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        @for (l of links(); track l.route) {
          <a
            [routerLink]="l.route"
            class="rounded-2xl border border-[#B8C8FA] bg-white p-4 shadow-[0_8px_30px_rgba(79,115,232,0.08)] transition hover:bg-[#F5F7FF]"
          >
            <p class="text-[15px] font-bold text-slate-900">{{ l.label }}</p>
            <p class="mt-1 text-[13px] text-slate-500">{{ l.hint }}</p>
          </a>
        }
      </div>
    }
  `,
})
export class QuickLinksComponent {
  private permissionService = inject(PermissionService);
  links = computed(() => LINKS.filter((l) => this.permissionService.hasPermission(l.permission)));
}
