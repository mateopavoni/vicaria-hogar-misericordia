/**
 * SCRUM-218 (front de SCRUM-54) + SCRUM-230 (front de SCRUM-57): registro de asistencia diaria.
 * Las actividades ya no son una lista fija: son un catálogo configurable por el Referente
 * (ver AttendanceActivitiesService). El id es un texto estable.
 */
export type AttendanceActivity = string;

/**
 * Área del hogar. El registro de la Casa de Convivencia es diario y va aparte del Hogar de
 * día (SCRUM-57): tiene su propia actividad fija y solo admite residentes.
 */
export type AttendanceArea = 'hogar-de-dia' | 'casa-convivencia';

/** De dónde sale la actividad: fija del sistema, sincronizada del calendario o creada a mano. */
export type AttendanceActivitySource = 'system' | 'calendar' | 'custom';

export interface AttendanceActivityDef {
  id: AttendanceActivity;
  name: string;
  area: AttendanceArea;
  /** Solo las activas se ofrecen para registrar; las inactivas conservan su historial. */
  active: boolean;
  source: AttendanceActivitySource;
}

export const HOGAR_DE_DIA_ID = 'hogar-de-dia';
export const CASA_CONVIVENCIA_ID = 'casa-convivencia';

export const AREA_LABELS: Record<AttendanceArea, string> = {
  'hogar-de-dia': 'Hogar de día',
  'casa-convivencia': 'Casa de Convivencia (diaria)',
};

/** Actividades fijas: no se pueden desactivar (son la base de cada área). */
export const SYSTEM_ACTIVITIES: AttendanceActivityDef[] = [
  { id: HOGAR_DE_DIA_ID, name: 'Hogar de día', area: 'hogar-de-dia', active: true, source: 'system' },
  { id: CASA_CONVIVENCIA_ID, name: 'Casa de Convivencia (diaria)', area: 'casa-convivencia', active: true, source: 'system' },
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
