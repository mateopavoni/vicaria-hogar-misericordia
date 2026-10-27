/**
 * SCRUM-275 (front de SCRUM-70): campos de la ficha que alimentan el informe de Cáritas.
 * Son opcionales, pero el sistema marca cuáles están pendientes.
 */
export type ReportFieldKey = 'gender' | 'housing' | 'origin' | 'document' | 'mainReason';

export const REPORT_FIELD_KEYS: readonly ReportFieldKey[] = ['gender', 'housing', 'origin', 'document', 'mainReason'];

export const REPORT_FIELD_LABELS: Record<ReportFieldKey, string> = {
  gender: 'Género',
  housing: 'Situación habitacional',
  origin: 'Localidad de origen',
  document: 'Documento',
  mainReason: 'Motivo principal de asistencia',
};

export const REPORT_FIELD_HINTS: Record<ReportFieldKey, string> = {
  gender: 'Se informa como porcentaje de género.',
  housing: 'Para contar personas en situación de calle vs. con domicilio.',
  origin: 'De dónde es la persona (distribución geográfica).',
  document: 'Para contar personas con DNI vs. sin DNI.',
  mainReason: 'Desglose por motivo de asistencia.',
};

/** Un valor del catálogo de un campo. Se desactiva en vez de borrarse: las fichas viejas lo conservan. */
export interface ReportFieldValue {
  id: string;
  label: string;
  active: boolean;
}

/** Valor elegido (id del catálogo) de cada campo para una persona; null/ausente = pendiente. */
export type PersonReportData = Partial<Record<ReportFieldKey, string | null>>;

export type CatalogActionResult = { ok: true; value: ReportFieldValue } | { ok: false; message: string };
