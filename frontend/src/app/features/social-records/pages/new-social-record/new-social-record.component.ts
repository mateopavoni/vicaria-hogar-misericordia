import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { SocialRecordsService } from '../../services/social-records.service';
import { PersonType } from '../../interfaces/social-record.interface';

@Component({
  selector: 'app-new-social-record',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './new-social-record.component.html',
  styleUrl: './new-social-record.component.css'
})
export class NewSocialRecordComponent {
  private fb = inject(FormBuilder);
  private socialRecordsService = inject(SocialRecordsService);

  loading = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  // solo mostramos errores de validación después de intentar enviar el formulario
  submitted = signal(false);
  // el contacto de referencia es un sub-formulario opcional y colapsable (SCRUM-109)
  showContact = signal(false);

  personTypes = [
    { value: PersonType.Ambulatory, label: 'Ambulatorio' },
    { value: PersonType.Resident, label: 'Residente' },
  ];

  form = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, Validators.minLength(2)]],
    lastName: [''],
    dni: [''],
    personType: this.fb.control<PersonType | null>(null),
    generalNotes: [''],
    contact: this.fb.nonNullable.group({
      firstName: [''],
      lastName: [''],
      phone: [''],
      address: [''],
    }),
  });

  toggleContact(): void {
    this.showContact.update(value => !value);
  }

  submit(): void {
    if (this.loading()) {
      return;
    }

    this.submitted.set(true);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { firstName, lastName, dni, personType, generalNotes, contact } = this.form.getRawValue();

    // el contacto es opcional, pero si se completa algún dato requiere nombre (igual que el backend)
    const contactHasData = !!(contact.lastName || contact.phone || contact.address);
    if (contactHasData && !contact.firstName) {
      this.errorMessage.set('El nombre del contacto es obligatorio si cargás sus datos.');
      return;
    }

    this.errorMessage.set(null);
    this.successMessage.set(null);
    this.loading.set(true);

    this.socialRecordsService.create({
      firstName,
      lastName: lastName || null,
      dni: dni || null,
      personType,
      generalNotes: generalNotes || null,
      hasDocumentation: false,
      contact: contact.firstName ? {
        firstName: contact.firstName,
        lastName: contact.lastName || null,
        phone: contact.phone || null,
        address: contact.address || null,
      } : null,
    }).subscribe({
      next: () => {
        this.loading.set(false);
        this.submitted.set(false);
        // sin listado de fichas todavía (SCRUM-6), mostramos éxito acá en vez de navegar
        this.successMessage.set('Ficha creada correctamente.');
        this.form.reset();
        this.showContact.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err?.error?.message || 'Ocurrió un error al guardar la ficha.');
      }
    });
  }
}
