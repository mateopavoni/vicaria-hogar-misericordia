import { DailyAttendance } from '../../attendance/interfaces/attendance.interface';
import { MedicationStockItem } from '../../medication-stock/interfaces/medication-stock.interface';
import { itemsNeedingAttention } from '../../medication-stock/utils/stock-level.util';

export interface AttendanceSummary {
  /** Personas con ficha + anónimas, sumadas entre actividades del día. */
  total: number;
  /** Actividades con al menos una persona registrada. */
  activities: number;
}

/** Resume la asistencia de un día: personas totales y cantidad de actividades con registro. */
export function summarizeAttendance(all: DailyAttendance[], dateIso: string): AttendanceSummary {
  const day = all
    .filter((a) => a.date === dateIso)
    .map((a) => a.persons.length + a.anonymousCount)
    .filter((n) => n > 0);
  return { total: day.reduce((s, n) => s + n, 0), activities: day.length };
}

/** Eventos/visitas de un día ordenados por hora de inicio (ISO), sin las canceladas. */
export function sortByStart<T extends { start: string }>(items: T[]): T[] {
  return [...items].sort((a, b) => a.start.localeCompare(b.start));
}

/** "HH:mm" de un ISO local ("2026-10-26T14:30:00"); vacío si no trae hora. */
export function timeOf(iso: string): string {
  return iso.length >= 16 && iso.includes('T') ? iso.slice(11, 16) : '';
}

/** Medicamentos sin stock o con stock bajo (los sin stock primero). */
export function lowStockItems(items: MedicationStockItem[]): MedicationStockItem[] {
  return itemsNeedingAttention(items);
}
