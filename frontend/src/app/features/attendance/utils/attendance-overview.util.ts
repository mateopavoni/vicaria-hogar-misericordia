import {
  ActivityStats,
  AttendanceActivityDef,
  AttendanceOverview,
  AttendanceStatsFilters,
  DailyAttendance,
  DayTotal,
  InactivePerson,
} from '../interfaces/attendance.interface';

/** Una persona que no asiste hace más de estos días se lista como "dejó de asistir" (SCRUM-56). */
export const INACTIVE_AFTER_DAYS = 14;

const round1 = (n: number): number => Math.round(n * 10) / 10;

function dayNumber(dateIso: string): number {
  const [y, m, d] = dateIso.split('-').map(Number);
  return Math.floor(Date.UTC(y, m - 1, d) / 86_400_000);
}

// lunes de la semana de esa fecha (clave de agrupación semanal)
function weekKey(dateIso: string): number {
  const n = dayNumber(dateIso);
  // 1970-01-01 fue jueves: (n + 3) % 7 => 0 = lunes
  return n - ((n + 3) % 7);
}

const monthKey = (dateIso: string): string => dateIso.slice(0, 7);

const dayTotal = (d: DailyAttendance): number => d.persons.length + d.anonymousCount;

function average(values: number[]): number {
  return values.length ? round1(values.reduce((a, b) => a + b, 0) / values.length) : 0;
}

function groupSums<K>(records: DailyAttendance[], keyOf: (date: string) => K): number[] {
  const sums = new Map<K, number>();
  for (const r of records) {
    sums.set(keyOf(r.date), (sums.get(keyOf(r.date)) ?? 0) + dayTotal(r));
  }
  return [...sums.values()];
}

/**
 * Estadísticas del período filtrado. Promedio diario = presencias / días con datos;
 * semanal y mensual = promedio de los totales de cada semana (lunes a domingo) o mes
 * que tiene al menos un día cargado.
 */
export function computeOverview(
  records: readonly DailyAttendance[],
  filters: AttendanceStatsFilters,
  activities: readonly Pick<AttendanceActivityDef, 'id' | 'name'>[],
): AttendanceOverview {
  const inRange = records.filter(
    (r) =>
      r.date >= filters.from &&
      r.date <= filters.to &&
      (!filters.activity || r.activity === filters.activity),
  );

  const perDayMap = new Map<string, number>();
  for (const r of inRange) {
    perDayMap.set(r.date, (perDayMap.get(r.date) ?? 0) + dayTotal(r));
  }
  const perDay: DayTotal[] = [...perDayMap.entries()]
    .map(([date, total]) => ({ date, total }))
    .sort((a, b) => a.date.localeCompare(b.date));

  const totalPresences = perDay.reduce((sum, d) => sum + d.total, 0);

  // una barra por actividad con datos en el período (o la elegida en el filtro, aunque esté vacía)
  const byActivity: ActivityStats[] = activities.filter((a) => !filters.activity || a.id === filters.activity).map(
    (a) => {
      const own = inRange.filter((r) => r.activity === a.id);
      const total = own.reduce((sum, r) => sum + dayTotal(r), 0);
      const days = new Set(own.map((r) => r.date)).size;
      return {
        activity: a.id,
        label: a.name,
        totalPresences: total,
        daysWithData: days,
        averagePerDay: days ? round1(total / days) : 0,
      };
    },
  ).filter((stat) => stat.daysWithData > 0 || !!filters.activity);

  return {
    totalPresences,
    daysWithData: perDay.length,
    dailyAverage: perDay.length ? round1(totalPresences / perDay.length) : 0,
    weeklyAverage: average(groupSums(inRange, weekKey)),
    monthlyAverage: average(groupSums(inRange, monthKey)),
    byActivity,
    perDay,
  };
}

/**
 * Personas con ficha cuya última asistencia (en cualquier actividad y sin importar el
 * rango elegido) fue hace más de `afterDays` días. Quien nunca asistió no aparece.
 */
export function findInactivePersons(
  records: readonly DailyAttendance[],
  todayIso: string,
  afterDays: number = INACTIVE_AFTER_DAYS,
): InactivePerson[] {
  const last = new Map<string, InactivePerson>();

  for (const r of records) {
    if (r.date > todayIso) {
      continue;
    }
    for (const p of r.persons) {
      const known = last.get(p.personId);
      if (!known || r.date > known.lastAttendance) {
        last.set(p.personId, { personId: p.personId, fullName: p.fullName, lastAttendance: r.date, daysSince: 0 });
      }
    }
  }

  return [...last.values()]
    .map((p) => ({ ...p, daysSince: dayNumber(todayIso) - dayNumber(p.lastAttendance) }))
    .filter((p) => p.daysSince > afterDays)
    .sort((a, b) => b.daysSince - a.daysSince);
}
