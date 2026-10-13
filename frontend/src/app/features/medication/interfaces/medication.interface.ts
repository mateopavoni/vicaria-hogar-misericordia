/**
 * SCRUM-234 (front de SCRUM-59): esquema de medicación de una persona.
 * OJO — backend pendiente (EP-11): todavía no existen el catálogo de medicamentos, el
 * stock de farmacia ni los esquemas (ver MedicationService).
 */

/** Medicamento del catálogo (medicamentos_catalogo) con su stock en farmacia. */
export interface MedicationCatalogItem {
  id: string;
  name: string;
  /** Unidades disponibles en farmacia. 0 = sin stock. */
  pharmacyStock: number;
}

/** Evaluación psiquiátrica de la persona: sin una vigente no se pueden cargar medicamentos. */
export interface PsychiatricEvaluationStatus {
  valid: boolean;
}

export interface MedicationScheme {
  id: string;
  personId: string;
  medicationId: string;
  medicationName: string;
  /** Texto libre, ej. "500 mg". */
  dose: string;
  /** HH:mm */
  time: string;
  /** Fecha y hora de carga (ISO). */
  loadedAt: string;
  loadedByUserId: string;
  loadedByName: string;
  /** true = vigente; false = histórico. */
  active: boolean;
  /** true si al cargarlo no había stock en farmacia: no entra a la agenda hasta que haya. */
  pendingStock: boolean;
}

export interface CreateMedicationSchemeRequest {
  personId: string;
  medicationId: string;
  dose: string;
  time: string;
}

/** Quién carga el esquema; se completa con el usuario logueado, nunca a mano. */
export interface MedicationActor {
  id: string;
  name: string;
}
