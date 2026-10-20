import { MedicationHistoryEntry } from '../interfaces/medication.interface';
import { filterHistory, summarizeHistory } from './medication-history.util';

const entry = (date: string, medicationId: string, status: MedicationHistoryEntry['status']): MedicationHistoryEntry => ({
  date,
  time: '08:00',
  schemeId: `s-${medicationId}`,
  medicationId,
  medicationName: medicationId,
  dose: '1',
  status,
  observation: null,
  recordedByName: null,
  recordedAt: null,
});

describe('medication-history.util', () => {
  const data = [
    entry('2026-10-10', 'a', 'administered'),
    entry('2026-10-11', 'a', 'not-administered'),
    entry('2026-10-11', 'b', 'unregistered'),
    entry('2026-10-12', 'b', 'administered'),
  ];

  it('sin filtros devuelve todo', () => {
    expect(filterHistory(data, { from: '', to: '', medicationId: '' }).length).toBe(4);
  });

  it('filtra por rango inclusivo', () => {
    const r = filterHistory(data, { from: '2026-10-11', to: '2026-10-11', medicationId: '' });
    expect(r.length).toBe(2);
  });

  it('filtra por medicamento y combina con el rango', () => {
    expect(filterHistory(data, { from: '', to: '', medicationId: 'b' }).length).toBe(2);
    expect(filterHistory(data, { from: '2026-10-12', to: '', medicationId: 'b' }).length).toBe(1);
  });

  it('resume por estado', () => {
    expect(summarizeHistory(data)).toEqual({ total: 4, administered: 2, notAdministered: 1, unregistered: 1 });
  });
});
