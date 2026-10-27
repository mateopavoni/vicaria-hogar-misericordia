import { Injectable, signal } from '@angular/core';
import { Observable, delay, of, throwError } from 'rxjs';
import { todayLocalIso } from '../../../shared/utils/date.util';
import { EvaluationActor, PsychiatricEvaluation, RegisterEvaluationRequest } from '../interfaces/psychiatric-evaluation.interface';

/**
 * SCRUM-260: evaluaciones psiquiátricas.
 *
 * OJO — backend pendiente (SCRUM-65): la entidad PsychiatricEvaluation existe en el backend, pero
 * no hay endpoints para listar ni cargar. Hasta entonces las evaluaciones viven en memoria (se
 * pierden al recargar) y se aplican acá las reglas del AC: al cargar una nueva, las anteriores
 * pasan a vigente = false (solo una vigente por persona). El campo tiene_eval_psiq de la ficha y
 * la validación real de Residente / ingreso a la Casa de Convivencia dependen del backend.
 * Al llegar los endpoints, `register` pasa a ser un POST y `load` un GET que llena `evaluations`.
 */
@Injectable({ providedIn: 'root' })
export class PsychiatricEvaluationsService {
  /** Estado compartido: el perfil, la pestaña y medicación leen de acá. */
  readonly evaluations = signal<PsychiatricEvaluation[]>([]);
  private nextId = 1;

  /** Hoy no hace nada (todo está en memoria); con backend traería las evaluaciones de la persona. */
  load(_personId: string): Observable<void> {
    return of(undefined).pipe(delay(50));
  }

  /** Evaluaciones de la persona, la más reciente primero. */
  forPerson(personId: string): PsychiatricEvaluation[] {
    return this.evaluations()
      .filter((e) => e.personId === personId)
      .sort((a, b) => b.date.localeCompare(a.date) || Number(b.id.split('-')[1]) - Number(a.id.split('-')[1]));
  }

  /** La única evaluación con vigente = true, si existe. */
  currentFor(personId: string): PsychiatricEvaluation | null {
    return this.evaluations().find((e) => e.personId === personId && e.vigente) ?? null;
  }

  register(request: RegisterEvaluationRequest, actor: EvaluationActor): Observable<PsychiatricEvaluation> {
    const professional = request.professional.trim();
    const diagnosis = request.diagnosis.trim();

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

    const evaluation: PsychiatricEvaluation = {
      id: `eval-${this.nextId++}`,
      personId: request.personId,
      date: request.date,
      professional,
      diagnosis,
      vigente: true,
      registeredByName: actor.name,
      registeredAt: new Date().toISOString(),
    };
    // una sola vigente por persona: las anteriores pasan a vigente = false
    this.evaluations.update((list) => [
      ...list.map((e) => (e.personId === request.personId ? { ...e, vigente: false } : e)),
      evaluation,
    ]);
    return of({ ...evaluation }).pipe(delay(150));
  }

  private fail(message: string): Observable<never> {
    return throwError(() => ({ error: { message } }));
  }
}
