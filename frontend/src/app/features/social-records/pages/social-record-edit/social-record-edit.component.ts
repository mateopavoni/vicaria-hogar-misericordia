import {Component, inject, OnInit, signal} from '@angular/core';
import { ActivatedRoute,Router, RouterLink} from '@angular/router';
import { FormBuilder,ReactiveFormsModule,Validators} from '@angular/forms';
import {SocialRecordsService} from '../../services/social-records.service';
import { SocialRecordFormComponent } from '../../components/social-record-form/social-record-form.component';


@Component({
  selector: 'app-social-record-edit',
  imports: [ ReactiveFormsModule, RouterLink, SocialRecordFormComponent],
  templateUrl: './social-record-edit.component.html',
  styleUrl: './social-record-edit.component.css'
})

export class SocialRecordEditComponent
  implements OnInit {

  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private socialRecordsService =
    inject(SocialRecordsService);

  loading = signal(true);
  saving = signal(false);

  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  recordId = '';

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


  ngOnInit(): void {

    const id = this.route.snapshot.paramMap.get('id');

    if (!id) {

      this.errorMessage.set(
        'No se encontró la ficha.'
      );

      this.loading.set(false);

      return;
    }

    this.recordId = id;

    this.loadRecord(id);
  }


  private loadRecord(id: string): void {

    this.loading.set(true);
    this.errorMessage.set(null);

    this.socialRecordsService .getById(id).subscribe({
        next: (record) => {

          this.form.patchValue({

            firstName:
              record.firstName ?? '',

            lastName:
              record.lastName ?? '',

            dni:
              record.dni ?? '',

            dateOfBirth:
              record.dateOfBirth
                ? record.dateOfBirth.substring(0, 10)
                : '',

            phone:
              record.phone ?? '',

            reasonForEntry:
              record.reasonForEntry ?? '',

            entryDate:
              record.entryDate
                ? record.entryDate.substring(0, 10)
                : '',

            housingSituation:
              record.housingSituation ?? '',

            overnightLocation:
              record.overnightLocation ?? '',

            occupation:
              record.occupation ?? '',

            hasDocumentation:
              record.hasDocumentation,

            generalNotes:
              record.generalNotes ?? '',

            contact: {

              firstName:
                record.contact?.firstName ?? '',

              lastName:
                record.contact?.lastName ?? '',

              phone:
                record.contact?.phone ?? '',

              address:
                record.contact?.address ?? ''

            }

          });

          
          //  Los datos fueron cargados desde el backend.
          //  El formulario no debe considerarse modificado.
           
          this.form.markAsPristine();

          this.loading.set(false);
        },

        error: (err) => {

          console.error(
            'Error al cargar la ficha:',
            err
          );

          this.loading.set(false);

          this.errorMessage.set(
            'No se pudo cargar la ficha.'
          );
        }

      });
  }


  submit(): void {

    if (this.saving()) {
      return;
    }

    this.form.markAllAsTouched();

    if (this.form.invalid) {
      return;
    }

    // Guardado lo implementamos cuando tengamos
    // confirmado el endpoint PUT/PATCH del backend.
  }

}