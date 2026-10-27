import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, catchError, debounceTime, distinctUntilChanged, of, switchMap, tap } from 'rxjs';
import { SocialRecordsService } from '../../../social-records/services/social-records.service';
import { SocialRecordListItem } from '../../../social-records/interfaces/social-record.interface';
import { CreateObservationModalComponent } from '../../../social-records/components/create-observation-modal/create-observation-modal.component';

/**
 * Observación rápida desde el inicio: se busca a la persona por nombre o DNI y se abre el
 * mismo modal de "Nueva observación" de la ficha. El que lo monta decide el permiso
 * (observaciones.create + fichas.view).
 */
@Component({
  selector: 'app-quick-observation',
  imports: [CreateObservationModalComponent],
  template: `
    <section class="mb-5 rounded-2xl border border-[#B8C8FA] bg-white p-5 shadow-[0_8px_30px_rgba(79,115,232,0.08)]">
      <p class="text-[16px] font-bold text-slate-900">Agregar observación rápida</p>
      <p class="mt-1 text-[13px] text-slate-500">Buscá a la persona por nombre o DNI.</p>

      <input
        type="search"
        class="mt-3 w-full rounded-md border border-slate-300 px-3 py-2 text-[14px] outline-none focus:border-[#4167D9]"
        placeholder="Nombre, apellido o DNI"
        aria-label="Buscar persona"
        [value]="term()"
        (input)="onInput($any($event.target).value)"
      />

      @if (successMessage()) {
        <p class="mt-3 rounded-md bg-emerald-50 px-3 py-2 text-[13px] font-semibold text-emerald-700" role="status">
          {{ successMessage() }}
        </p>
      }

      @if (searching()) {
        <p class="mt-3 text-[13px] text-slate-500">Buscando...</p>
      } @else if (searched() && results().length === 0) {
        <p class="mt-3 text-[13px] text-slate-500">No se encontraron personas.</p>
      } @else if (results().length > 0) {
        <ul class="mt-3 divide-y divide-slate-100 rounded-md border border-slate-200">
          @for (r of results(); track r.id) {
            <li class="flex items-center justify-between gap-3 px-3 py-2 text-[13px]">
              <span class="text-slate-800">
                <span class="font-semibold">{{ fullName(r) }}</span>
                @if (r.dni) {
                  <span class="ml-2 text-slate-500">DNI {{ r.dni }}</span>
                }
              </span>
              <button type="button" class="rounded-md bg-[#4167D9] px-3 py-1.5 font-semibold text-white" (click)="select(r)">
                Agregar observación
              </button>
            </li>
          }
        </ul>
      }
    </section>

    @if (selected(); as person) {
      <app-create-observation-modal
        [socialRecordId]="person.id"
        (observationSaved)="onSaved(person)"
        (cancel)="selected.set(null)"
      />
    }
  `,
})
export class QuickObservationComponent {
  private records = inject(SocialRecordsService);
  private search$ = new Subject<string>();

  term = signal('');
  results = signal<SocialRecordListItem[]>([]);
  searching = signal(false);
  searched = signal(false);
  selected = signal<SocialRecordListItem | null>(null);
  successMessage = signal<string | null>(null);

  constructor() {
    this.search$
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        tap((t) => {
          if (t.length < 2) {
            this.results.set([]);
            this.searched.set(false);
            this.searching.set(false);
          } else {
            this.searching.set(true);
          }
        }),
        switchMap((t) =>
          t.length < 2
            ? of(null)
            : this.records.getAll(1, t).pipe(catchError(() => of({ items: [], total: 0, totalPages: 0 }))),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((res) => {
        if (!res) return;
        this.results.set(res.items.slice(0, 5));
        this.searched.set(true);
        this.searching.set(false);
      });
  }

  onInput(value: string): void {
    this.term.set(value);
    this.successMessage.set(null);
    this.search$.next(value.trim());
  }

  fullName(r: SocialRecordListItem): string {
    return [r.firstName, r.lastName].filter(Boolean).join(' ');
  }

  select(r: SocialRecordListItem): void {
    this.selected.set(r);
  }

  onSaved(person: SocialRecordListItem): void {
    this.selected.set(null);
    this.successMessage.set(`Observación guardada en la ficha de ${this.fullName(person)}.`);
  }
}
