import { Component, effect, inject, input, output, signal } from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import {RouterLink} from '@angular/router';
import {
  CreateSocialRecordRequest,
  SocialRecordDetail,
  PersonType
} from '../../interfaces/social-record.interface';

@Component({
  selector: 'app-social-record-form',
  imports: [ReactiveFormsModule,RouterLink],
  templateUrl: './social-record-form.component.html',
  styleUrl: './social-record-form.component.css'
})
export class SocialRecordFormComponent {

  private fb = inject(FormBuilder);

  // 'create' para Nueva Ficha
  // 'edit' para Editar Ficha
  mode = input<'create' | 'edit'>('create');

  // Datos que recibiremos cuando editemos una ficha
  initialData = input<SocialRecordDetail | null>(null);

  // El componente padre recibe los datos cuando se presiona Guardar
  submittedForm = output<CreateSocialRecordRequest>();

  saving = signal(false);

  loading = input(false);

  errorMessage = signal<string | null>(null);
  
  successMessage = signal<string | null>(null);
  

  submitted = signal(false);

  showContact = signal(false);

  showMoreInfo = signal(false);


  form = this.fb.nonNullable.group({

    firstName: [
      '',
      [
        Validators.required,
        Validators.minLength(2)
      ]
    ],

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

      address: ['']

    })

  });


  constructor() {

    /*
     * Cuando lleguen los datos de una ficha
     * en modo edición, precargamos el formulario.
     */
    effect(() => {

      const data = this.initialData();

      if (!data) {
        return;
      }

      this.form.patchValue({

        firstName: data.firstName ?? '',

        lastName: data.lastName ?? '',

        dni: data.dni ?? '',

        dateOfBirth: data.dateOfBirth
          ? data.dateOfBirth.substring(0, 10)
          : '',

        phone: data.phone ?? '',

        reasonForEntry:
          data.reasonForEntry ?? '',

        entryDate: data.entryDate
          ? data.entryDate.substring(0, 10)
          : '',

        housingSituation:
          data.housingSituation ?? '',

        overnightLocation:
          data.overnightLocation ?? '',

        occupation:
          data.occupation ?? '',

        hasDocumentation:
          data.hasDocumentation,

        generalNotes:
          data.generalNotes ?? '',

        contact: {

          firstName:
            data.contact?.firstName ?? '',

          lastName:
            data.contact?.lastName ?? '',

          phone:
            data.contact?.phone ?? '',

          address:
            data.contact?.address ?? ''

        }

      });

      /*
       * Los datos recién cargados no cuentan
       * como modificaciones del usuario.
       */
      this.form.markAsPristine();

    });

  }


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

    /*
     * Mantiene la validación del formulario.
     */
    if (this.form.invalid) {

      this.form.markAllAsTouched();

      return;
    }


    const {
      firstName,
      lastName,
      dni,
      generalNotes,
      contact,
      dateOfBirth,
      phone,
      reasonForEntry,
      entryDate,
      housingSituation,
      overnightLocation,
      occupation,
      hasDocumentation
    } = this.form.getRawValue();


    /*
     * Validación específica del contacto.
     *
     * Si se completó algún dato del contacto,
     * el nombre pasa a ser obligatorio.
     */
    const contactHasData =
      !!(
        contact.lastName ||
        contact.phone ||
        contact.address
      );


    if (contactHasData && !contact.firstName) {

      this.errorMessage.set(
        'El nombre del contacto es obligatorio si cargás sus datos.'
      );

      return;
    }


    this.errorMessage.set(null);


    const request: CreateSocialRecordRequest = {

      firstName,

      lastName:
        lastName || null,

      dni:
        dni || null,

      dateOfBirth:
        dateOfBirth || null,

      phone:
        phone || null,

      /*
       * Toda ficha nace como Ambulatoria.
       */
      personType:
        PersonType.Ambulatory,

      reasonForEntry:
        reasonForEntry || null,

      entryDate:
        entryDate || null,

      housingSituation:
        housingSituation || null,

      overnightLocation:
        overnightLocation || null,

      occupation:
        occupation || null,

      generalNotes:
        generalNotes || null,

      hasDocumentation,

      contact: contact.firstName
        ? {
            firstName:
              contact.firstName,

            lastName:
              contact.lastName || null,

            phone:
              contact.phone || null,

            address:
              contact.address || null
          }
        : null

    };


    /*
     * El formulario no sabe si estamos creando
     * o editando.
     *
     * Simplemente entrega los datos al padre.
     */
    this.submittedForm.emit(request);
  }


  /*
   * Lo vamos a utilizar después con CanDeactivate.
   */
  canDeactivate(): boolean {

    return !this.form.dirty;

  }

}