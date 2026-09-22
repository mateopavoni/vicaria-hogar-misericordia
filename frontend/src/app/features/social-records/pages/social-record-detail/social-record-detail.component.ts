import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe, TitleCasePipe } from '@angular/common';
import { SocialRecordDetail, PersonType } from '../../interfaces/social-record.interface';
import { SocialRecordsService } from '../../services/social-records.service';
import { EmptyFieldBadgeComponent } from '../../../../shared/components/empty-field-badge/empty-field-badge.component';
import { ChangeHistoryComponent } from '../../components/change-history/change-history.component';
import { StaysTimelineComponent } from '../../components/stays-timeline/stays-timeline.component';
import { ExitStayModalComponent } from '../../components/exit-stay-modal/exit-stay-modal.component';
import { PermissionService } from '../../../../core/auth/permission.service';
import { RegisterExitRequest } from '../../interfaces/exit-stay.interface';
import { EXIT_REASON_TO_BACKEND } from '../../interfaces/stay.interface';

@Component({
  selector: 'app-social-record-detail.component',
  imports: [RouterLink, DatePipe, TitleCasePipe, EmptyFieldBadgeComponent, ChangeHistoryComponent, StaysTimelineComponent, ExitStayModalComponent],
  templateUrl: './social-record-detail.component.html',
  styleUrl: './social-record-detail.component.css',
})
export class SocialRecordDetailComponent implements OnInit {

  public permissionService = inject(PermissionService);
  readonly PersonType = PersonType;

  private route = inject(ActivatedRoute);
  private socialRecordsService = inject(SocialRecordsService);

  record = signal<SocialRecordDetail | null>(null);
  loading = signal(true);
  errorMessage = signal<string | null>(null);
  activeTab = signal('datos');
  showExitModal = signal(false);

  openExitModal(): void {
    this.showExitModal.set(true);
  }

  // marca a la persona como Residente; el backend crea la estadia en la Casona automaticamente (SCRUM-134)
  registerEntry(): void {
    const personId = this.record()?.personId;
    if (!personId) {
      return;
    }

    this.socialRecordsService.updatePersonType(personId, PersonType.Resident).subscribe({
      next: () => this.loadRecord(this.record()!.id),
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
        this.loadRecord(person!.id);
      },
      error: (err) => {
        this.errorMessage.set(err?.error?.message || 'Error al registrar el egreso.');
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
