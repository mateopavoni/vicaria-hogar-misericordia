import { MedicationScheme } from '../interfaces/medication.interface';

/** Un esquema vigente sin revisar hace más de estos días dispara la alerta (SCRUM-250). */
export const REVIEW_ALERT_DAYS = 30;

/** La evaluación psiquiátrica vence a los 6 meses: pasado eso no se pueden agregar medicamentos. */
export const EVALUATION_VALID_MONTHS = 6;

const DAY_MS = 24 * 60 * 60 * 1000;

/** Días desde la última revisión (revisado_en); si nunca se revisó, desde que se cargó. */
export function daysSinceReview(scheme: Pick<MedicationScheme, 'reviewedAt' | 'loadedAt'>, now: Date = new Date()): number {
  const reference = new Date(scheme.reviewedAt ?? scheme.loadedAt).getTime();
  return Math.floor((now.getTime() - reference) / DAY_MS);
}

/** true si es vigente y lleva más de 30 días sin revisión. */
export function needsReview(
  scheme: Pick<MedicationScheme, 'active' | 'reviewedAt' | 'loadedAt'>,
  now: Date = new Date(),
): boolean {
  return scheme.active && daysSinceReview(scheme, now) > REVIEW_ALERT_DAYS;
}

/** Fecha (YYYY-MM-DD) en que vence una evaluación hecha en `evaluatedAt` (+6 meses). */
export function evaluationExpiryDate(evaluatedAt: string): string {
  const [y, m, d] = evaluatedAt.split('-').map(Number);
  const target = new Date(y, m - 1 + EVALUATION_VALID_MONTHS, 1);
  const lastDay = new Date(target.getFullYear(), target.getMonth() + 1, 0).getDate();
  target.setDate(Math.min(d, lastDay));
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${target.getFullYear()}-${pad(target.getMonth() + 1)}-${pad(target.getDate())}`;
}

/** Vencida si pasaron más de 6 meses; el mismo día del vencimiento todavía es válida. */
export function isEvaluationExpired(evaluatedAt: string, todayIso: string): boolean {
  return todayIso > evaluationExpiryDate(evaluatedAt);
}
