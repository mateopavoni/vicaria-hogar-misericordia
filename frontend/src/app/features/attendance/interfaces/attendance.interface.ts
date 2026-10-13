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
