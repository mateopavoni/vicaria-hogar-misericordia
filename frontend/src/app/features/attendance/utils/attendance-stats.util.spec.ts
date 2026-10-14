import { AttendanceHistoryEntry } from '../interfaces/attendance.interface';
import { computeAttendanceStats, isAbsenceAlert } from './attendance-stats.util';

const hd = (date: string): AttendanceHistoryEntry => ({ activity: 'hogar-de-dia', date });

describe('computeAttendanceStats', () => {
  it('sin asistencias devuelve todo en cero y sin alerta', () => {
    const stats = computeAttendanceStats([], '2026-10-13');
    expect(stats).toEqual({ totalDays: 0, currentStreak: 0, lastAttendance: null, daysSinceLast: null });
    expect(isAbsenceAlert(stats)).toBe(false);
  });

  it('cuenta días distintos aunque haya dos actividades el mismo día', () => {
    const stats = computeAttendanceStats(
      [hd('2026-10-12'), { activity: 'casa-convivencia', date: '2026-10-12' }, hd('2026-10-10')],
      '2026-10-13',
    );
    expect(stats.totalDays).toBe(2);
  });

  it('calcula la racha actual de días consecutivos hasta ayer o hoy', () => {
    const stats = computeAttendanceStats([hd('2026-10-13'), hd('2026-10-12'), hd('2026-10-11'), hd('2026-10-09')], '2026-10-13');
    expect(stats.currentStreak).toBe(3);
    expect(stats.lastAttendance).toBe('2026-10-13');
    expect(stats.daysSinceLast).toBe(0);
  });

  it('la racha es 0 si la última asistencia fue hace más de un día', () => {
    const stats = computeAttendanceStats([hd('2026-10-10'), hd('2026-10-09')], '2026-10-13');
    expect(stats.currentStreak).toBe(0);
    expect(stats.daysSinceLast).toBe(3);
  });

  it('alerta solo cuando pasan más de 7 días sin asistir', () => {
    expect(isAbsenceAlert(computeAttendanceStats([hd('2026-10-06')], '2026-10-13'))).toBe(false); // 7 días
    expect(isAbsenceAlert(computeAttendanceStats([hd('2026-10-05')], '2026-10-13'))).toBe(true); // 8 días
  });

  it('cruza el cambio de mes sin errores de huso horario', () => {
    const stats = computeAttendanceStats([hd('2026-10-01'), hd('2026-09-30')], '2026-10-01');
    expect(stats.currentStreak).toBe(2);
  });
});
