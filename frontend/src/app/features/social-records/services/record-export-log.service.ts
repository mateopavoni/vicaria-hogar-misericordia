import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ExportSections } from '../utils/record-export.util';

export interface RecordExportLogEntry {
  sections: ExportSections;
  format: 'pdf' | 'print';
}

/**
 * SCRUM-23 / SCRUM-253: registro de quién exportó una ficha y cuándo. El PDF se arma en el
 * navegador; el backend deja la auditoría con el usuario del token y la fecha
 * (POST /api/social-records/{id}/exports).
 */
@Injectable({ providedIn: 'root' })
export class RecordExportLogService {
  private http = inject(HttpClient);

  log(socialRecordId: string, entry: RecordExportLogEntry): Observable<void> {
    return this.http.post<void>(`/api/social-records/${socialRecordId}/exports`, entry);
  }
}
