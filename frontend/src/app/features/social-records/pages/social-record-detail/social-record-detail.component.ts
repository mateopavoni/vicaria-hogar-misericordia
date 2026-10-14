import { extractApiError } from '../../../../core/http/api-error';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { UiBackButtonComponent } from '../../../../shared/ui';
import { DatePipe } from '@angular/common';
import { PersonProfileStatus, PersonStatus, SocialRecordDetail, PersonType } from '../../interfaces/social-record.interface';
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
import { AttendanceHistoryComponent } from '../../../attendance/components/attendance-history/attendance-history.component';
import { AttendanceHistoryEntry } from '../../../attendance/interfaces/attendance.interface';
import { AttendanceService } from '../../../attendance/services/attendance.service';
import { computeAttendanceStats, isAbsenceAlert } from '../../../attendance/utils/attendance-stats.util';
import { todayLocalIso } from '../../../../shared/utils/date.util';
import { AuthService } from '../../../../core/auth/auth.service';
import { ExportRecordModalComponent } from '../../components/export-record-modal/export-record-modal.component';
import { RecordExportPrintComponent } from '../../components/record-export-print/record-export-print.component';
import { RecordExportLogService } from '../../services/record-export-log.service';
import { ExportSections, RecordExportDocument, buildExportFilename, buildRecordExport } from '../../utils/record-export.util';
import { buildPdf } from '../../utils/simple-pdf.util';
import { PersonAttachmentsComponent } from '../../../attachments/components/person-attachments/person-attachments.component';
import { PsychiatricEvaluationsComponent } from '../../../psychiatric-evaluations/components/psychiatric-evaluations/psychiatric-evaluations.component';
import { PsychiatricEvaluationsService } from '../../../psychiatric-evaluations/services/psychiatric-evaluations.service';
import { daysUntilExpiry, evaluationValidity } from '../../../psychiatric-evaluations/utils/evaluation-validity.util';
import { ReportFieldsModalComponent } from '../../../report-fields/components/report-fields-modal/report-fields-modal.component';
import { PersonReportData, REPORT_FIELD_KEYS, REPORT_FIELD_LABELS } from '../../../report-fields/interfaces/report-fields.interface';
import { PersonReportDataService } from '../../../report-fields/services/person-report-data.service';
import { ReportFieldCatalogService } from '../../../report-fields/services/report-field-catalog.service';
import { pendingReportFields } from '../../../report-fields/utils/report-fields.util';
import { StatusChangeModalComponent } from '../../components/status-change-modal/status-change-modal.component';
import { StatusChangeLogService, mergeStatusHistory } from '../../services/status-change-log.service';
import { MedicationSchemesComponent } from '../../../medication/components/medication-schemes/medication-schemes.component';

@Component({
  selector: 'app-social-record-detail',
  imports: [
    RouterLink,
    UiBackButtonComponent,
    DatePipe,
    EmptyFieldBadgeComponent,
    ChangeHistoryComponent,
    StaysTimelineComponent,
    ExitStayModalComponent,
    RegisterEntryStayModalComponent,
    SuccessModalComponent,
    LifeHistoryComponent,
    ObservationsComponent,
    AttendanceHistoryComponent,
    MedicationSchemesComponent,
    ExportRecordModalComponent,
    PersonAttachmentsComponent,
    PsychiatricEvaluationsComponent,
    RecordExportPrintComponent,
    StatusChangeModalComponent,
    ReportFieldsModalComponent,
  ],
  templateUrl: './social-record-detail.component.html',
  styleUrl: './social-record-detail.component.css',
})
export class SocialRecordDetailComponent implements OnInit {

  public permissionService = inject(PermissionService);
  readonly PersonType = PersonType;
  readonly PersonStatus = PersonStatus;

