import {Component, inject, OnInit,signal} from '@angular/core';
import { ActivatedRoute,  Router} from '@angular/router';
import {  SocialRecordsService} from '../../services/social-records.service';
import { SocialRecordDetail, CreateSocialRecordRequest} from '../../interfaces/social-record.interface';
import { SocialRecordFormComponent} from '../../components/social-record-form/social-record-form.component';


@Component({
  selector: 'app-social-record-edit',
  imports: [ SocialRecordFormComponent ],
  templateUrl: './social-record-edit.component.html',
  styleUrl: './social-record-edit.component.css'
})
export class SocialRecordEditComponent
  implements OnInit {

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


  updateRecord(
    data: CreateSocialRecordRequest
  ): void {

    // Lo implementamos cuando
    // tengamos confirmado el endpoint
    // PUT/PATCH del backend.

    console.log(
      'Datos a actualizar:',
      this.recordId,
      data
    );

  }

}