import { PersonStatus, PersonType, SocialRecordDetail } from '../interfaces/social-record.interface';
import { LifeHistory } from '../interfaces/life-history.interface';
import { Observation } from '../interfaces/observation.interface';
import { buildExportFilename, buildRecordExport } from './record-export.util';

const record = {
  id: '1', personId: 'p1', firstName: 'Ana', lastName: 'Pérez', dni: '30111222', dateOfBirth: '1980-05-02T00:00:00Z',
  phone: '1155550000', personType: PersonType.Resident, reasonForEntry: 'Motivo', entryDate: '2026-01-10',
  housingSituation: null, overnightLocation: null, occupation: null, generalNotes: 'Nota', hasDocumentation: true,
  contact: { firstName: 'Contacto Tercero', lastName: null, phone: '1100000000', address: 'Calle 123' },
  status: PersonStatus.Active, staysHistory: [],
} as unknown as SocialRecordDetail;

const lifeHistory = {
  beforeHome: { text: null, lastEditedBy: 'Usuario Secreto', lastEditedAt: null, entries: [{ id: 'e1', text: 'Vivía en la calle', authoredBy: 'Usuario Secreto', authoredAt: '2026-02-01T10:00:00Z' }] },
  duringHome: null,
  afterHome: null,
} as LifeHistory;

const observations: Observation[] = [
  { id: 'o1', socialRecordId: '1', categoryId: 'c', categoryName: 'Salud', content: 'Control médico', createdBy: 'Usuario Secreto', createdAt: '2026-03-04T10:00:00Z' },
];

const build = (sections: 'datos' | 'datos-observaciones' | 'todo') =>
  buildRecordExport({ record, lifeHistory, observations, sections, exportedAt: new Date(2026, 9, 14, 18, 30), exportedBy: 'Belén Test' });

const titles = (s: 'datos' | 'datos-observaciones' | 'todo') => build(s).sections.map((x) => x.title);
const everything = (s: 'datos' | 'datos-observaciones' | 'todo') => JSON.stringify(build(s));

describe('record-export.util', () => {
  it('solo datos: sin observaciones ni historia de vida', () => {
    expect(titles('datos')).toEqual(['Datos personales', 'Ingreso y situación']);
  });

  it('datos + observaciones', () => {
    expect(titles('datos-observaciones')).toEqual(['Datos personales', 'Ingreso y situación', 'Observaciones']);
  });

  it('todo incluye historia de vida y observaciones', () => {
    const t = titles('todo');
    expect(t.length).toBe(6);
    expect(t.includes('Observaciones')).toBe(true);
    expect(t[2].startsWith('Historia de vida')).toBe(true);
  });

  it('incluye sistema, fecha de exportación y referente', () => {
    const doc = build('datos');
    expect(doc.systemName.length > 0).toBe(true);
    expect(doc.exportInfo).toBe('Exportado el 14/10/2026 18:30 por Belén Test');
  });

  it('no incluye usuarios del sistema ni datos del contacto (terceros)', () => {
    const all = everything('todo');
    expect(all.includes('Usuario Secreto')).toBe(false);
    expect(all.includes('Contacto Tercero')).toBe(false);
    expect(all.includes('Calle 123')).toBe(false);
    expect(all.includes('1100000000')).toBe(false);
  });

  it('muestra guion en los campos vacíos', () => {
    const fields = build('datos').sections[1].fields!;
    expect(fields.find((f) => f.label === 'Ocupación')!.value).toBe('—');
  });

  it('arma el nombre de archivo sin acentos ni caracteres raros', () => {
    expect(buildExportFilename({ firstName: 'José María', lastName: 'Núñez' }, new Date(2026, 9, 14))).toBe('ficha-nunez-jose-maria-20261014.pdf');
  });
});
