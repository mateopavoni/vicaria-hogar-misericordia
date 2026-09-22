import { Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, RouterLink} from '@angular/router';
import { DatePipe , TitleCasePipe} from '@angular/common';
import { SocialRecordDetail, PersonType} from '../../interfaces/social-record.interface';
import { SocialRecordsService} from '../../services/social-records.service';
import { EmptyFieldBadgeComponent } from '../../../../shared/components/empty-field-badge/empty-field-badge.component';
import { ChangeHistoryItem } from '../../interfaces/change-history.interface';
import { ChangeHistoryComponent } from '../../components/change-history/change-history.component';
import { StaysTimelineComponent } from '../../components/stays-timeline/stays-timeline.component';
import { PermissionService } from '../../../../core/auth/permission.service';
import { RegisterExitRequest } from '../../interfaces/exit-stay.interface';


@Component({
  selector: 'app-social-record-detail.component',
  imports: [RouterLink, DatePipe, TitleCasePipe, EmptyFieldBadgeComponent, ChangeHistoryComponent, StaysTimelineComponent],
  templateUrl: './social-record-detail.component.html',
  styleUrl: './social-record-detail.component.css',
})
  export class SocialRecordDetailComponent


    implements OnInit {
      
      public permissionService = inject(PermissionService);
      
      
      private route = inject(ActivatedRoute);
      private socialRecordsService = inject(SocialRecordsService);
      
      record = signal<SocialRecordDetail | null>(null);
      
      loading = signal(true);
      
      errorMessage = signal<string | null>(null);
      
      activeTab = signal('datos');
      
      
      showExitModal = signal(false);
      [x: string]: any;
      openExitModal() {
      throw new Error('Method not implemented.');
      }
      registerEntry() {
      throw new Error('Method not implemented.');
      }

      /**
   * Maneja la confirmación proveniente del modal de egreso
   */
    handleExitConfirm(data: RegisterExitRequest): void {
        const recordId = this.record()?.id;
        if (!recordId) return;

        this.socialRecordsService.registerExit(recordId, data).subscribe({
          next: (updatedRecord) => {
            // Actualizamos el Signal con la ficha que retorna el backend
            this.record.set(updatedRecord);
            this.showExitModal.set(false);
          },
          error: (err) => {
            this.errorMessage.set('Error al registrar el egreso.');
            console.error('Error al registrar egreso:', err);
          }
        });
      }
    

    ngOnInit(): void {

      const id = this.route.snapshot.paramMap.get('id');

      if (!id) {
        this.errorMessage.set(
          'No se encontró la ficha.'
        );

        this.loading.set(false);

        return;
      }

      this.loadRecord(id);
    }

    loadRecord(id: string): void {

      this.loading.set(true);

      this.errorMessage.set(null);

      this.socialRecordsService
        .getById(id).subscribe({
          next: (response) => {
            this.record.set(response);
            this.loading.set(false);
          },

          error: () => {
            this.loading.set(false);
            this.errorMessage.set(
              'No se pudo cargar la ficha.'
            );
          }

        });
    }

    

    selectTab(tab: string): void {
      this.activeTab.set(tab);
    }

    getPersonTypeLabel(
      type: PersonType | null
    ): string {

      if (type === PersonType.Ambulatory) {
        return 'Ambulatorio';
      }

      if (type === PersonType.Resident) {
        return 'Residente';
      }

      return '—';
    }

    getFullName(): string {

      const person = this.record();

      if (!person) {
        return '';
      }

      return `${person.firstName} ${person.lastName ?? ''}`.trim();
    }
  }