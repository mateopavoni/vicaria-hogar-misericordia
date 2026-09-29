import {Component, inject, OnInit,signal,viewChild} from '@angular/core';
import { ActivatedRoute,  Router} from '@angular/router';
import { Observable, Subject } from 'rxjs';
import {  SocialRecordsService} from '../../services/social-records.service';
import { SocialRecordDetail, CreateSocialRecordRequest} from '../../interfaces/social-record.interface';
import { SocialRecordFormComponent} from '../../components/social-record-form/social-record-form.component';
import { ComponentCanDeactivate } from '../../../../core/guards/pending-changes.guard';
import { UiConfirmDialogComponent } from '../../../../shared/ui';
import { SuccessModalComponent } from '../../../../shared/ui/success-modal/success-modal.component';


@Component({
  selector: 'app-social-record-edit',
  imports: [ SocialRecordFormComponent, UiConfirmDialogComponent, SuccessModalComponent ],
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

  // se muestra en app-success-modal apenas se confirma el guardado exitoso
  // (antes era un cartel verde inline, fácil de no notar).
  showSuccessModal = signal(false);

  // reemplaza al confirm() nativo del navegador que usaba pendingChangesGuard:
  // se muestra este modal cuando el form tiene cambios sin guardar y se intenta salir.
  confirmingLeave = signal(false);
  private leaveDecision$ = new Subject<boolean>();


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

  // Ya no resuelve al toque: si hay cambios sin guardar, abre el modal
  // (ui-confirm-dialog) y le devuelve al guard un Observable<boolean> que
  // recién emite cuando la persona confirma o cancela desde ese modal.
  canDeactivate(): boolean | Observable<boolean> {
    const sinCambiosPendientes = this.formComponent()?.canDeactivate() ?? true;

    if (sinCambiosPendientes) {
      return true;
    }

    this.confirmingLeave.set(true);
    return this.leaveDecision$.asObservable();
  }

  confirmLeave(): void {
    this.confirmingLeave.set(false);
    this.leaveDecision$.next(true);
  }

  cancelLeave(): void {
    this.confirmingLeave.set(false);
    this.leaveDecision$.next(false);
  }

  // bug reportado 2026-09-23: esta pantalla cargaba datos mockeados ("María Belén
  // González" hardcodeada) en vez de la ficha real del backend.
  private loadRecord(id: string): void {

    this.loading.set(true);

    this.errorMessage.set(null);

    this.socialRecordsService
      .getById(id)
      .subscribe({

        next: (record) => {

          this.record.set(record);

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


  updateRecord(data: CreateSocialRecordRequest): void {
    this.errorMessage.set(null);
    this.saving.set(true);

    // bug reportado 2026-09-23: el backend devuelve 204 No Content, no la ficha
    // actualizada — antes se esperaba un body que nunca llegaba.
    this.socialRecordsService
      .update(this.recordId, data)
      .subscribe({
        next: () => {
          this.saving.set(false);

          // bug reportado 2026-09-29: el formulario seguía "dirty" después de
          // guardar (nunca se le avisaba que el guardado había sido exitoso),
          // así que pendingChangesGuard mostraba el confirm() nativo tanto al
          // redirigir automáticamente como después, al tocar "Cancelar" — aun
          // cuando los cambios ya estaban guardados en el backend.
          this.formComponent()?.markAsSaved();

          this.showSuccessModal.set(true);
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

  // Se llama al cerrar app-success-modal (botón "Aceptar"): recién ahí
  // redirigimos al detalle, así la persona ve el aviso a su propio ritmo
  // en vez de que se le vaya la pantalla sola a los 1.5 segundos.
  closeSuccessModal(): void {
    this.showSuccessModal.set(false);
    this.router.navigate(['/dashboard/fichas', this.recordId]);
  }

}