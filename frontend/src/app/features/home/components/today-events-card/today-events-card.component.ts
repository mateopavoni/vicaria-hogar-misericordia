import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CalendarEventsService } from '../../../calendar/services/calendar-events.service';
import { CalendarEvent } from '../../../calendar/interfaces/calendar-event.interface';
import { todayLocalIso } from '../../../../shared/utils/date.util';
import { sortByStart, timeOf } from '../../utils/home-summary.util';

/** Eventos del calendario general de hoy. El que lo monta decide el permiso (calendario.view). */
@Component({
  selector: 'app-today-events-card',
  imports: [RouterLink],
  template: `
    <section class="rounded-2xl border border-[#B8C8FA] bg-white p-5 shadow-[0_8px_30px_rgba(79,115,232,0.08)]">
      <div class="flex items-center justify-between gap-3">
        <p class="text-[16px] font-bold text-slate-900">Calendario de hoy</p>
        <a routerLink="/dashboard/calendario" class="text-[13px] font-semibold text-[#4167D9]">Ver calendario</a>
      </div>
      @if (!loaded()) {
        <p class="mt-2 text-[13px] text-slate-500">Cargando...</p>
      } @else if (events().length === 0) {
        <p class="mt-2 text-[13px] text-slate-500">No hay eventos para hoy.</p>
      } @else {
        <ul class="mt-3 space-y-2">
          @for (e of events(); track e.id) {
            <li class="flex gap-3 text-[13px] text-slate-700">
              <span class="w-12 shrink-0 font-semibold text-slate-900">{{ time(e.start) || 'Todo el día' }}</span>
              <span>{{ e.title }}</span>
            </li>
          }
        </ul>
      }
    </section>
  `,
})
export class TodayEventsCardComponent implements OnInit {
  private service = inject(CalendarEventsService);

  events = signal<CalendarEvent[]>([]);
  loaded = signal(false);
  time = timeOf;

  ngOnInit(): void {
    const today = todayLocalIso();
    this.service.getByRange(today, today, 'general').subscribe({
      next: (list) => {
        this.events.set(sortByStart(list));
        this.loaded.set(true);
      },
      error: () => this.loaded.set(true),
    });
  }
}
