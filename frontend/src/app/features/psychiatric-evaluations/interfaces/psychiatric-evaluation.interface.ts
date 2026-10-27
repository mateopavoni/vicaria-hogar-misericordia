/**
 * SCRUM-260 (front de SCRUM-65): evaluación psiquiátrica de una persona.
 * OJO — backend pendiente: la entidad PsychiatricEvaluation existe pero no hay endpoints
 * (ver PsychiatricEvaluationsService).
 */
export interface PsychiatricEvaluation {
  id: string;
  personId: string;
  /** Fecha de la evaluación (YYYY-MM-DD). Vence a los 6 meses. */
  date: string;
  professional: string;
  /** Texto libre. */
  diagnosis: string;
  /** true = la evaluación actual de la persona. Solo una por persona. */
  vigente: boolean;
  /** Quién la cargó. */
  registeredByName: string;
  /** ISO */
  registeredAt: string;
}

export interface RegisterEvaluationRequest {
  personId: string;
  date: string;
  professional: string;
  diagnosis: string;
}

export interface EvaluationActor {
  id: string;
  name: string;
}

/** valid = vigente por más de 30 días; expiring = vence en menos de 30 días; expired = vencida. */
export type EvaluationValidity = 'valid' | 'expiring' | 'expired';
