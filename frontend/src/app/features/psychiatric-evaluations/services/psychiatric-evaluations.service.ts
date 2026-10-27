import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, forkJoin, map, switchMap, throwError } from 'rxjs';
import { todayLocalIso } from '../../../shared/utils/date.util';
import { EvaluationActor, PsychiatricEvaluation, RegisterEvaluationRequest } from '../interfaces/psychiatric-evaluation.interface';

/** PsychiatricEvaluationStatusDto del backend (SCRUM-259). */
interface StatusDto {
  hasEvaluation: boolean;
  evaluationId: string | null;
  date: string | null;
  professional: string | null;
  diagnosis: string | null;
}

/** PsychiatricEvaluationHistoryItemDto del backend (SCRUM-259). */
interface HistoryItemDto {
  id: string;
  personId: string;
  date: string;
  professional: string | null;
  diagnosis: string | null;
  isValid: boolean;
  registeredByName: string;
}

/**
 * SCRUM-260: evaluaciones psiquiátricas contra la API (SCRUM-258/259).
 * `load` trae la vigente (GET .../psychiatric-evaluations/status) y las anteriores
 * (GET .../history) y deja todo en `evaluations`; `register` hace el POST y recarga. El backend
 * pone vigente = false en las anteriores y actualiza tiene_eval_psiq de la ficha.
 */
@Injectable({ providedIn: 'root' })
export class PsychiatricEvaluationsService {
  private readonly http = inject(HttpClient);

  /** Estado compartido: el perfil, la pestaña y medicación leen de acá. */
  readonly evaluations = signal<PsychiatricEvaluation[]>([]);

  load(personId: string): Observable<void> {
    const base = `/api/persons/${personId}/psychiatric-evaluations`;
    return forkJoin([this.http.get<StatusDto>(`${base}/status`), this.http.get<HistoryItemDto[]>(`${base}/history`)]).pipe(
      map(([status, history]) => {
        const loaded = history.map((h) => toEvaluation(h.id, personId, h.date, h.professional, h.diagnosis, false, h.registeredByName));
        if (status.hasEvaluation && status.evaluationId && status.date) {
          loaded.push(toEvaluation(status.evaluationId, personId, status.date, status.professional, status.diagnosis, true, ''));
        }
        this.evaluations.update((list) => [...list.filter((e) => e.personId !== personId), ...loaded]);
      }),
    );
  }

  /** Evaluaciones de la persona, la más reciente primero. */
  forPerson(personId: string): PsychiatricEvaluation[] {
    return this.evaluations()
      .filter((e) => e.personId === personId)
      .sort((a, b) => b.date.localeCompare(a.date) || Number(b.vigente) - Number(a.vigente));
  }

  /** La única evaluación con vigente = true, si existe. */
  currentFor(personId: string): PsychiatricEvaluation | null {
    return this.evaluations().find((e) => e.personId === personId && e.vigente) ?? null;
  }

  register(request: RegisterEvaluationRequest, _actor: EvaluationActor): Observable<PsychiatricEvaluation> {
    const professional = request.professional.trim();
    const diagnosis = request.diagnosis.trim();
    // mismas reglas del formulario, para avisar sin llamar a la API
    if (!request.date) {
      return this.fail('La fecha de la evaluación es obligatoria.');
    }
    if (request.date > todayLocalIso()) {
      return this.fail('La fecha de la evaluación no puede ser futura.');
    }
    const current = this.currentFor(request.personId);
    if (current && request.date < current.date) {
      return this.fail('La fecha no puede ser anterior a la de la evaluación vigente.');
    }
    if (professional.length < 3 || professional.length > 100) {
      return this.fail('El profesional debe tener entre 3 y 100 caracteres.');
    }
    if (!diagnosis) {
      return this.fail('El diagnóstico es obligatorio.');
    }
    if (diagnosis.length > 2000) {
      return this.fail('El diagnóstico no puede superar los 2000 caracteres.');
    }

    return this.http
      .post<{ id: string }>(`/api/persons/${request.personId}/psychiatric-evaluations`, { date: request.date, professional, diagnosis })
      .pipe(
        switchMap(() => this.load(request.personId)),
        map(() => this.currentFor(request.personId)!),
      );
  }

  private fail(message: string): Observable<never> {
    return throwError(() => ({ error: { message } }));
  }
}

function toEvaluation(
  id: string,
  personId: string,
  date: string,
  professional: string | null,
  diagnosis: string | null,
  vigente: boolean,
  registeredByName: string,
): PsychiatricEvaluation {
  return {
    id,
    personId,
    date: date.slice(0, 10),
    professional: professional ?? '',
    diagnosis: diagnosis ?? '',
    vigente,
    registeredByName,
    registeredAt: '',
  };
}
