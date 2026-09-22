import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe, TitleCasePipe } from '@angular/common';
import { SocialRecordDetail, PersonType } from '../../interfaces/social-record.interface';
import { SocialRecordsService } from '../../services/social-records.service';
import { EmptyFieldBadgeComponent } from '../../../../shared/components/empty-field-badge/empty-field-badge.component';
import { ChangeHistoryComponent } from '../../components/change-history/change-history.component';
import { StaysTimelineComponent } from '../../components/stays-timeline/stays-timeline.component';
import { PermissionService } from '../../../../core/auth/permission.service';
import { RegisterExitRequest } from '../../interfaces/exit-stay.interface';
import { LifeHistoryComponent } from '../../components/life-history/life-history.component';
import { LifeHistory } from '../../interfaces/life-history.interface';
import { ExitStayModalComponent } from '../../components/exit-stay-modal/exit-stay-modal.component';
import { RegisterEntrySubmitData, RegisterEntryStayModalComponent } from '../../components/register-entry-stay-modal/register-entry-stay-modal.component';
import { SuccessModalComponent } from '../../../../shared/components/success-modal/success-modal.component';
import { ObservationsComponent } from '../../components/observations/observations.component';
import { Observation } from '../../interfaces/observation.interface';
@Component({
  selector: 'app-social-record-detail',
  imports: [
    RouterLink,
    DatePipe,
    TitleCasePipe,
    EmptyFieldBadgeComponent,
    ChangeHistoryComponent,
    StaysTimelineComponent,
    LifeHistoryComponent,
    ExitStayModalComponent,
    RegisterEntryStayModalComponent,
    SuccessModalComponent,
    ObservationsComponent
],
  templateUrl: './social-record-detail.component.html',
  styleUrl: './social-record-detail.component.css',
})
export class SocialRecordDetailComponent implements OnInit {
  public permissionService = inject(PermissionService);

  private route = inject(ActivatedRoute);
  private socialRecordsService = inject(SocialRecordsService);

  record = signal<SocialRecordDetail | null>(null);
  loading = signal(true);
  errorMessage = signal<string | null>(null);

  readonly PersonType = PersonType;
  activeTab = signal('datos');
  lifeHistory = signal<LifeHistory | null>(null);

  showExitModal = signal(false);
  showEntryModal = signal(false);

  showSuccessModal = signal(false);
  successModalTitle = signal('¡Registro Exitoso!');
  successModalMessage = signal('');

  openExitModal(): void {
    this.showExitModal.set(true);
  }

  handleExitConfirm(data: RegisterExitRequest): void {
    const recordId = this.record()?.id;
    if (!recordId) return;

    this.socialRecordsService.registerExit(recordId, data).subscribe({
      next: (updatedRecord) => {
        this.record.set(updatedRecord);
        this.showExitModal.set(false);
        this.successModalTitle.set('¡Egreso Registrado!');
        this.successModalMessage.set('El egreso de la Casona se registró correctamente en la línea de tiempo.');
        this.showSuccessModal.set(true);
      },
      error: (err) => {
        this.errorMessage.set('Error al registrar el egreso.');
        console.error('Error al registrar egreso:', err);
      }
    });
  }

  openEntryModal(): void {
    this.showEntryModal.set(true);
  }

  handleEntryConfirm(data: RegisterEntrySubmitData): void {
    const recordId = this.record()?.id;
    if (!recordId) return;

    this.socialRecordsService.registerEntry(recordId, data.entryDate).subscribe({
      next: (updatedRecord) => {
        this.record.set(updatedRecord);
        this.showEntryModal.set(false);
        this.successModalMessage.set('El ingreso a la Casona y el estado de Residente se registraron correctamente.');
        this.showSuccessModal.set(true);
      },
      error: (err:unknown) => {
        this.errorMessage.set('Error al registrar el ingreso.');
        console.error(err);
      }
    });
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');

    if (!id) {
      this.errorMessage.set('No se encontró la ficha.');
      this.loading.set(false);
      return;
    }

    this.loadRecord(id);
  }

  loadRecord(id: string): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.socialRecordsService.getById(id).subscribe({
      next: (response) => {
        this.record.set(response);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.errorMessage.set('No se pudo cargar la ficha.');
      }
    });
  }

  selectTab(tab: string): void {
    this.activeTab.set(tab);
  }

  getPersonTypeLabel(type: PersonType | null): string {
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