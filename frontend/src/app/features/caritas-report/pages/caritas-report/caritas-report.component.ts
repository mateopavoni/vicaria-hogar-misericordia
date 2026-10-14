import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { AuthService } from '../../../../core/auth/auth.service';
import { PermissionService } from '../../../../core/auth/permission.service';
import { extractApiError } from '../../../../core/http/api-error';
import { UiButtonComponent, UiFormFieldComponent, UiInputDirective } from '../../../../shared/ui';
import { todayLocalIso } from '../../../../shared/utils/date.util';
import { RecordExportPrintComponent } from '../../../social-records/components/record-export-print/record-export-print.component';
import { RecordExportDocument } from '../../../social-records/utils/record-export.util';
import { buildPdf } from '../../../social-records/utils/simple-pdf.util';
import { CaritasReport, PERIOD_LABELS, ReportPeriodType } from '../../interfaces/caritas-report.interface';
import { CaritasReportService } from '../../services/caritas-report.service';
import {
  SAMPLE_WARNING,
  buildCaritasDocument,
  caritasFilename,
  formatDecimal,
  periodRange,
  reportCategories,
} from '../../utils/caritas-report.util';

/**
 * SCRUM-271: pantalla de generación del informe estadístico para Cáritas
 * (/dashboard/informes/caritas). SMART: selector de período, vista con tablas y barras por
 * categoría, exportar a PDF e imprimir. Solo Referente (permiso informes.caritas).
 */
@Component({
  selector: 'app-caritas-report',
  imports: [DatePipe, UiButtonComponent, UiFormFieldComponent, UiInputDirective, RecordExportPrintComponent],
  templateUrl: './caritas-report.component.html',
})
export class CaritasReportComponent {
  private reportService = inject(CaritasReportService);
  private authService = inject(AuthService);
  private permissionService = inject(PermissionService);

  readonly periodTypes = Object.keys(PERIOD_LABELS) as ReportPeriodType[];
  readonly periodLabels = PERIOD_LABELS;
  readonly today = todayLocalIso();
  readonly sampleWarning = SAMPLE_WARNING;
  readonly formatDecimal = formatDecimal;

  periodType = signal<ReportPeriodType>('trimestral');
  dateTo = signal(todayLocalIso());

  report = signal<CaritasReport | null>(null);
  loading = signal(false);
  errorMessage = signal<string | null>(null);
  printDoc = signal<RecordExportDocument | null>(null);

  categories = computed(() => {
    const r = this.report();
    return r ? reportCategories(r) : [];
  });

  rangePreview = computed(() => {
    const to = this.dateTo();
    return to && to <= this.today ? periodRange(this.periodType(), to) : null;
  });

  get dateError(): string | null {
    const to = this.dateTo();
    if (!to) return 'La fecha de corte es obligatoria.';
    return to > this.today ? 'La fecha de corte no puede ser futura.' : null;
  }

  generate(): void {
    if (!this.permissionService.hasPermission('informes.caritas') || this.loading() || this.dateError) {
      return;
    }
    this.loading.set(true);
    this.errorMessage.set(null);
    this.reportService.generate(this.periodType(), this.dateTo()).subscribe({
      next: (report) => {
        this.report.set(report);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(extractApiError(err, 'No se pudo generar el informe.'));
      },
    });
  }

  private buildDoc(report: CaritasReport): RecordExportDocument | null {
    const user = this.authService.user();
    if (!user || !this.permissionService.hasPermission('informes.caritas')) {
      return null;
    }
    return buildCaritasDocument(report, new Date(), `${user.name} ${user.lastname}`.trim());
  }

  exportPdf(): void {
    const report = this.report();
    const doc = report && this.buildDoc(report);
    if (!report || !doc) {
      return;
    }
    const bytes = buildPdf(doc);
    const url = URL.createObjectURL(new Blob([bytes.buffer as ArrayBuffer], { type: 'application/pdf' }));
    const link = document.createElement('a');
    link.href = url;
    link.download = caritasFilename(report);
    document.body.appendChild(link);
    link.click();
    link.remove();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }

  print(): void {
    const report = this.report();
    const doc = report && this.buildDoc(report);
    if (!doc) {
      return;
    }
    this.printDoc.set(doc);
    const body = document.body;
    const cleanup = () => {
      body.classList.remove('print-only-target');
      this.printDoc.set(null);
      window.removeEventListener('afterprint', cleanup);
    };
    body.classList.add('print-only-target');
    window.addEventListener('afterprint', cleanup);
    // esperar a que Angular pinte la vista de impresión antes de abrir el diálogo
    setTimeout(() => window.print(), 100);
  }
}
