/**
 * SCRUM-218 (front de SCRUM-54): registro de asistencia diaria.
 * Actividades pedidas por el AC: Hogar de día y Casa de Convivencia (diaria).
 */
export type AttendanceActivity = 'hogar-de-dia' | 'casa-convivencia';

export interface AttendanceActivityOption {
  id: AttendanceActivity;
  label: string;
}

export const ATTENDANCE_ACTIVITIES: AttendanceActivityOption[] = [
  { id: 'hogar-de-dia', label: 'Hogar de día' },
  { id: 'casa-convivencia', label: 'Casa de Convivencia (diaria)' },
];

/** Persona con ficha marcada como presente. */
export interface AttendancePerson {
  personId: string;
  fullName: string;
  dni: string | null;
}

/** Asistencia guardada de una actividad en una fecha (YYYY-MM-DD). */
export interface DailyAttendance {
  activity: AttendanceActivity;
  date: string;
  persons: AttendancePerson[];
  /** Personas sin ficha: solo se lleva el conteo. */
  anonymousCount: number;
  /** Última justificación al modificar un día ya cerrado. */
  justification: string | null;
}

export interface SaveDailyAttendanceRequest {
  activity: AttendanceActivity;
  date: string;
  persons: AttendancePerson[];
  anonymousCount: number;
  /** Obligatoria si el día ya está cerrado (fecha anterior a hoy). */
  justification: string | null;
}

/** SCRUM-222: una fecha en la que una persona asistió a una actividad. */
export interface AttendanceHistoryEntry {
  activity: AttendanceActivity;
  /** YYYY-MM-DD */
  date: string;
}

/** Indicadores simples del historial de una persona. */
export interface AttendanceStats {
  /** Días distintos con asistencia (aunque haya ido a dos actividades el mismo día). */
  totalDays: number;
  /** Días seguidos asistidos hasta la última asistencia; 0 si la última fue hace más de un día. */
  currentStreak: number;
  lastAttendance: string | null;
  /** Días corridos desde la última asistencia hasta hoy; null si nunca asistió. */
  daysSinceLast: number | null;
}

/** SCRUM-226: filtros del panel de estadísticas. */
export interface AttendanceStatsFilters {
  /** YYYY-MM-DD, inclusive */
  from: string;
  /** YYYY-MM-DD, inclusive */
  to: string;
  /** null = todas las actividades */
  activity: AttendanceActivity | null;
}

export interface ActivityStats {
  activity: AttendanceActivity;
  label: string;
  /** Presencias totales (con ficha + sin ficha) en el período. */
  totalPresences: number;
  /** Días del período con asistencia cargada para esta actividad. */
  daysWithData: number;
  averagePerDay: number;
}

export interface DayTotal {
  date: string;
  total: number;
}

export interface AttendanceOverview {
  totalPresences: number;
  daysWithData: number;
  dailyAverage: number;
  weeklyAverage: number;
  monthlyAverage: number;
  byActivity: ActivityStats[];
  perDay: DayTotal[];
}

/** Persona que dejó de asistir hace más de N días (SCRUM-56: 14). */
export interface InactivePerson {
  personId: string;
  fullName: string;
  lastAttendance: string;
  daysSince: number;
}
