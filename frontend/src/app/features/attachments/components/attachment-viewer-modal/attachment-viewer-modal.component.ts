import { Component, input, output } from '@angular/core';
import { UiButtonComponent, UiModalComponent } from '../../../../shared/ui';
import { PersonAttachment } from '../../interfaces/attachment.interface';

/** SCRUM-256: visor de imágenes dentro del sistema (DUMB). */
@Component({
  selector: 'app-attachment-viewer-modal',
  imports: [UiModalComponent, UiButtonComponent],
  template: `
    <ui-modal [title]="attachment().name" size="lg" (closed)="closed.emit()">
      <div class="flex max-h-[65vh] items-center justify-center overflow-auto rounded-lg bg-slate-50">
        <img [src]="attachment().url" [alt]="attachment().name" class="max-h-[65vh] max-w-full object-contain" />
      </div>

      <ng-container ngProjectAs="[modal-footer]">
        <a
          [href]="attachment().url"
          [attr.download]="attachment().name"
          class="rounded-md border border-[#D5DDF3] bg-white px-4 py-2 text-[13px] font-semibold text-slate-700 hover:bg-slate-50"
        >
          Descargar
        </a>
        <button uiButton type="button" (click)="closed.emit()">Cerrar</button>
      </ng-container>
    </ui-modal>
  `,
})
export class AttachmentViewerModalComponent {
  attachment = input.required<PersonAttachment>();
  closed = output<void>();
}
