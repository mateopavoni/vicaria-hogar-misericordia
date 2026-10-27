import { Component, input } from '@angular/core';
import { RecordExportDocument } from '../../utils/record-export.util';

/**
 * SCRUM-253: vista de impresión de la ficha exportada (DUMB). Solo se ve al imprimir (@media
 * print, clase `print:block`); `print-target` hace que, con la clase `print-only-target` en el
 * body (ver styles.css), se imprima únicamente este bloque.
 */
@Component({
  selector: 'app-record-export-print',
  template: `
    @if (doc(); as d) {
      <article class="print-target hidden bg-white text-slate-900 print:block">
        <p class="text-[11px] text-slate-500">{{ d.systemName }}</p>
        <h1 class="mt-1 text-[24px] font-bold">{{ d.title }}</h1>
        <p class="text-[11px] text-slate-500">{{ d.exportInfo }}</p>
        <p class="text-[11px] text-slate-500">Secciones incluidas: {{ d.sectionsLabel }}</p>

        @for (section of d.sections; track section.title) {
          <section class="mt-5 break-inside-avoid-page">
            <h2 class="border-b border-slate-300 pb-1 text-[15px] font-bold">{{ section.title }}</h2>

            @if (section.fields; as fields) {
              <dl class="mt-2 grid grid-cols-[170px_1fr] gap-x-4 gap-y-1 text-[12px]">
                @for (field of fields; track field.label) {
                  <dt class="font-semibold">{{ field.label }}</dt>
                  <dd class="whitespace-pre-line">{{ field.value }}</dd>
                }
              </dl>
            }

            @for (entry of section.entries ?? []; track $index) {
              <div class="mt-2 text-[12px]">
                @if (entry.heading) {
                  <p class="font-semibold">{{ entry.heading }}</p>
                }
                <p class="whitespace-pre-line">{{ entry.text }}</p>
              </div>
            } @empty {
              @if (!section.fields && section.emptyText) {
                <p class="mt-2 text-[12px] text-slate-500">{{ section.emptyText }}</p>
              }
            }
          </section>
        }
      </article>
    }
  `,
})
export class RecordExportPrintComponent {
  doc = input<RecordExportDocument | null>(null);
}
