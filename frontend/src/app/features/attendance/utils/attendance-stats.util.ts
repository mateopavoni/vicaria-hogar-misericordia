import { AttendanceHistoryEntry, AttendanceStats } from '../interfaces/attendance.interface';

/** Más de este número de días sin asistir se marca como alerta en el perfil (SCRUM-55). */
export const ABSENCE_ALERT_DAYS = 7;

// días desde la época de una fecha YYYY-MM-DD, sin depender del huso horario
function toDayNumber(dateIso: string): number {
  const [y, m, d] = dateIso.split('-').map(Number);
  return Math.floor(Date.UTC(y, m - 1, d) / 86_400_000);
}

/**
 * Calcula los indicadores del historial. `todayIso` se recibe por parámetro (en vez de
 * leer el reloj) para que sea testeable. Las fechas futuras se ignoran.
 */
export function computeAttendanceStats(entries: readonly AttendanceHistoryEntry[], todayIso: string): AttendanceStats {
  const days = [...new Set(entries.map((e) => e.date).filter((d) => d <= todayIso))].sort().reverse();

  if (days.length === 0) {
    return { totalDays: 0, currentStreak: 0, lastAttendance: null, daysSinceLast: null };
  }

  const last = days[0];
  const daysSinceLast = toDayNumber(todayIso) - toDayNumber(last);

  // la racha sigue "viva" si asistió hoy o ayer
  let currentStreak = 0;
  if (daysSinceLast <= 1) {
    currentStreak = 1;
    for (let i = 1; i < days.length; i++) {
      if (toDayNumber(days[i - 1]) - toDayNumber(days[i]) === 1) {
        currentStreak++;
      } else {
        break;
      }
    }
  }

  return { totalDays: days.length, currentStreak, lastAttendance: last, daysSinceLast };
}

export function isAbsenceAlert(stats: AttendanceStats): boolean {
  return stats.daysSinceLast !== null && stats.daysSinceLast > ABSENCE_ALERT_DAYS;
}
