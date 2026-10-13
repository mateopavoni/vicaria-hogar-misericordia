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

/**
 * SCRUM-238 (front de SCRUM-60): agenda diaria de medicamentos.
 * OJO — backend pendiente: ver MedicationService (agenda y administraciones en memoria).
 */
export type AdministrationStatus = 'administered' | 'not-administered';

/** Registro de una toma de un esquema en una fecha (YYYY-MM-DD). */
export interface MedicationAdministration {
  schemeId: string;
  date: string;
  status: AdministrationStatus;
  /** Opcional. */
  observation: string | null;
  recordedAt: string;
  recordedByUserId: string;
  recordedByName: string;
}

/** Una toma de la agenda: esquema + persona + (si ya se registró) su administración. */
export interface AgendaItem {
  schemeId: string;
  personId: string;
  personName: string;
  medicationName: string;
  dose: string;
  /** HH:mm */
  time: string;
  administration: MedicationAdministration | null;
}

/** Tomas de una misma hora, ej. 08:00 → Juan Pérez: A 2 mg / José Reyes: B 500 mg. */
export interface AgendaTimeSlot {
  time: string;
  items: AgendaItem[];
}

export interface MedicationAgenda {
  date: string;
  slots: AgendaTimeSlot[];
  /** Esquemas de personas presentes que no entran a la agenda por estar pendientes de stock. */
  pendingStockCount: number;
}

export interface RecordAdministrationRequest {
  schemeId: string;
  date: string;
  status: AdministrationStatus;
  observation: string | null;
}

/**
 * SCRUM-242 (front de SCRUM-61): historial de tomas de una persona.
 * 'unregistered' = estaba programada pero nadie la marcó.
 */
export type HistoryStatus = AdministrationStatus | 'unregistered';

export interface MedicationHistoryEntry {
  /** YYYY-MM-DD */
  date: string;
  /** HH:mm */
  time: string;
  schemeId: string;
  medicationId: string;
  medicationName: string;
  dose: string;
  status: HistoryStatus;
  observation: string | null;
  recordedByName: string | null;
  recordedAt: string | null;
}

export interface MedicationHistoryFilters {
  /** YYYY-MM-DD, vacío = sin límite */
  from: string;
  to: string;
  /** vacío = todos los medicamentos */
  medicationId: string;
}

export interface MedicationHistorySummary {
  total: number;
  administered: number;
  notAdministered: number;
  unregistered: number;
}
