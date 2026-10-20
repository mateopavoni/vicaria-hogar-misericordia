import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Label + control proyectado + error/hint. Reemplaza el bloque repetido de
 * <label> + <input> + mensaje de error que aparecía en cada formulario y modal.
 *   <ui-form-field label="Fecha de egreso" for="exit-date" [required]="true" [error]="err()">
 *     <input id="exit-date" uiInput type="date" formControlName="exitDate" />
 *   </ui-form-field>
 */
@Component({
  selector: 'ui-form-field',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div>
      <label [attr.for]="for()" class="block text-[12px] font-semibold text-slate-700">
        {{ label() }}
        @if (required()) {
          <span class="text-rose-500" aria-hidden="true">*</span>
        }
      </label>
      <ng-content />
      @if (error()) {
        <p class="mt-1 text-[11px] text-rose-500" role="alert">{{ error() }}</p>
      } @else if (hint()) {
        <p class="mt-1 text-[11px] text-slate-400">{{ hint() }}</p>
      }
    </div>
  `,
})
export class UiFormFieldComponent {
  readonly label = input.required<string>();
  readonly for = input<string>();
  readonly required = input(false);
  readonly error = input<string | null>(null);
  readonly hint = input<string | null>(null);
}
