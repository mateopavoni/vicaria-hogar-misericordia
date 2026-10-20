import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { UiButtonComponent, UiButtonVariant } from '../button/button.component';
import { UiModalComponent } from '../modal/modal.component';

/**
 * Modal de confirmación genérico: reemplaza al `confirm()` nativo del navegador
 * (bloqueante, sin estilo, no se puede testear) en cualquier acción "¿estás seguro?".
 *
 * Uso típico (con un signal que guarda qué se está por confirmar):
 *   pendingUser = signal<ManagedUser | null>(null);
 *
 *   <ui-confirm-dialog
 *     *ngIf="pendingUser() as user"
 *     title="Inactivar usuario"
 *     [message]="'¿Estás seguro de que deseas inhabilitar la cuenta de ' + user.name + '?'"
 *     confirmLabel="Inactivar"
 *     confirmVariant="warning"
 *     (confirmed)="doDeactivate(user)"
 *     (cancelled)="pendingUser.set(null)"
 *   />
 */
@Component({
  selector: 'ui-confirm-dialog',
  imports: [UiModalComponent, UiButtonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ui-modal [title]="title()" size="sm" (closed)="cancelled.emit()">
      <p class="text-[13px] text-slate-600">{{ message() }}</p>

      <ng-container ngProjectAs="[modal-footer]">
        <button uiButton variant="secondary" type="button" (click)="cancelled.emit()">
          {{ cancelLabel() }}
        </button>
        <button uiButton [variant]="confirmVariant()" type="button" [loading]="loading()" (click)="confirmed.emit()">
          {{ confirmLabel() }}
        </button>
      </ng-container>
    </ui-modal>
  `,
})
export class UiConfirmDialogComponent {
  readonly title = input.required<string>();
  readonly message = input.required<string>();
  readonly confirmLabel = input('Confirmar');
  readonly cancelLabel = input('Cancelar');
  readonly confirmVariant = input<UiButtonVariant>('primary');
  /** Deshabilita el botón de confirmar y le muestra el spinner mientras la acción está en curso. */
  readonly loading = input(false);

  readonly confirmed = output<void>();
  readonly cancelled = output<void>();
}
