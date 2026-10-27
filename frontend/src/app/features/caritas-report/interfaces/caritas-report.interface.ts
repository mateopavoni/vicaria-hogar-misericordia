/** SCRUM-271 (front de SCRUM-69): informe estadístico para Cáritas. */
export type ReportPeriodType = 'trimestral' | 'semestral';

export const PERIOD_MONTHS: Record<ReportPeriodType, number> = { trimestral: 3, semestral: 6 };

export const PERIOD_LABELS: Record<ReportPeriodType, string> = {
  trimestral: 'Trimestral (3 meses)',
  semestral: 'Semestral (6 meses)',
};

export interface ReportRange {
  /** YYYY-MM-DD, inclusive */
  from: string;
  /** YYYY-MM-DD, inclusive */
  to: string;
}

export interface CountItem {
  label: string;
  count: number;
}

export interface CaritasReport {
  periodType: ReportPeriodType;
  from: string;
  to: string;
  /** true mientras los números son de ejemplo (backend sin conectar): se avisa en pantalla, PDF e impresión. */
  isSample: boolean;
  totals: {
    /** Personas atendidas en el período */
    attendedPersons: number;
    /** Promedio de asistencias por persona */
    averageAttendancesPerPerson: number;
    /** Nuevas altas en el período */
    newRegistrations: number;
  };
  /**
   * Cada categoría trae los valores del catálogo configurable (SCRUM-70) más
   * "No informado", así que suma el total de personas atendidas.
   */
  gender: CountItem[];
  housing: CountItem[];
  dni: CountItem[];
  /** De dónde son (distribución geográfica): solo las localidades con personas */
  geography: CountItem[];
  reasons: CountItem[];
}
