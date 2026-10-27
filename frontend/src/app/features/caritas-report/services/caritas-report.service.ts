import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { CaritasReport, ReportPeriodType } from '../interfaces/caritas-report.interface';

/**
 * SCRUM-271: informe estadístico para Cáritas contra la API (SCRUM-270):
 * GET /api/caritas-reports?type=trimestral|semestral&to=YYYY-MM-DD, solo Referente.
 * El backend devuelve el mismo CaritasReport con números reales (isSample = false).
 */
@Injectable({ providedIn: 'root' })
export class CaritasReportService {
  private readonly http = inject(HttpClient);

  generate(type: ReportPeriodType, to: string): Observable<CaritasReport> {
    if (type !== 'trimestral' && type !== 'semestral') {
      return throwError(() => ({ error: { message: 'Elegí un período trimestral o semestral.' } }));
    }
    if (!/^\d{4}-\d{2}-\d{2}$/.test(to)) {
      return throwError(() => ({ error: { message: 'La fecha de corte no es válida.' } }));
    }
    const params = new HttpParams().set('type', type).set('to', to);
    return this.http.get<CaritasReport>('/api/caritas-reports', { params });
  }
}
