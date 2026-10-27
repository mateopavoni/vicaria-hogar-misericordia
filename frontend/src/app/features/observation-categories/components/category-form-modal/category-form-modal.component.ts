import { Component, OnInit, input, output, signal } from '@angular/core';
import { UiButtonComponent, UiFormFieldComponent, UiInputDirective, UiModalComponent } from '../../../../shared/ui';
import { CreateCategoryDto, ObservationCategory } from '../../../social-records/interfaces/observation-category.interface';
import { DESCRIPTION_MAX, NAME_MAX, validateCategory } from '../../utils/category-filter.util';

/** SCRUM-180: crear o editar una categoría de observación (DUMB). Sin `category` = alta. */
@Component({
  selector: 'app-category-form-modal',
  imports: [UiModalComponent, UiButtonComponent, UiFormFieldComponent, UiInputDirective],
  template: `
    <ui-modal
      [title]="category() ? 'Editar categoría' : 'Nueva categoría'"
      subtitle="Las categorías las ve todo el equipo del Centro Barrial."
      size="md"
      [closeOnBackdrop]="false"
      (closed)="closed.emit()"
    >
      <div class="flex flex-col gap-4">
        <ui-form-field label="Nombre" for="category-name" [required]="true" [error]="errors().name">
          <input id="category-name" uiInput type="text" [attr.maxlength]="nameMax" [value]="name()" (input)="name.set($any($event.target).value)" />
        </ui-form-field>

        <ui-form-field label="Descripción" for="category-description" [error]="errors().description">
          <textarea id="category-description" uiInput rows="3" [attr.maxlength]="descriptionMax" [value]="description()" (input)="description.set($any($event.target).value)"></textarea>
        </ui-form-field>

        @if (errorMessage()) {
          <p class="text-[12px] font-semibold text-rose-500" role="alert">{{ errorMessage() }}</p>
        }
      </div>

      <ng-container ngProjectAs="[modal-footer]">
        <button uiButton variant="secondary" type="button" [disabled]="saving()" (click)="closed.emit()">Cancelar</button>
        <button uiButton type="button" [loading]="saving()" [disabled]="saving()" (click)="submit()">Guardar</button>
      </ng-container>
    </ui-modal>
  `,
})
export class CategoryFormModalComponent implements OnInit {
  category = input<ObservationCategory | null>(null);
  saving = input(false);
  errorMessage = input<string | null>(null);

  saved = output<CreateCategoryDto>();
  closed = output<void>();

  readonly nameMax = NAME_MAX;
  readonly descriptionMax = DESCRIPTION_MAX;

  name = signal('');
  description = signal('');
  private submitted = signal(false);

  ngOnInit(): void {
    this.name.set(this.category()?.name ?? '');
    this.description.set(this.category()?.description ?? '');
  }

  errors = () => (this.submitted() ? validateCategory(this.name(), this.description()) : { name: null, description: null });

  submit(): void {
    this.submitted.set(true);
    const errors = validateCategory(this.name(), this.description());
    if (errors.name || errors.description) {
      return;
    }
    this.saved.emit({ name: this.name().trim(), description: this.description().trim() || null });
  }
}
