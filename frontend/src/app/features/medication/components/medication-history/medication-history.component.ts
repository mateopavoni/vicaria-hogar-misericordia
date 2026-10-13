import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { todayLocalIso } from '../../../../shared/utils/date.util';
import { HistoryStatus, MedicationHistoryEntry } from '../../interfaces/medication.interface';
import { MedicationService } from '../../services/medication.service';
import { filterHistory, summarizeHistory } from '../../utils/medication-history.util';

/**
 * SCRUM-242: historial de tomas de medicación de una persona (pestaña "Medicación" del
 * perfil). Tabla por fecha con filtro de rango y de medicamento; las no administradas se
 * resaltan. "Exportar PDF" usa la impresión del navegador ("Guardar como PDF"), mostrando
 * solo este bloque (ver estilos .print-target en styles.css). El que lo monta decide el
 * permiso (medicamentos.history: Referente y Directora).
 */
@Component({
  selector: 'app-medication-history',
  imports: [DatePipe],
  templateUrl: './medication-history.component.html',
})
export class MedicationHistoryComponent implements OnInit {
  private medicationService = inject(MedicationService);

  personId = input.required<string>();
  personName = input('');

  readonly today = todayLocalIso();

  entries = signal<MedicationHistoryEntry[]>([]);
  loading = signal(true);
  errorMessage = signal<string | null>(null);

  dateFrom = signal('');
  dateTo = signal('');
  medicationId = signal('');

  medications = computed(() => {
    const seen = new Map<string, string>();
    for (const e of this.entries()) seen.set(e.medicationId, e.medicationName);
    return [...seen.entries()].map(([id, name]) => ({ id, name }));
  });

  rangeInvalid = computed(() => !!this.dateFrom() && !!this.dateTo() && this.dateFrom() > this.dateTo());

  filtered = computed(() =>
    this.rangeInvalid()
      ? []
      : filterHistory(this.entries(), {
          from: this.dateFrom(),
          to: this.dateTo(),
          medicationId: this.medicationId(),
        }),
  );
  summary = computed(() => summarizeHistory(this.filtered()));

  filterLabel = computed(() => {
    const parts: string[] = [];
    if (this.dateFrom() || this.dateTo()) {
      parts.push(`Período: ${this.fmt(this.dateFrom()) || 'inicio'} al ${this.fmt(this.dateTo()) || 'hoy'}`);
    }
    const med = this.medications().find((m) => m.id === this.medicationId());
    if (med) parts.push(`Medicamento: ${med.name}`);
    return parts.join(' · ') || 'Todo el historial';
  });

  ngOnInit(): void {
    this.medicationService.getPersonHistory(this.personId()).subscribe({
      next: (entries) => {
        this.entries.set(entries);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.errorMessage.set('No se pudo cargar el historial de medicación.');
      },
    });
  }

  statusLabel(status: HistoryStatus): string {
    return status === 'administered' ? 'Administrada' : status === 'not-administered' ? 'No administrada' : 'Sin registrar';
  }

  clearFilters(): void {
    this.dateFrom.set('');
    this.dateTo.set('');
    this.medicationId.set('');
  }

  /** Imprime solo el bloque del historial; el diálogo del navegador permite "Guardar como PDF". */
  exportPdf(): void {
    const body = document.body;
    const cleanup = () => {
      body.classList.remove('print-only-target');
      window.removeEventListener('afterprint', cleanup);
    };
    body.classList.add('print-only-target');
    window.addEventListener('afterprint', cleanup);
    setTimeout(() => window.print(), 50);
  }

  private fmt(iso: string): string {
    if (!iso) return '';
    const [y, m, d] = iso.split('-');
    return `${d}/${m}/${y}`;
  }
}
