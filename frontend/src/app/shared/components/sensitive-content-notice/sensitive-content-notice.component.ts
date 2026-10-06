import { Component } from '@angular/core';

// recordatorio del acuerdo de no cargar información de abusos en el sistema (se maneja solo
// de forma verbal); el backend además rechaza las frases inequívocas
@Component({
  selector: 'app-sensitive-content-notice',
  template: `
    <p class="mt-1 text-[11px] leading-snug text-slate-500">
      No cargues información sobre abusos: ese tema se maneja solo de forma verbal con el equipo.
    </p>
  `,
})
export class SensitiveContentNoticeComponent {}
