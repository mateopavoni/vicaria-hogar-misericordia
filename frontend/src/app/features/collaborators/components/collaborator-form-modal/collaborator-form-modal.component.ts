import { Component, computed, effect, inject, input, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { UiButtonComponent, UiFormFieldComponent, UiInputDirective, UiModalComponent } from '../../../../shared/ui';
import { Collaborator, CollaboratorType, CreateCollaboratorDto } from '../../interfaces/collaborator.interface';

/**
 * Modal para dar de alta o editar un colaborador (SCRUM-201).
 * collaborator = null -> modo "Nuevo colaborador". collaborator = {...} -> modo "Editar".
 * El padre (collaborator-management) decide si llama a create() o update() al recibir "saved".
 * Único campo obligatorio: nombre (ver AC de SCRUM-18).
 */
@Component({
  selector: 'app-collaborator-form-modal',
  imports: [ReactiveFormsModule, UiModalComponent, UiButtonComponent, UiFormFieldComponent, UiInputDirective],
  templateUrl: './collaborator-form-modal.component.html',
})
export class CollaboratorFormModalComponent {
  private fb = inject(FormBuilder);

  // null = alta de un colaborador nuevo; con valor = edición de ese colaborador
  collaborator = input<Collaborator | null>(null);

  // se prende mientras el padre espera la respuesta del backend (crear/editar)
  saving = input(false);

  // mensaje de error que devuelva el backend (ej. DNI duplicado)
  errorMessage = input<string | null>(null);

  saved = output<CreateCollaboratorDto>();
  closed = output<void>();

  readonly CollaboratorType = CollaboratorType;

  title = computed(() => (this.collaborator() ? 'Editar colaborador' : 'Nuevo colaborador'));

  submitted = false;

  form = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.maxLength(100)]],
    dni: ['', [Validators.maxLength(20)]],
    phone: ['', [Validators.maxLength(30)]],
    email: ['', [Validators.email, Validators.maxLength(150)]],
    type: [''],
    workArea: ['', [Validators.maxLength(100)]],
  });

  constructor() {
    // precarga el form cuando se abre en modo edición
    effect(() => {
      const collaborator = this.collaborator();
      if (collaborator) {
        this.form.patchValue({
          firstName: collaborator.firstName,
          lastName: collaborator.lastName ?? '',
          dni: collaborator.dni ?? '',
          phone: collaborator.phone ?? '',
          email: collaborator.email ?? '',
          type: collaborator.type ?? '',
          workArea: collaborator.workArea ?? '',
        });
      }
    });
  }

  get firstNameError(): string | null {
    const control = this.form.controls.firstName;
    if (!this.submitted || !control.errors) {
      return null;
    }
    if (control.errors['required']) {
      return 'El nombre es obligatorio.';
    }
    if (control.errors['maxlength']) {
      return 'El nombre no puede superar los 100 caracteres.';
    }
    return null;
  }

  get lastNameError(): string | null {
    return this.maxLengthError('lastName', 'El apellido no puede superar los 100 caracteres.');
  }

  get dniError(): string | null {
    return this.maxLengthError('dni', 'El DNI no puede superar los 20 caracteres.');
  }

  get phoneError(): string | null {
    return this.maxLengthError('phone', 'El teléfono no puede superar los 30 caracteres.');
  }

  get emailError(): string | null {
    const control = this.form.controls.email;
    if (!this.submitted || !control.errors) {
      return null;
    }
    if (control.errors['email']) {
      return 'Ingresá un email válido.';
    }
    if (control.errors['maxlength']) {
      return 'El email no puede superar los 150 caracteres.';
    }
    return null;
  }

  get workAreaError(): string | null {
    return this.maxLengthError('workArea', 'El área de trabajo no puede superar los 100 caracteres.');
  }

  private maxLengthError(controlName: 'lastName' | 'dni' | 'phone' | 'workArea', message: string): string | null {
    const control = this.form.controls[controlName];
    if (!this.submitted || !control.errors) {
      return null;
    }
    return control.errors['maxlength'] ? message : null;
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

    const { firstName, lastName, dni, phone, email, type, workArea } = this.form.getRawValue();

    this.saved.emit({
      firstName: firstName.trim(),
      lastName: lastName.trim() || null,
      dni: dni.trim() || null,
      phone: phone.trim() || null,
      email: email.trim() || null,
      type: (type as CollaboratorType) || null,
      workArea: workArea.trim() || null,
    });
  }
}
