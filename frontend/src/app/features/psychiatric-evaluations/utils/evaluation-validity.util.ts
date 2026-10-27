import { evaluationExpiryDate, isEvaluationExpired } from '../../medication/utils/scheme-review.util';
import { EvaluationValidity } from '../interfaces/psychiatric-evaluation.interface';

export { evaluationExpiryDate, isEvaluationExpired };

/** Se alerta cuando faltan menos de estos días para que venza la evaluación. */
export const EXPIRING_SOON_DAYS = 30;

const DAY_MS = 24 * 60 * 60 * 1000;

const utc = (iso: string): number => {
  const [y, m, d] = iso.slice(0, 10).split('-').map(Number);
  return Date.UTC(y, m - 1, d);
};

/** Días que faltan para el vencimiento (negativo si ya venció). */
export function daysUntilExpiry(evaluationDate: string, todayIso: string): number {
  return Math.round((utc(evaluationExpiryDate(evaluationDate)) - utc(todayIso)) / DAY_MS);
}

/** Vigencia calculada por fecha: la evaluación vence a los 6 meses desde su fecha. */
export function evaluationValidity(evaluationDate: string, todayIso: string): EvaluationValidity {
  if (isEvaluationExpired(evaluationDate, todayIso)) return 'expired';
  return daysUntilExpiry(evaluationDate, todayIso) < EXPIRING_SOON_DAYS ? 'expiring' : 'valid';
}