  private route = inject(ActivatedRoute);
  private socialRecordsService = inject(SocialRecordsService);
  private lifeStoryService = inject(LifeStoryService);
  private observationsService = inject(ObservationsService);
  private attendanceService = inject(AttendanceService);
  private authService = inject(AuthService);
  private exportLog = inject(RecordExportLogService);
  private evaluationsService = inject(PsychiatricEvaluationsService);

  record = signal<SocialRecordDetail | null>(null);
  loading = signal(true);
  errorMessage = signal<string | null>(null);
  activeTab = signal('datos');

  lifeHistory = signal<LifeHistory | null>(null);
  initialObservations = signal<Observation[]>([]);

  // SCRUM-222: historial de asistencia (solo se pide con permiso asistencia.view)
  attendanceHistory = signal<AttendanceHistoryEntry[]>([]);
  attendanceStats = computed(() => computeAttendanceStats(this.attendanceHistory(), todayLocalIso()));
  hasAbsenceAlert = computed(() => isAbsenceAlert(this.attendanceStats()));

  // SCRUM-260: evaluación psiquiátrica vigente y su vigencia (vence a los 6 meses)
  currentEvaluation = computed(() => {
    const personId = this.record()?.personId;
    return personId ? this.evaluationsService.currentFor(personId) : null;
  });
  evaluationValidity = computed(() => {
    const evaluation = this.currentEvaluation();
    return evaluation ? evaluationValidity(evaluation.date, todayLocalIso()) : null;
  });
  evaluationDaysLeft = computed(() => {
    const evaluation = this.currentEvaluation();
    return evaluation ? daysUntilExpiry(evaluation.date, todayLocalIso()) : 0;
  });
  // evaluación vencida: se bloquea el ingreso a la Casa de Convivencia
  entryBlocked = computed(() => this.evaluationValidity() === 'expired');

  showExitModal = signal(false);
  showEntryModal = signal(false);
  showSuccessModal = signal(false);
  successModalTitle = signal('¡Registro Exitoso!');
  successModalMessage = signal('');

  // bug reportado 2026-09-23: el botón de ingreso/egreso se gateaba solo por personType,
  // que puede desincronizarse de si hay una estadía realmente abierta. Ahora depende de si
  // existe una estadía sin egreso, que es la condición que de verdad importa para el botón.
  hasActiveStay = computed(() => (this.record()?.staysHistory ?? []).some(stay => !stay.exitDate));

  openExitModal(): void {
    this.showExitModal.set(true);
  }

  openEntryModal(): void {
    if (this.entryBlocked()) {
      return;
    }
    this.showEntryModal.set(true);
  }

  // marca a la persona como Residente; el backend crea la estadía en la Casa de Convivencia automaticamente (SCRUM-134).
  // la fecha que carga el modal no se usa: el backend siempre registra el ingreso "ahora" (SCRUM-141)
  handleEntryConfirm(_data?: RegisterEntrySubmitData): void {
    const personId = this.record()?.personId;
    if (!personId || this.entryBlocked()) {
      return;
    }

    this.socialRecordsService.updatePersonType(personId, PersonType.Resident).subscribe({
      next: () => {
        this.showEntryModal.set(false);
        this.successModalTitle.set('¡Ingreso Registrado!');
        this.successModalMessage.set('El ingreso a la Casa de Convivencia y el estado de Residente se registraron correctamente.');
        this.showSuccessModal.set(true);
        this.loadRecord(this.record()!.id);
      },
      error: (err) => {
        this.errorMessage.set(err?.error?.message || 'No se pudo registrar el ingreso a la Casa de Convivencia.');
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
        this.successModalMessage.set('El egreso de la Casa de Convivencia se registró correctamente en la línea de tiempo.');
        this.showSuccessModal.set(true);
        this.loadRecord(person!.id);
      },
      error: (err) => {
        this.errorMessage.set(err?.error?.message || 'Error al registrar el egreso.');
      }
    });
  }

