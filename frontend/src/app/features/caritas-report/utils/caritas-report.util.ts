import { RecordExportDocument, SYSTEM_NAME, formatDate, formatDateTime } from '../../social-records/utils/record-export.util';
import { CaritasReport, CountItem, PERIOD_LABELS, PERIOD_MONTHS, ReportPeriodType, ReportRange } from '../interfaces/caritas-report.interface';

const pad = (n: number) => String(n).padStart(2, '0');
const toIso = (d: Date) => `${d.getUTCFullYear()}-${pad(d.getUTCMonth() + 1)}-${pad(d.getUTCDate())}`;

/**
 * Rango del informe: los últimos 3 o 6 meses que terminan en `to` (inclusive).
 * Ej.: trimestral hasta 2026-09-30 → 2026-07-01 a 2026-09-30.
 */
export function periodRange(type: ReportPeriodType, to: string): ReportRange {
  const end = new Date(`${to}T00:00:00Z`);
  const nextDay = new Date(end);
  nextDay.setUTCDate(end.getUTCDate() + 1);
  if (nextDay.getUTCDate() === 1) {
    // fin de mes: el período empieza el día 1 del mes que corresponde (ej. 31/03 semestral → 01/10)
    const first = new Date(Date.UTC(end.getUTCFullYear(), end.getUTCMonth() - (PERIOD_MONTHS[type] - 1), 1));
    return { from: toIso(first), to };
  }
  // desde el día siguiente al mismo día del mes que está N meses atrás (ajustado si ese mes es más corto)
  const target = new Date(end);
  target.setUTCDate(1);
  target.setUTCMonth(target.getUTCMonth() - PERIOD_MONTHS[type]);
  const lastDayOfTargetMonth = new Date(Date.UTC(target.getUTCFullYear(), target.getUTCMonth() + 1, 0)).getUTCDate();
  target.setUTCDate(Math.min(end.getUTCDate(), lastDayOfTargetMonth));
  target.setUTCDate(target.getUTCDate() + 1);
  return { from: toIso(target), to };
}

/** "40,0 %" — con coma decimal; si el total es 0 devuelve "0,0 %". */
export function formatPercent(count: number, total: number): string {
  const value = total > 0 ? (count / total) * 100 : 0;
  return `${value.toFixed(1).replace('.', ',')} %`;
}

export function percentNumber(count: number, total: number): number {
  return total > 0 ? Math.round((count / total) * 1000) / 10 : 0;
}

export function formatDecimal(value: number): string {
  return value.toFixed(1).replace('.', ',');
}

export interface ReportRow {
  label: string;
  count: number;
  percent: string;
  /** 0-100, para el ancho de la barra */
  percentValue: number;
}

export function toRows(items: CountItem[]): ReportRow[] {
  const total = items.reduce((sum, i) => sum + i.count, 0);
  return items.map((i) => ({
    label: i.label,
    count: i.count,
    percent: formatPercent(i.count, total),
    percentValue: percentNumber(i.count, total),
  }));
}

export interface ReportCategory {
  title: string;
  rows: ReportRow[];
}

/** Las categorías del informe, en el orden del criterio de aceptación. */
export function reportCategories(report: CaritasReport): ReportCategory[] {
  return [
    { title: 'Género', rows: toRows(report.gender) },
    { title: 'Situación de vivienda', rows: toRows(report.housing) },
    { title: 'Documentación', rows: toRows(report.dni) },
    { title: 'Distribución geográfica (de dónde son)', rows: toRows(report.geography) },
    { title: 'Motivo de asistencia', rows: toRows(report.reasons) },
  ];
}

export const SAMPLE_WARNING = 'DATOS DE EJEMPLO: el informe todavía no está conectado al backend. No usar para la rendición a Cáritas.';

/** Documento neutro para el PDF y la vista de impresión (reusa el generador de la ficha). */
export function buildCaritasDocument(report: CaritasReport, generatedAt: Date, generatedBy: string): RecordExportDocument {
  const sections: RecordExportDocument['sections'] = [];

  if (report.isSample) {
    sections.push({ title: 'Aviso', entries: [{ heading: '', text: SAMPLE_WARNING }] });
  }

  sections.push({
    title: 'Totales del período',
    fields: [
      { label: 'Personas atendidas', value: String(report.totals.attendedPersons) },
      { label: 'Promedio de asistencias por persona', value: formatDecimal(report.totals.averageAttendancesPerPerson) },
      { label: 'Nuevas altas', value: String(report.totals.newRegistrations) },
    ],
  });

  for (const category of reportCategories(report)) {
    sections.push({
      title: category.title,
      fields: category.rows.map((r) => ({ label: r.label, value: `${r.count} (${r.percent})` })),
    });
  }

  return {
    systemName: SYSTEM_NAME,
    title: `Informe estadístico para Cáritas - ${report.periodType === 'trimestral' ? 'Trimestral' : 'Semestral'}`,
    exportInfo: `Generado el ${formatDateTime(generatedAt)} por ${generatedBy}`,
    sectionsLabel: `Período ${formatDate(report.from)} al ${formatDate(report.to)}${report.isSample ? ' - DATOS DE EJEMPLO' : ''}`,
    sections,
  };
}

export function caritasFilename(report: Pick<CaritasReport, 'periodType' | 'from' | 'to'>): string {
  return `informe-caritas-${report.periodType}-${report.from}-a-${report.to}.pdf`;
}

export { PERIOD_LABELS };
