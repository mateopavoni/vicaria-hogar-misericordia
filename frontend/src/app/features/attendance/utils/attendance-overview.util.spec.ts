import { DailyAttendance } from '../interfaces/attendance.interface';
import { computeOverview, findInactivePersons } from './attendance-overview.util';

const ana = { personId: 'a', fullName: 'Ana', dni: null };
const beto = { personId: 'b', fullName: 'Beto', dni: null };

const day = (activity: DailyAttendance['activity'], date: string, persons = [ana], anonymousCount = 0): DailyAttendance => ({
  activity,
  date,
  persons,
  anonymousCount,
  justification: null,
});

describe('computeOverview', () => {
  const records = [
    day('hogar-de-dia', '2026-10-05', [ana, beto], 1), // lunes, total 3
    day('hogar-de-dia', '2026-10-06', [ana], 0), // martes, total 1
    day('casa-convivencia', '2026-10-06', [beto], 2), // martes, total 3
    day('hogar-de-dia', '2026-10-13', [ana], 1), // semana siguiente, total 2
  ];
  const all = { from: '2026-10-01', to: '2026-10-31', activity: null };

  it('suma presencias con ficha y sin ficha', () => {
    expect(computeOverview(records, all).totalPresences).toBe(9);
  });

  it('calcula el promedio diario sobre los días con datos', () => {
    // días: 5 (3), 6 (1+3=4), 13 (2) => 9 / 3 días
    const overview = computeOverview(records, all);
    expect(overview.daysWithData).toBe(3);
    expect(overview.dailyAverage).toBe(3);
  });

  it('calcula promedio semanal y mensual', () => {
    const overview = computeOverview(records, all);
    // semana 5-11 oct: 3+4 = 7; semana 12-18 oct: 2 => (7+2)/2
    expect(overview.weeklyAverage).toBe(4.5);
    // todo octubre: 9 en un solo mes
    expect(overview.monthlyAverage).toBe(9);
  });

  it('filtra por actividad y arma la comparativa', () => {
    const overview = computeOverview(records, { ...all, activity: 'casa-convivencia' });
    expect(overview.totalPresences).toBe(3);
    expect(overview.byActivity.length).toBe(1);
    expect(overview.byActivity[0].averagePerDay).toBe(3);
  });

  it('respeta el rango de fechas', () => {
    expect(computeOverview(records, { ...all, to: '2026-10-05' }).totalPresences).toBe(3);
  });

  it('sin datos devuelve ceros', () => {
    const overview = computeOverview([], all);
    expect(overview.dailyAverage).toBe(0);
    expect(overview.weeklyAverage).toBe(0);
  });
});

describe('findInactivePersons', () => {
  it('lista a quien no asiste hace más de 14 días y excluye a quien asistió hace 14', () => {
    const records = [day('hogar-de-dia', '2026-09-28', [ana]), day('hogar-de-dia', '2026-09-29', [beto])];
    const result = findInactivePersons(records, '2026-10-13');
    expect(result.map((p) => p.personId)).toEqual(['a']); // Ana: 15 días; Beto: 14 días
    expect(result[0].daysSince).toBe(15);
  });

  it('usa la última asistencia aunque haya ido a otra actividad después', () => {
    const records = [day('hogar-de-dia', '2026-08-01', [ana]), day('casa-convivencia', '2026-10-10', [ana])];
    expect(findInactivePersons(records, '2026-10-13')).toEqual([]);
  });
});
