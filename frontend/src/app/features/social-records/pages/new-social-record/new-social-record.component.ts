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
  // información personal extra, opcional y colapsable, mismo patrón que el contacto
  showMoreInfo = signal(false);

  form = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, Validators.minLength(2)]],
    lastName: [''],
    dni: [''],
    dateOfBirth: [''],
    phone: [''],
    reasonForEntry: [''],
    entryDate: [''],
    housingSituation: [''],
    overnightLocation: [''],
    occupation: [''],
    hasDocumentation: [false],
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

  toggleMoreInfo(): void {
    this.showMoreInfo.update(value => !value);
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

    const {
      firstName, lastName, dni, generalNotes, contact,
      dateOfBirth, phone, reasonForEntry, entryDate,
      housingSituation, overnightLocation, occupation, hasDocumentation,
    } = this.form.getRawValue();

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
      dateOfBirth: dateOfBirth || null,
      phone: phone || null,
      personType: PersonType.Ambulatory, // toda ficha nace ambulatoria; pasar a Residente es otra acción (no es una eleccion al crear)
      reasonForEntry: reasonForEntry || null,
      entryDate: entryDate || null,
      housingSituation: housingSituation || null,
      overnightLocation: overnightLocation || null,
      occupation: occupation || null,
      generalNotes: generalNotes || null,
      hasDocumentation,
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
