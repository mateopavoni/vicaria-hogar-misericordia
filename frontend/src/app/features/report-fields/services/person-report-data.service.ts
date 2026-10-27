import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, map } from 'rxjs';
import { PersonReportData, ReportFieldKey } from '../interfaces/report-fields.interface';
import { cleanReportData } from '../utils/report-fields.util';

/** SocialRecordReportFieldsDto del backend: un id del catálogo por campo, null = pendiente. */
export interface ReportFieldsDto {
  genderId: string | null;
  housingSituationId: string | null;
  originLocalityId: string | null;
  documentationStatusId: string | null;
  attendanceReasonId: string | null;
}

const DTO_FIELD: Record<ReportFieldKey, keyof ReportFieldsDto> = {
  gender: 'genderId',
  housing: 'housingSituationId',
  origin: 'originLocalityId',
  document: 'documentationStatusId',
  mainReason: 'attendanceReasonId',
};

/**
 * SCRUM-275: valores de los campos del informe de cada persona (clave: personId). Se leen del
 * detalle de la ficha (`reportFields`) y se guardan con PUT /api/social-records/{id}/report-fields.
 */
@Injectable({ providedIn: 'root' })
export class PersonReportDataService {
  private readonly http = inject(HttpClient);
  private readonly state = signal<Record<string, PersonReportData>>({});

  /** Se lee en un computed/template: reacciona cuando se guarda. */
  get(personId: string): PersonReportData {
    return this.state()[personId] ?? {};
  }

  /** Lo que trae el detalle de la ficha. */
  setFromRecord(personId: string, fields: ReportFieldsDto | null | undefined): void {
    const data: PersonReportData = {};
    for (const [key, field] of Object.entries(DTO_FIELD) as [ReportFieldKey, keyof ReportFieldsDto][]) {
      data[key] = fields?.[field] ?? null;
    }
    this.state.update((all) => ({ ...all, [personId]: cleanReportData(data) }));
  }

  save(socialRecordId: string, personId: string, data: PersonReportData): Observable<PersonReportData> {
    const clean = cleanReportData(data);
    const body = Object.fromEntries(
      (Object.entries(DTO_FIELD) as [ReportFieldKey, keyof ReportFieldsDto][]).map(([key, field]) => [field, clean[key] ?? null]),
    );
    return this.http.put<void>(`/api/social-records/${socialRecordId}/report-fields`, body).pipe(
      map(() => {
        this.state.update((all) => ({ ...all, [personId]: clean }));
        return clean;
      }),
    );
  }
}
