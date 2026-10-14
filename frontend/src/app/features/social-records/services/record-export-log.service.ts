import { Injectable } from '@angular/core';
import { ExportSections } from '../utils/record-export.util';

export interface RecordExportLogEntry {
  personId: string;
  sections: ExportSections;
  format: 'pdf' | 'print';
  exportedById: string;
  exportedByName: string;
  /** ISO */
  exportedAt: string;
}

/**
 * SCRUM-253: registro de quién exportó una ficha y cuándo.
 *
 * OJO — backend pendiente: no existe un endpoint de auditoría de exportaciones. Hasta entonces el
 * registro queda solo en memoria (se pierde al recargar). Cuando exista, `log` debe pasar a hacer
 * el POST correspondiente; el componente que lo llama no cambia.
 */
@Injectable({ providedIn: 'root' })
export class RecordExportLogService {
  private readonly entries: RecordExportLogEntry[] = [];

  log(entry: Omit<RecordExportLogEntry, 'exportedAt'>): void {
    this.entries.push({ ...entry, exportedAt: new Date().toISOString() });
  }

  getAll(): RecordExportLogEntry[] {
    return this.entries.map((e) => ({ ...e }));
  }
}
