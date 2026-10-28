import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ExportSections } from '../utils/record-export.util';

export interface RecordExportLogEntry {
  sections: ExportSections;
  format: 'pdf' | 'print';
}

/**
 * SCRUM-23 / SCRUM-253: exportación de la ficha. El PDF lo genera el backend
 * (GET /api/persons/{id}/export-pdf, SCRUM-252), que también deja la auditoría. La vista de
 * impresión se arma en el navegador y se registra con POST /api/social-records/{id}/exports.
 */
@Injectable({ providedIn: 'root' })
export class RecordExportLogService {
  private http = inject(HttpClient);

  log(socialRecordId: string, entry: RecordExportLogEntry): Observable<void> {
    return this.http.post<void>(`/api/social-records/${socialRecordId}/exports`, entry);
  }

  downloadPdf(personId: string, sections: ExportSections): Observable<Blob> {
    return this.http.get(`/api/persons/${personId}/export-pdf`, { params: { sections }, responseType: 'blob' });
  }
}
