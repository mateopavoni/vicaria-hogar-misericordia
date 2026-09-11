import { Component, inject, signal } from '@angular/core';

import { SocialRecordsService } from '../../services/social-records.service';

import {
  CreateSocialRecordRequest
} from '../../interfaces/social-record.interface';

import {
  SocialRecordFormComponent
} from '../../components/social-record-form/social-record-form.component';

@Component({
  selector: 'app-new-social-record',
  imports: [
    SocialRecordFormComponent
  ],
  templateUrl: './new-social-record.component.html',
  styleUrl: './new-social-record.component.css'
})
export class NewSocialRecordComponent {

  private socialRecordsService =
    inject(SocialRecordsService);

  saving = signal(false);

  errorMessage = signal<string | null>(null);

  successMessage = signal<string | null>(null);


  createRecord(
    data: CreateSocialRecordRequest
  ): void {

    this.errorMessage.set(null);
    this.successMessage.set(null);
    this.saving.set(true);

    this.socialRecordsService
      .create(data)
      .subscribe({

        next: () => {

          this.saving.set(false);

          this.successMessage.set(
            'Ficha creada correctamente.'
          );

        },

        error: (err) => {

          this.saving.set(false);

          this.errorMessage.set(
            err?.error?.message ||
            'Ocurrió un error al guardar la ficha.'
          );

        }

      });

  }

}