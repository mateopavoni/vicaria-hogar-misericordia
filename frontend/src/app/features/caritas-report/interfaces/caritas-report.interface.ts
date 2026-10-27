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
  gender: { male: number; female: number; other: number };
  housing: { street: number; housed: number };
  dni: { withDni: number; withoutDni: number };
  /** De dónde son (distribución geográfica) */
  geography: CountItem[];
  reasons: {
    alcohol: number;
    drugs: number;
    violence: number;
    multiple: number;
    unspecified: number;
  };
}
