import { Component, computed, effect, inject, input, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { UiButtonComponent, UiFormFieldComponent, UiInputDirective, UiModalComponent } from '../../../../../shared/ui';
import { CreateCategoryDto, ObservationCategory } from '../../interfaces/observation-category.interface';

/**
 * Modal para crear o editar una categoría de observaciones.
 * category = null -> modo "Nueva categoría". category = {...} -> modo "Editar".
 * El padre (category-settings) decide si llama a create() o update() al recibir "saved".
 */
@Component({
  selector: 'app-category-form-modal',
  imports: [ReactiveFormsModule, UiModalComponent, UiButtonComponent, UiFormFieldComponent, UiInputDirective],
  templateUrl: './category-form-modal.component.html',
})
export class CategoryFormModalComponent {
  private fb = inject(FormBuilder);

  // null = alta de una categoría nueva; con valor = edición de esa categoría
  category = input<ObservationCategory | null>(null);

  // se prende mientras el padre espera la respuesta del backend (crear/editar)
  saving = input(false);

  // mensaje de error que devuelva el backend (ej. nombre duplicado)
  errorMessage = input<string | null>(null);

  saved = output<CreateCategoryDto>();
  closed = output<void>();

  title = computed(() => (this.category() ? 'Editar categoría' : 'Nueva categoría'));

  submitted = false;

  form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', [Validators.maxLength(500)]],
  });

  constructor() {
    // precarga el form cuando se abre en modo edición
    effect(() => {
      const cat = this.category();
      if (cat) {
        this.form.patchValue({
          name: cat.name,
          description: cat.description ?? '',
        });
      }
    });
  }

  get nameError(): string | null {
    const control = this.form.controls.name;
    if (!this.submitted || !control.errors) {
      return null;
    }
    if (control.errors['required']) {
      return 'El nombre de la categoría es obligatorio.';
    }
    if (control.errors['maxlength']) {
      return 'El nombre no puede superar los 100 caracteres.';
    }
    return null;
  }

  get descriptionError(): string | null {
    const control = this.form.controls.description;
    if (!this.submitted || !control.errors) {
      return null;
    }
    if (control.errors['maxlength']) {
      return 'La descripción no puede superar los 500 caracteres.';
    }
    return null;
  }

  onClose(): void {
    this.closed.emit();
  }

  submit(): void {
    this.submitted = true;

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { name, description } = this.form.getRawValue();

    this.saved.emit({
      name: name.trim(),
      description: description.trim() || null,
    });
  }
}