  // SCRUM-275: datos de la ficha que alimentan el informe de Cáritas, con indicador de los pendientes
  private reportDataService = inject(PersonReportDataService);
  private reportCatalog = inject(ReportFieldCatalogService);
  readonly reportFieldKeys = REPORT_FIELD_KEYS;
  readonly reportFieldLabels = REPORT_FIELD_LABELS;
  showReportFieldsModal = signal(false);

  reportData = computed(() => {
    const person = this.record();
    return person ? this.reportDataService.get(person.personId) : {};
  });
  pendingReportFields = computed(() => pendingReportFields(this.reportData()));

  reportValueLabel(key: (typeof REPORT_FIELD_KEYS)[number]): string | null {
    return this.reportCatalog.label(key, this.reportData()[key]);
  }

  saveReportFields(data: PersonReportData): void {
    const person = this.record();
    if (!person || !this.permissionService.hasPermission('fichas.edit')) {
      return;
    }
    this.reportDataService.save(person.personId, data);
    this.showReportFieldsModal.set(false);
  }

  // SCRUM-264: cambio manual de estado (ambulatorios): pide un motivo obligatorio y lo deja en el historial
  private statusLog = inject(StatusChangeLogService);
  showStatusModal = signal(false);
  statusSaving = signal(false);
  statusError = signal<string | null>(null);

  statusTargetLabel = computed(() =>
    this.record()?.status === PersonStatus.Active ? 'Inactivo' : 'Activo');

  // historial de estados: el que venga del backend + los cambios manuales hechos en esta sesión
  statusHistory = computed(() => {
    const person = this.record();
    return person ? mergeStatusHistory(person.statusHistory, this.statusLog.forPerson(person.personId)) : [];
  });

  openStatusModal(): void {
    this.statusError.set(null);
    this.showStatusModal.set(true);
  }

  confirmStatusChange(reason: string): void {
    const person = this.record();
    const user = this.authService.user();
    if (!person || !user || this.statusSaving()) {
      return;
    }

    const previous = person.status;
    const newStatus = previous === PersonStatus.Active
      ? PersonProfileStatus.InactiveAmbulatory
      : PersonProfileStatus.ActiveAmbulatory;

    this.statusSaving.set(true);
    this.socialRecordsService.updateProfileStatus(person.personId, newStatus, reason).subscribe({
      next: () => {
        this.statusLog.record(
          person.personId,
          previous,
          previous === PersonStatus.Active ? PersonStatus.Inactive : PersonStatus.Active,
          `${user.name} ${user.lastname}`.trim(),
          reason,
        );
        this.statusSaving.set(false);
        this.showStatusModal.set(false);
        this.loadRecord(person.id);
      },
      error: (err) => {
        this.statusSaving.set(false);
        this.statusError.set(err?.error?.message || 'No se pudo cambiar el estado.');
      }
    });
  }

  // agrega una entrada nueva a una etapa de la historia de vida (SCRUM-11/171).
  // Bug reportado 2026-09-23: faltaba el modal de éxito acá (sí existía para
  // ingreso/egreso) — se reusa el mismo patrón de signals.
  // error al guardar la historia de vida: se muestra como aviso, sin reemplazar la ficha entera
  lifeHistoryError = signal<string | null>(null);

  // SCRUM-253: "Exportar ficha" abre el selector de secciones; de ahí se descarga el PDF o se imprime
  showExportModal = signal(false);
  exportDoc = signal<RecordExportDocument | null>(null);
  exporting = signal(false);

  exportRecord(): void {
    if (!this.permissionService.hasPermission('fichas.export')) {
      return;
    }
    this.showExportModal.set(true);
  }

