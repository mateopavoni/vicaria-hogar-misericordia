import {Component, inject, OnInit,signal,viewChild} from '@angular/core';
import { ActivatedRoute,  Router} from '@angular/router';
import {  SocialRecordsService} from '../../services/social-records.service';
import { SocialRecordDetail, CreateSocialRecordRequest,PersonType} from '../../interfaces/social-record.interface';
import { SocialRecordFormComponent} from '../../components/social-record-form/social-record-form.component';
import { ComponentCanDeactivate } from '../../../../core/guards/pending-changes.guard';


@Component({
  selector: 'app-social-record-edit',
  imports: [ SocialRecordFormComponent ],
  templateUrl: './social-record-edit.component.html',
  styleUrl: './social-record-edit.component.css'
})
export class SocialRecordEditComponent
  implements OnInit, ComponentCanDeactivate {

    // Obtenemos la referencia al componente hijo del formulario
  formComponent = viewChild(SocialRecordFormComponent);

  private route = inject(ActivatedRoute);

  private router = inject(Router);

  private socialRecordsService =
    inject(SocialRecordsService);


  loading = signal(true);

  saving = signal(false);

  errorMessage =
    signal<string | null>(null);

  successMessage =
    signal<string | null>(null);


  recordId = '';

  record =
    signal<SocialRecordDetail | null>(null);


  ngOnInit(): void {

    const id =
      this.route.snapshot.paramMap.get('id');


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

  canDeactivate(): boolean {
    return this.formComponent()?.canDeactivate() ?? true;
  }

  private loadRecord(id: string): void {

    this.loading.set(true);

    this.errorMessage.set(null);


    // this.socialRecordsService
    //   .getById(id)
    //   .subscribe({

    //     next: (record) => {

    //       this.record.set(record);

    //       this.loading.set(false);

    //     },


    //     error: (err) => {

    //       console.error(
    //         'Error al cargar la ficha:',
    //         err
    //       );

    //       this.loading.set(false);

    //       this.errorMessage.set(
    //         'No se pudo cargar la ficha.'
    //       );

    //     }

    //   });

    // --- MOCK TEMPORAL DE DATOS PARA EDICIÓN ---
  setTimeout(() => {
    this.record.set({
      id: id || '123',
      firstName: 'María Belén',
      lastName: 'González',
      dni: '38123456',
      dateOfBirth: '1995-04-12T00:00:00.000Z',
      phone: '3519876543',
      personType: PersonType.Resident,
      reasonForEntry: 'Acompañamiento e ingreso por situación habitacional.',
      entryDate: '2026-01-15T00:00:00.000Z',
      housingSituation: 'Parador / Casa de Convivencia',
      overnightLocation: 'Casona',
      occupation: 'Estudiante',
      hasDocumentation: true,
      generalNotes: 'Observaciones generales ficticias cargadas en modo edición.',
      contact: {
        firstName: 'Juan',
        lastName: 'González',
        phone: '3511112233',
        address: 'Av. Colón 1234'
      }
    } as SocialRecordDetail);

    this.loading.set(false); // Apagamos el estado de carga
  }, 200); // Pequeño delay opcional para simular la carga
}
  

  updateRecord(data: CreateSocialRecordRequest): void {
    this.errorMessage.set(null);
    this.successMessage.set(null);
    this.saving.set(true);

    this.socialRecordsService
      .update(this.recordId, data)
      .subscribe({
        next: (updatedRecord) => {
          this.saving.set(false);
          this.successMessage.set('Ficha actualizada correctamente.');
          
          // Actualizamos la signal record para refrescar la instancia si fuera necesario
          this.record.set(updatedRecord);

          // Opción A: Redirigir al detalle de la ficha pasados 1.5 segundos
          setTimeout(() => {
            this.router.navigate(['/dashboard/fichas', this.recordId]);
          }, 1500);
        },
        error: (err) => {
          console.error('Error al actualizar la ficha:', err);
          this.saving.set(false);
          this.errorMessage.set(
            err?.error?.message || 'Ocurrió un error al actualizar la ficha.'
          );
        }
      });
  }

  

}