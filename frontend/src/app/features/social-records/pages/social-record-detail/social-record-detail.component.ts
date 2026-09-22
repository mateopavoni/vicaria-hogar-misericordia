import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { SocialRecordDetail, PersonType } from '../../interfaces/social-record.interface';
import { SocialRecordsService } from '../../services/social-records.service';
import { EmptyFieldBadgeComponent } from '../../../../shared/components/empty-field-badge/empty-field-badge.component';
import { ChangeHistoryComponent } from '../../components/change-history/change-history.component';
import { StaysTimelineComponent } from '../../components/stays-timeline/stays-timeline.component';
import { ExitStayModalComponent } from '../../components/exit-stay-modal/exit-stay-modal.component';
import { RegisterEntryStayModalComponent, RegisterEntrySubmitData } from '../../components/register-entry-stay-modal/register-entry-stay-modal.component';
import { SuccessModalComponent } from '../../../../shared/components/success-modal/success-modal.component';
import { LifeHistoryComponent } from '../../components/life-history/life-history.component';
import { ObservationsComponent } from '../../components/observations/observations.component';
import { PermissionService } from '../../../../core/auth/permission.service';
import { RegisterExitRequest } from '../../interfaces/exit-stay.interface';
import { EXIT_REASON_TO_BACKEND } from '../../interfaces/stay.interface';
import { LifeHistory, LifeHistoryStage } from '../../interfaces/life-history.interface';
import { LifeStoryService } from '../../services/life-story.service';
import { Observation } from '../../interfaces/observation.interface';
import { ObservationsService } from '../../services/observations.service';

@Component({
  selector: 'app-social-record-detail',
  imports: [
    RouterLink,
    DatePipe,
    EmptyFieldBadgeComponent,
    ChangeHistoryComponent,
    StaysTimelineComponent,
    ExitStayModalComponent,
    RegisterEntryStayModalComponent,
    SuccessModalComponent,
    LifeHistoryComponent,
    ObservationsComponent,
  ],
  templateUrl: './social-record-detail.component.html',
  styleUrl: './social-record-detail.component.css',
})
export class SocialRecordDetailComponent implements OnInit {

  public permissionService = inject(PermissionService);
  readonly PersonType = PersonType;

  private route = inject(ActivatedRoute);
  private socialRecordsService = inject(SocialRecordsService);
  private lifeStoryService = inject(LifeStoryService);
  private observationsService = inject(ObservationsService);

  record = signal<SocialRecordDetail | null>(null);
  loading = signal(true);
  errorMessage = signal<string | null>(null);
  activeTab = signal('datos');

  lifeHistory = signal<LifeHistory | null>(null);
  initialObservations = signal<Observation[]>([]);

  showExitModal = signal(false);
  showEntryModal = signal(false);
  showSuccessModal = signal(false);
  successModalTitle = signal('¡Registro Exitoso!');
  successModalMessage = signal('');

  openExitModal(): void {
    this.showExitModal.set(true);
  }

  openEntryModal(): void {
    this.showEntryModal.set(true);
  }

  // marca a la persona como Residente; el backend crea la estadia en la Casona automaticamente (SCRUM-134).
  // la fecha que carga el modal no se usa: el backend siempre registra el ingreso "ahora" (SCRUM-141)
  handleEntryConfirm(_data?: RegisterEntrySubmitData): void {
    const personId = this.record()?.personId;
    if (!personId) {
      return;
    }

    this.socialRecordsService.updatePersonType(personId, PersonType.Resident).subscribe({
      next: () => {
        this.showEntryModal.set(false);
        this.successModalTitle.set('¡Ingreso Registrado!');
        this.successModalMessage.set('El ingreso a la Casona y el estado de Residente se registraron correctamente.');
        this.showSuccessModal.set(true);
        this.loadRecord(this.record()!.id);
      },
      error: (err) => {
        this.errorMessage.set(err?.error?.message || 'No se pudo registrar el ingreso a la Casona.');
      }
    });
  }

  // maneja la confirmación proveniente del modal de egreso
  handleExitConfirm(data: RegisterExitRequest): void {
    const person = this.record();
    const activeStay = person?.staysHistory?.find(s => !s.exitDate);
    if (!activeStay?.id) {
      this.errorMessage.set('No se encontró una estadía activa para registrar el egreso.');
      return;
    }

    this.socialRecordsService.registerExit(activeStay.id, {
      exitReason: EXIT_REASON_TO_BACKEND[data.exitReason],
      reason: data.exitDetail,
    }).subscribe({
      next: () => {
        this.showExitModal.set(false);
        this.successModalTitle.set('¡Egreso Registrado!');
        this.successModalMessage.set('El egreso de la Casona se registró correctamente en la línea de tiempo.');
        this.showSuccessModal.set(true);
        this.loadRecord(person!.id);
      },
      error: (err) => {
        this.errorMessage.set(err?.error?.message || 'Error al registrar el egreso.');
      }
    });
  }

  // guarda una etapa de la historia de vida (SCRUM-11/171)
  handleLifeHistorySaved(event: { stage: LifeHistoryStage; text: string }): void {
    const personId = this.record()?.personId;
    if (!personId) {
      return;
    }

    this.lifeStoryService.updateStage(personId, event.stage, event.text).subscribe({
      next: (updated) => this.lifeHistory.set(updated),
      error: (err) => {
        this.errorMessage.set(err?.error?.message || 'No se pudo guardar la historia de vida.');
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
        this.loadLifeHistory(response.personId);
        this.loadObservations(response.personId);
      },
      error: () => {
        this.loading.set(false);
        this.errorMessage.set('No se pudo cargar la ficha.');
      }
    });
  }

  private loadObservations(personId: string): void {
    this.observationsService.getByPersonId(personId).subscribe({
      next: (observations) => this.initialObservations.set(observations),
      error: () => this.initialObservations.set([])
    });
  }

  private loadLifeHistory(personId: string): void {
    this.lifeStoryService.getByPersonId(personId).subscribe({
      next: (history) => this.lifeHistory.set(history),
      // la historia de vida no bloquea el resto del perfil si falla
      error: () => this.lifeHistory.set(null)
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