  // genera el PDF y lo descarga directo al dispositivo
  downloadExport(sections: ExportSections): void {
    this.prepareExport(sections, (doc, record, exportedAt) => {
      const bytes = buildPdf(doc);
      const url = URL.createObjectURL(new Blob([bytes.buffer as ArrayBuffer], { type: 'application/pdf' }));
      const link = document.createElement('a');
      link.href = url;
      link.download = buildExportFilename(record, exportedAt);
      document.body.appendChild(link);
      link.click();
      link.remove();
      setTimeout(() => URL.revokeObjectURL(url), 1000);
      this.logExport(record.personId, sections, 'pdf');
    });
  }

  // vista de impresión: se imprime solo la ficha armada (ver .print-only-target en styles.css)
  printExport(sections: ExportSections): void {
    this.prepareExport(sections, (doc, record) => {
      this.exportDoc.set(doc);
      this.logExport(record.personId, sections, 'print');

      const body = document.body;
      const cleanup = () => {
        body.classList.remove('print-only-target');
        this.exportDoc.set(null);
        window.removeEventListener('afterprint', cleanup);
      };
      body.classList.add('print-only-target');
      window.addEventListener('afterprint', cleanup);
      // esperar a que Angular pinte la vista de impresión antes de abrir el diálogo
      setTimeout(() => window.print(), 100);
    });
  }

  // las observaciones se piden de nuevo para incluir las que se cargaron en esta sesión
  private prepareExport(
    sections: ExportSections,
    run: (doc: RecordExportDocument, record: SocialRecordDetail, exportedAt: Date) => void,
  ): void {
    const record = this.record();
    const user = this.authService.user();
    if (!record || !user || this.exporting() || !this.permissionService.hasPermission('fichas.export')) {
      return;
    }

    const finish = (observations: Observation[]) => {
      const exportedAt = new Date();
      const doc = buildRecordExport({
        record,
        lifeHistory: this.lifeHistory(),
        observations,
        sections,
        exportedAt,
        exportedBy: `${user.name} ${user.lastname}`.trim(),
      });
      this.showExportModal.set(false);
      this.exporting.set(false);
      run(doc, record, exportedAt);
    };

    if (sections === 'datos') {
      finish([]);
      return;
    }

    this.exporting.set(true);
    this.observationsService.getByPersonId(record.personId).subscribe({
      next: (observations) => finish(observations),
      error: () => {
        this.exporting.set(false);
        this.errorMessage.set('No se pudieron cargar las observaciones para exportar la ficha.');
      },
    });
  }

  private logExport(personId: string, sections: ExportSections, format: 'pdf' | 'print'): void {
    const user = this.authService.user();
    if (!user) return;
    this.exportLog.log({
      personId,
      sections,
      format,
      exportedById: user.id,
      exportedByName: `${user.name} ${user.lastname}`.trim(),
    });
  }

  handleLifeHistorySaved(event: { stage: LifeHistoryStage; text: string }): void {
    const personId = this.record()?.personId;
    if (!personId) {
      return;
    }

    this.lifeHistoryError.set(null);

    this.lifeStoryService.updateStage(personId, event.stage, event.text).subscribe({
      next: (updated) => {
        this.lifeHistory.set(updated);
        this.successModalTitle.set('¡Entrada Registrada!');
        this.successModalMessage.set('La entrada de historia de vida se guardó correctamente.');
        this.showSuccessModal.set(true);
      },
      error: (err) => {
        this.lifeHistoryError.set(extractApiError(err, 'No se pudo guardar la historia de vida.'));
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
        this.loadAttendanceHistory(response.personId);
      },
      error: () => {
        this.loading.set(false);
        this.errorMessage.set('No se pudo cargar la ficha.');
      }
    });
  }

  private loadAttendanceHistory(personId: string): void {
    if (!this.permissionService.hasPermission('asistencia.view')) {
      this.attendanceHistory.set([]);
      return;
    }
    this.attendanceService.getPersonHistory(personId).subscribe({
      next: (entries) => this.attendanceHistory.set(entries),
      // el historial no bloquea el resto del perfil si falla
      error: () => this.attendanceHistory.set([])
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
