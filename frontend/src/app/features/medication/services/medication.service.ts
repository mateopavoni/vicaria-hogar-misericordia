import { Injectable } from '@angular/core';
import { Observable, delay, of, throwError } from 'rxjs';
import {
  CreateMedicationSchemeRequest,
  MedicationActor,
  MedicationCatalogItem,
  MedicationScheme,
  PsychiatricEvaluationStatus,
} from '../interfaces/medication.interface';

const TIME_PATTERN = /^([01]\d|2[0-3]):[0-5]\d$/;

/**
 * SCRUM-234: esquema de medicación de una persona.
 *
 * OJO — backend pendiente (EP-11, SCRUM-59): hoy no existen endpoints de catálogo de
 * medicamentos, stock de farmacia, evaluaciones psiquiátricas ni esquemas. Hasta entonces
 * este servicio vive en memoria (se pierde al recargar) y aplica las reglas del AC con la
 * misma forma de error ({ error: { message } }) que lee extractApiError. Al llegar los
 * endpoints reales solo hay que cambiar el cuerpo de estos métodos por llamadas HttpClient.
 *
 * - Catálogo: DATOS DE PRUEBA con nombres genéricos (no son medicamentos reales), uno de
 *   ellos con stock 0 para poder ver la alerta de "pendiente de stock".
 * - Evaluación vigente: el backend exige una evaluación vigente para que una persona sea
 *   Residente (SCRUM-134), así que acá se asume vigente para residentes y no vigente para el resto.
 */
@Injectable({ providedIn: 'root' })
export class MedicationService {
  private readonly catalog: MedicationCatalogItem[] = [
    { id: 'med-1', name: 'Medicamento de prueba A', pharmacyStock: 40 },
    { id: 'med-2', name: 'Medicamento de prueba B', pharmacyStock: 12 },
    { id: 'med-3', name: 'Medicamento de prueba C (sin stock)', pharmacyStock: 0 },
  ];

  private readonly schemes: MedicationScheme[] = [];
  private nextId = 1;

  getCatalog(): Observable<MedicationCatalogItem[]> {
    return of(this.catalog.map((m) => ({ ...m }))).pipe(delay(100));
  }

  getEvaluationStatus(_personId: string, isResident: boolean): Observable<PsychiatricEvaluationStatus> {
    return of({ valid: isResident }).pipe(delay(100));
  }

  getSchemes(personId: string): Observable<MedicationScheme[]> {
    const own = this.schemes
      .filter((s) => s.personId === personId)
      .sort((a, b) => b.loadedAt.localeCompare(a.loadedAt));
    return of(own.map((s) => ({ ...s }))).pipe(delay(100));
  }

  create(request: CreateMedicationSchemeRequest, isResident: boolean, actor: MedicationActor): Observable<MedicationScheme> {
    // 1) evaluación psiquiátrica vigente, si no se bloquea la carga
    if (!isResident) {
      return this.fail('No se puede cargar medicación: la persona no tiene una evaluación psiquiátrica vigente.');
    }

    // 2) solo medicamentos que existan en el catálogo
    const medication = this.catalog.find((m) => m.id === request.medicationId);
    if (!medication) {
      return this.fail('El medicamento no existe en el catálogo.');
    }

    const dose = request.dose.trim();
    if (!dose) {
      return this.fail('La dosis es obligatoria.');
    }
    if (!TIME_PATTERN.test(request.time)) {
      return this.fail('La hora no es válida.');
    }

    // 3) sin stock: se permite, pero queda marcado como pendiente de stock
    const scheme: MedicationScheme = {
      id: `scheme-${this.nextId++}`,
      personId: request.personId,
      medicationId: medication.id,
      medicationName: medication.name,
      dose,
      time: request.time,
      loadedAt: new Date().toISOString(),
      loadedByUserId: actor.id,
      loadedByName: actor.name,
      active: true,
      pendingStock: medication.pharmacyStock <= 0,
    };
    this.schemes.push(scheme);
    return of({ ...scheme }).pipe(delay(150));
  }

  /** Marca un esquema como vigente (true) o histórico (false). */
  setActive(schemeId: string, active: boolean): Observable<MedicationScheme> {
    const scheme = this.schemes.find((s) => s.id === schemeId);
    if (!scheme) {
      return this.fail('El esquema no existe.');
    }
    scheme.active = active;
    return of({ ...scheme }).pipe(delay(100));
  }

  private fail(message: string): Observable<never> {
    return throwError(() => ({ error: { message } }));
  }
}
