import {
  MedicationHistoryEntry,
  MedicationHistoryFilters,
  MedicationHistorySummary,
} from '../interfaces/medication.interface';

/** Filtra por rango de fechas (inclusivo, YYYY-MM-DD) y por medicamento. Vacío = sin filtro. */
export function filterHistory(
  entries: MedicationHistoryEntry[],
  filters: MedicationHistoryFilters,
): MedicationHistoryEntry[] {
  return entries.filter(
    (e) =>
      (!filters.from || e.date >= filters.from) &&
      (!filters.to || e.date <= filters.to) &&
      (!filters.medicationId || e.medicationId === filters.medicationId),
  );
}

export function summarizeHistory(entries: MedicationHistoryEntry[]): MedicationHistorySummary {
  return {
    total: entries.length,
    administered: entries.filter((e) => e.status === 'administered').length,
    notAdministered: entries.filter((e) => e.status === 'not-administered').length,
    unregistered: entries.filter((e) => e.status === 'unregistered').length,
  };
}
