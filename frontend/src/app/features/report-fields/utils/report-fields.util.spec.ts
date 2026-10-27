import { cleanReportData, hasPendingReportFields, pendingReportFields } from './report-fields.util';

describe('pendingReportFields', () => {
  it('sin datos están pendientes los 5 campos', () => {
    expect(pendingReportFields(undefined).length).toBe(5);
    expect(pendingReportFields({}).length).toBe(5);
  });

  it('un campo cargado deja de estar pendiente', () => {
    const pending = pendingReportFields({ gender: 'femenino', housing: '' });
    expect(pending.includes('gender')).toBe(false);
    expect(pending.includes('housing')).toBe(true);
    expect(pending.length).toBe(4);
  });

  it('completo no tiene pendientes', () => {
    const full = { gender: 'a', housing: 'b', origin: 'c', document: 'd', mainReason: 'e' };
    expect(hasPendingReportFields(full)).toBe(false);
  });

  it('cleanReportData deja null en los vacíos', () => {
    expect(cleanReportData({ gender: '', housing: 'x' })).toEqual({
      gender: null, housing: 'x', origin: null, document: null, mainReason: null,
    });
  });
});
