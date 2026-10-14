import { CaritasReportService } from '../services/caritas-report.service';
import { buildCaritasDocument, caritasFilename, formatPercent, periodRange, reportCategories } from './caritas-report.util';

describe('periodRange', () => {
  it('trimestral: 3 meses terminando en la fecha', () => {
    expect(periodRange('trimestral', '2026-09-30').from).toBe('2026-07-01');
    expect(periodRange('trimestral', '2026-10-14').from).toBe('2026-07-15');
  });

  it('semestral: 6 meses, cruzando el año', () => {
    expect(periodRange('semestral', '2026-03-31').from).toBe('2025-10-01');
    expect(periodRange('semestral', '2026-06-30').from).toBe('2026-01-01');
  });

  it('ajusta cuando el mes de inicio es más corto', () => {
    expect(periodRange('trimestral', '2026-05-31').from).toBe('2026-03-01');
  });
});

describe('formatPercent', () => {
  it('usa coma decimal y tolera total 0', () => {
    expect(formatPercent(1, 3)).toBe('33,3 %');
    expect(formatPercent(0, 0)).toBe('0,0 %');
  });
});

describe('informe de ejemplo', () => {
  const report = CaritasReportService.sample('trimestral', '2026-09-30');
  const total = report.totals.attendedPersons;

  it('cada categoría suma el total de personas atendidas', () => {
    for (const category of reportCategories(report)) {
      expect(category.rows.reduce((s, r) => s + r.count, 0)).toBe(total);
    }
  });

  it('es repetible para el mismo período y se marca como ejemplo', () => {
    expect(CaritasReportService.sample('trimestral', '2026-09-30')).toEqual(report);
    expect(report.isSample).toBe(true);
  });

  it('el semestral tiene más personas que el trimestral', () => {
    expect(CaritasReportService.sample('semestral', '2026-09-30').totals.attendedPersons > total).toBe(true);
  });

  it('el documento exportable incluye el aviso de ejemplo, totales y las 5 categorías', () => {
    const doc = buildCaritasDocument(report, new Date(2026, 9, 14, 18, 0), 'Ana Ruiz');
    expect(doc.sections[0].title).toBe('Aviso');
    expect(doc.sections.length).toBe(1 + 1 + 5);
    expect(doc.sectionsLabel.includes('DATOS DE EJEMPLO')).toBe(true);
    expect(doc.exportInfo.includes('Ana Ruiz')).toBe(true);
  });

  it('sin ejemplo no hay aviso', () => {
    const doc = buildCaritasDocument({ ...report, isSample: false }, new Date(), 'Ana');
    expect(doc.sections[0].title).toBe('Totales del período');
    expect(caritasFilename(report)).toBe('informe-caritas-trimestral-2026-07-01-a-2026-09-30.pdf');
  });
});
