import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { todayLocalIso } from '../../../../shared/utils/date.util';
import { MedicationService } from '../../services/medication.service';

/**
 * SCRUM-238: acceso directo a la agenda de medicamentos de hoy, visible en el inicio.
 * Muestra cuántas tomas quedan sin registrar. El que lo monta decide el permiso.
 */
@Component({
  selector: 'app-medication-agenda-shortcut',
  imports: [RouterLink],
  template: `
    <a
      routerLink="/dashboard/medicacion"
      class="mb-5 flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-[#B8C8FA] bg-white p-5 shadow-[0_8px_30px_rgba(79,115,232,0.08)] transition hover:bg-[#F5F7FF]"
    >
      <div>
        <p class="text-[16px] font-bold text-slate-900">Agenda de medicamentos de hoy</p>
        <p class="mt-1 text-[13px] text-slate-500">
          @if (loaded()) {
            @if (total() === 0) {
              No hay tomas para hoy.
            } @else if (pending() === 0) {
              Las {{ total() }} tomas de hoy ya están registradas.
            } @else {
              {{ pending() }} de {{ total() }} tomas sin registrar.
            }
          } @else {
            Cargando...
          }
        </p>
      </div>
      <span class="rounded-md bg-[#4167D9] px-4 py-2 text-[13px] font-semibold text-white">Ver agenda</span>
    </a>
  `,
})
export class MedicationAgendaShortcutComponent implements OnInit {
  private medicationService = inject(MedicationService);

  total = signal(0);
  pending = signal(0);
  loaded = signal(false);

  ngOnInit(): void {
    this.medicationService.getAgenda(todayLocalIso()).subscribe({
      next: (agenda) => {
        const items = agenda.slots.flatMap((s) => s.items);
        this.total.set(items.length);
        this.pending.set(items.filter((i) => !i.administration).length);
        this.loaded.set(true);
      },
      // si falla, el acceso directo igual sirve para entrar a la agenda
      error: () => this.loaded.set(true),
    });
  }
}
