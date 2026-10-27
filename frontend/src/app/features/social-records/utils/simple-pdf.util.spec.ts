import { buildPdf, toWinAnsi, wrapText } from './simple-pdf.util';
import { RecordExportDocument } from './record-export.util';

const doc = (entries: { heading: string; text: string }[] = []): RecordExportDocument => ({
  systemName: 'Sistema de prueba',
  title: 'Ficha de Ana Pérez',
  exportInfo: 'Exportado el 14/10/2026 18:30 por Belén Test',
  sectionsLabel: 'Todo',
  sections: [
    { title: 'Datos personales', fields: [{ label: 'Nombre', value: 'Ana' }, { label: 'Notas', value: 'Ñandú ¿qué? ¡sí!' }] },
    { title: 'Observaciones', entries, emptyText: 'Sin registros.' },
  ],
});

const asText = (bytes: Uint8Array) => Array.from(bytes, (b) => String.fromCharCode(b)).join('');

describe('simple-pdf.util', () => {
  it('convierte tildes y ñ a WinAnsi y reemplaza lo que no existe', () => {
    expect(toWinAnsi('á')).toBe(0xe1);
    expect(toWinAnsi('ñ')).toBe(0xf1);
    expect(toWinAnsi('¿')).toBe(0xbf);
    expect(toWinAnsi('—')).toBe(0x97);
    expect(toWinAnsi('😀')).toBe(0x3f);
  });

  it('corta el texto en líneas que entran en el ancho', () => {
    const lines = wrapText('uno dos tres cuatro cinco seis siete ocho', 'F1', 10, 60);
    expect(lines.length > 1).toBe(true);
  });

  it('parte palabras más largas que la línea', () => {
    const lines = wrapText('a'.repeat(200), 'F1', 10, 100);
    expect(lines.length > 1).toBe(true);
  });

  it('genera un PDF con cabecera, xref y fin de archivo válidos', () => {
    const text = asText(buildPdf(doc()));
    expect(text.startsWith('%PDF-1.4')).toBe(true);
    expect(text.trimEnd().endsWith('%%EOF')).toBe(true);
    expect(text.includes('xref')).toBe(true);
  });

  it('agrega páginas cuando el contenido no entra en una', () => {
    const entries = Array.from({ length: 80 }, (_, i) => ({ heading: `Entrada ${i}`, text: 'texto '.repeat(40) }));
    const text = asText(buildPdf(doc(entries)));
    const pages = (text.match(/\/Type \/Page /g) ?? []).length;
    expect(pages > 1).toBe(true);
  });
});
