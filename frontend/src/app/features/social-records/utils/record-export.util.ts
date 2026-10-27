import { LifeHistory, LifeHistorySection } from '../interfaces/life-history.interface';
import { Observation } from '../interfaces/observation.interface';
import { PersonStatus, PersonType, SocialRecordDetail } from '../interfaces/social-record.interface';

/**
 * SCRUM-253: qué secciones incluye la ficha exportada.
 * - 'datos': solo datos personales
 * - 'datos-observaciones': datos + observaciones
 * - 'todo': datos + historia de vida + observaciones
 */
export type ExportSections = 'datos' | 'datos-observaciones' | 'todo';

export const EXPORT_SECTION_OPTIONS: readonly { id: ExportSections; label: string; description: string }[] = [
  { id: 'datos', label: 'Solo datos personales', description: 'Datos de la ficha, sin observaciones ni historia de vida.' },
  { id: 'datos-observaciones', label: 'Datos y observaciones', description: 'Datos de la ficha más las observaciones registradas.' },
  { id: 'todo', label: 'Todo', description: 'Datos, historia de vida y observaciones.' },
];

export const SYSTEM_NAME = 'Vicaria | Nuestra Señora de la Misericordia';

export interface ExportField {
  label: string;
  value: string;
}

export interface ExportEntry {
  heading: string;
  text: string;
}

export interface ExportSection {
  title: string;
  fields?: ExportField[];
  entries?: ExportEntry[];
  /** Se muestra si la sección no tiene contenido. */
  emptyText?: string;
}

/** Documento neutro: de acá salen tanto el PDF descargable como la vista de impresión. */
export interface RecordExportDocument {
  systemName: string;
  title: string;
  /** "Exportado el 14/10/2026 18:30 por Nombre Apellido" */
  exportInfo: string;
  sectionsLabel: string;
  sections: ExportSection[];
}

export interface BuildExportInput {
  record: SocialRecordDetail;
  lifeHistory: LifeHistory | null;
  observations: Observation[];
  sections: ExportSections;
  exportedAt: Date;
  /** Nombre del referente (usuario logueado) que genera el archivo. */
  exportedBy: string;
}

const EMPTY = '—';

const pad = (n: number) => String(n).padStart(2, '0');

export function formatDate(iso: string | null | undefined): string {
  if (!iso) return EMPTY;
  const [y, m, d] = iso.slice(0, 10).split('-');
  return y && m && d ? `${d}/${m}/${y}` : EMPTY;
}

export function formatDateTime(date: Date): string {
  return `${pad(date.getDate())}/${pad(date.getMonth() + 1)}/${date.getFullYear()} ${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

const orEmpty = (value: string | null | undefined): string => (value && value.trim() ? value.trim() : EMPTY);

/**
 * Arma el documento a exportar.
 * Privacidad (AC): no se incluyen datos de otros usuarios del sistema (autores de observaciones o
 * de la historia de vida, historial de cambios) ni de terceros (el contacto de la ficha).
 */
export function buildRecordExport(input: BuildExportInput): RecordExportDocument {
  const { record, lifeHistory, observations, sections, exportedAt, exportedBy } = input;
  const fullName = `${record.firstName} ${record.lastName ?? ''}`.trim();

  const result: ExportSection[] = [
    {
      title: 'Datos personales',
      fields: [
        { label: 'Nombre', value: orEmpty(record.firstName) },
        { label: 'Apellido', value: orEmpty(record.lastName) },
        { label: 'DNI', value: orEmpty(record.dni) },
        { label: 'Fecha de nacimiento', value: formatDate(record.dateOfBirth) },
        { label: 'Teléfono', value: orEmpty(record.phone) },
        { label: 'Tipo de persona', value: personTypeLabel(record.personType) },
        { label: 'Estado', value: record.status === PersonStatus.Active ? 'Activo' : 'Inactivo' },
      ],
    },
    {
      title: 'Ingreso y situación',
      fields: [
        { label: 'Fecha de ingreso', value: formatDate(record.entryDate) },
        { label: 'Motivo de ingreso', value: orEmpty(record.reasonForEntry) },
        { label: 'Situación habitacional', value: orEmpty(record.housingSituation) },
        { label: 'Lugar donde pernocta', value: orEmpty(record.overnightLocation) },
        { label: 'Ocupación', value: orEmpty(record.occupation) },
        { label: 'Posee documentación', value: record.hasDocumentation ? 'Sí' : 'No' },
        { label: 'Notas generales', value: orEmpty(record.generalNotes) },
      ],
    },
  ];

  if (sections === 'todo') {
    result.push(
      lifeHistoryBlock('Antes del Centro Barrial', lifeHistory?.beforeHome ?? null),
      lifeHistoryBlock('En el Centro Barrial', lifeHistory?.duringHome ?? null),
      lifeHistoryBlock('Después del Centro Barrial', lifeHistory?.afterHome ?? null),
    );
  }

  if (sections !== 'datos') {
    result.push({
      title: 'Observaciones',
      entries: observations.map((o) => ({
        heading: `${formatDate(o.createdAt)} · ${o.categoryName}`,
        text: o.content,
      })),
      emptyText: 'No hay observaciones registradas.',
    });
  }

  return {
    systemName: SYSTEM_NAME,
    title: `Ficha de ${fullName}`,
    exportInfo: `Exportado el ${formatDateTime(exportedAt)} por ${exportedBy}`,
    sectionsLabel: EXPORT_SECTION_OPTIONS.find((o) => o.id === sections)!.label,
    sections: result,
  };
}

function lifeHistoryBlock(title: string, section: LifeHistorySection | null): ExportSection {
  const entries = (section?.entries ?? []).map((e) => ({ heading: formatDate(e.authoredAt), text: e.text }));
  if (entries.length === 0 && section?.text?.trim()) {
    entries.push({ heading: '', text: section.text.trim() });
  }
  return { title: `Historia de vida · ${title}`, entries, emptyText: 'Sin registros.' };
}

function personTypeLabel(type: PersonType | null): string {
  if (type === PersonType.Ambulatory) return 'Ambulatorio';
  if (type === PersonType.Resident) return 'Residente';
  return EMPTY;
}

/** Nombre del archivo: ficha-apellido-nombre-AAAAMMDD.pdf, sin acentos ni caracteres raros. */
export function buildExportFilename(record: Pick<SocialRecordDetail, 'firstName' | 'lastName'>, date: Date): string {
  const slug = `${record.lastName ?? ''} ${record.firstName}`
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
  return `ficha-${slug || 'persona'}-${date.getFullYear()}${pad(date.getMonth() + 1)}${pad(date.getDate())}.pdf`;
}
