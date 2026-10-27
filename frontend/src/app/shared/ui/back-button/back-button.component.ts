import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

/**
 * Botón "Volver" único para todas las pantallas. Uso:
 *   <ui-back-button to="/dashboard/fichas" label="Volver a Fichas" />
 */
@Component({
  selector: 'ui-back-button',
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'no-print mb-4 inline-block' },
  template: `
    <a
      [routerLink]="to()"
      class="group inline-flex h-[38px] items-center gap-2 rounded-lg border border-[#D5DDF3] bg-white px-4
             text-[13px] font-semibold text-[#4167D9] shadow-sm transition
             hover:border-[#B8C8FA] hover:bg-[#F5F7FF] hover:text-[#3556C0]
             focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#6B8DF5]"
    >
      <svg
        class="h-4 w-4 transition-transform group-hover:-translate-x-0.5"
        fill="none"
        stroke="currentColor"
        stroke-width="2"
        viewBox="0 0 24 24"
        aria-hidden="true"
      >
        <path stroke-linecap="round" stroke-linejoin="round" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
      </svg>
      {{ label() }}
    </a>
  `,
})
export class UiBackButtonComponent {
  readonly to = input.required<string | readonly unknown[]>();
  readonly label = input('Volver');
}
