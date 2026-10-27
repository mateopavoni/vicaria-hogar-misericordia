import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CasaConvivenciaVisitsService } from '../../../casa-convivencia-visits/services/casa-convivencia-visits.service';
import { Visit } from '../../../casa-convivencia-visits/interfaces/visit.interface';
import { todayLocalIso } from '../../../../shared/utils/date.util';
import { sortByStart, timeOf } from '../../utils/home-summary.util';

/** Visitas de hoy a la Casa de Convivencia (sin canceladas). El que lo monta decide el permiso (visitas.view). */
@Component({
  selector: 'app-today-visits-card',
  imports: [RouterLink],
  template: `
    <section class="rounded-2xl border border-[#B8C8FA] bg-white p-5 shadow-[0_8px_30px_rgba(79,115,232,0.08)]">
      <div class="flex items-center justify-between gap-3">
        <p class="text-[16px] font-bold text-slate-900">Visitas de hoy · Casa de Convivencia</p>
        <a routerLink="/dashboard/calendario" class="text-[13px] font-semibold text-[#4167D9]">Ver visitas</a>
      </div>
      @if (!loaded()) {
        <p class="mt-2 text-[13px] text-slate-500">Cargando...</p>
      } @else if (visits().length === 0) {
        <p class="mt-2 text-[13px] text-slate-500">No hay visitas programadas para hoy.</p>
      } @else {
        <ul class="mt-3 space-y-2">
          @for (v of visits(); track v.id) {
            <li class="flex gap-3 text-[13px] text-slate-700">
              <span class="w-12 shrink-0 font-semibold text-slate-900">{{ time(v.start) }}</span>
              <span>
                {{ v.visitorName }} visita a {{ v.residentName }}
                @if (v.status === 'done') {
                  <span class="ml-1 rounded bg-emerald-50 px-1.5 py-0.5 text-[11px] font-semibold text-emerald-700">Realizada</span>
                }
              </span>
            </li>
          }
        </ul>
      }
    </section>
  `,
})
export class TodayVisitsCardComponent implements OnInit {
  private service = inject(CasaConvivenciaVisitsService);

  visits = signal<Visit[]>([]);
  loaded = signal(false);
  time = timeOf;

  ngOnInit(): void {
    const today = todayLocalIso();
    this.service.getByRange(today, today).subscribe({
      next: (list) => {
        this.visits.set(sortByStart(list.filter((v) => v.status !== 'cancelled')));
        this.loaded.set(true);
      },
      error: () => this.loaded.set(true),
    });
  }
}
