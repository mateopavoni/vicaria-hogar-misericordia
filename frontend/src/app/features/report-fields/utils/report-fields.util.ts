import { PersonReportData, REPORT_FIELD_KEYS, ReportFieldKey } from '../interfaces/report-fields.interface';

/** Campos del informe que todavía no se cargaron para una persona. */
export function pendingReportFields(data: PersonReportData | null | undefined): ReportFieldKey[] {
  return REPORT_FIELD_KEYS.filter((key) => !data?.[key]);
}

export function hasPendingReportFields(data: PersonReportData | null | undefined): boolean {
  return pendingReportFields(data).length > 0;
}

/** Quita los campos vacíos ('' → null) para guardar siempre lo mismo. */
export function cleanReportData(data: PersonReportData): PersonReportData {
  const out: PersonReportData = {};
  for (const key of REPORT_FIELD_KEYS) {
    out[key] = data[key] ? data[key] : null;
  }
  return out;
}
