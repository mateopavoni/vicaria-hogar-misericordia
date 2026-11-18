import { HELVETICA_BOLD_WIDTHS, HELVETICA_WIDTHS } from './helvetica-widths';
import { RecordExportDocument } from './record-export.util';

/**
 * Generador de PDF de texto, sin dependencias (el proyecto no trae ninguna librería
 * de PDF). Usa las fuentes estándar Helvetica / Helvetica-Bold con codificación WinAnsi, que
 * cubre el español (tildes, ñ, ¿, ¡). Soporta corte de líneas y varias páginas.
 */

const PAGE_W = 595; // A4 en puntos
const PAGE_H = 842;
const MARGIN = 50;
const CONTENT_W = PAGE_W - MARGIN * 2;
const FOOTER_Y = 30;
const BOTTOM_LIMIT = 55;

const HEADER_H = 64;
const LABEL_GAP = 14;
const ROW_H = 13;

type Font = 'F1' | 'F2';
type Rgb = readonly [number, number, number];

// paleta del documento (valores 0-1, como los espera el operador `rg` de PDF)
const COLOR_BRAND: Rgb = [0.16, 0.27, 0.52];
const COLOR_TEXT: Rgb = [0.13, 0.15, 0.2];
const COLOR_MUTED: Rgb = [0.42, 0.45, 0.52];
const COLOR_WHITE: Rgb = [1, 1, 1];
const COLOR_STRIPE: Rgb = [0.94, 0.96, 0.99];
const COLOR_RULE: Rgb = [0.78, 0.83, 0.93];

interface Line {
  font: Font;
  size: number;
  text: string;
  x: number;
  /** Posición vertical en la página (origen abajo a la izquierda). */
  y: number;
  color?: Rgb;
}

/** Rectángulo de fondo (bandas, filas alternadas, líneas de acento). */
interface Shape {
  x: number;
  y: number;
  w: number;
  h: number;
  color: Rgb;
}

interface Page {
  lines: Line[];
  shapes: Shape[];
}

interface LineSpec {
  font: Font;
  size: number;
  text: string;
  x: number;
  /** Alto que ocupa la línea (interlineado). */
  height: number;
  /** Espacio extra antes de la línea. */
  before?: number;
  color?: Rgb;
}

// Caracteres fuera de Latin-1 que sí existen en WinAnsi (cp1252)
const WIN_ANSI_EXTRA: Record<number, number> = {
  0x20ac: 0x80, 0x201a: 0x82, 0x0192: 0x83, 0x201e: 0x84, 0x2026: 0x85, 0x2020: 0x86, 0x2021: 0x87,
  0x02c6: 0x88, 0x2030: 0x89, 0x0160: 0x8a, 0x2039: 0x8b, 0x0152: 0x8c, 0x017d: 0x8e,
  0x2018: 0x91, 0x2019: 0x92, 0x201c: 0x93, 0x201d: 0x94, 0x2022: 0x95, 0x2013: 0x96, 0x2014: 0x97,
  0x02dc: 0x98, 0x2122: 0x99, 0x0161: 0x9a, 0x203a: 0x9b, 0x0153: 0x9c, 0x017e: 0x9e, 0x0178: 0x9f,
};

/** Código WinAnsi de un carácter; lo que no existe se reemplaza por '?'. */
export function toWinAnsi(char: string): number {
  const cp = char.codePointAt(0)!;
  if (cp === 0x09 || cp === 0x0a || cp === 0x0d) return 0x20;
  if (cp >= 0x20 && cp <= 0x7e) return cp;
  if (cp >= 0xa0 && cp <= 0xff) return cp;
  return WIN_ANSI_EXTRA[cp] ?? 0x3f;
}

function normalize(text: string): number[] {
  return Array.from(text.normalize('NFC'), toWinAnsi);
}

function widthOf(codes: number[], font: Font, size: number): number {
  const table = font === 'F2' ? HELVETICA_BOLD_WIDTHS : HELVETICA_WIDTHS;
  let total = 0;
  for (const c of codes) {
    total += c >= 32 ? (table[c - 32] ?? 556) : 0;
  }
  return (total * size) / 1000;
}

const fromCodes = (codes: number[]): string => String.fromCharCode(...codes);

/** Corta un texto en líneas que entren en `maxWidth`. */
export function wrapText(text: string, font: Font, size: number, maxWidth: number): string[] {
  const out: string[] = [];
  for (const paragraph of text.split(/\r?\n/)) {
    const words = paragraph.split(/\s+/).filter(Boolean);
    if (words.length === 0) {
      out.push('');
      continue;
    }
    let current: number[] = [];
    const push = () => {
      out.push(fromCodes(current));
      current = [];
    };
    for (const word of words) {
      let codes = normalize(word);
      const space = current.length ? [0x20] : [];
      if (widthOf([...current, ...space, ...codes], font, size) <= maxWidth) {
        current = [...current, ...space, ...codes];
        continue;
      }
      if (current.length) push();
      // palabra más larga que la línea: se parte
      while (widthOf(codes, font, size) > maxWidth) {
        let n = codes.length - 1;
        while (n > 1 && widthOf(codes.slice(0, n), font, size) > maxWidth) n--;
        out.push(fromCodes(codes.slice(0, n)));
        codes = codes.slice(n);
      }
      current = codes;
    }
    if (current.length) push();
  }
  return out;
}

function escapePdf(text: string): string {
  return text.replace(/\\/g, '\\\\').replace(/\(/g, '\\(').replace(/\)/g, '\\)');
}

/** Distribuye el documento en páginas de líneas ya posicionadas. */
/**
 * Ancho de la columna de etiquetas de una sección: el de la etiqueta más larga más un margen,
 * con tope para que el valor siempre tenga lugar. Si una etiqueta igual no entra, se corta en
 * varias líneas (antes la columna era fija de 150pt y las etiquetas largas quedaban debajo del valor).
 */
export function labelColumnWidth(labels: string[]): number {
  const widest = labels.reduce((max, l) => Math.max(max, widthOf(normalize(l), 'F2', 10)), 0);
  return Math.min(Math.max(widest + LABEL_GAP, 110), CONTENT_W * 0.6);
}

/** Distribuye el documento en páginas de líneas ya posicionadas. */
function layout(doc: RecordExportDocument): Page[] {
  const pages: Page[] = [{ lines: [], shapes: [] }];
  const current = () => pages[pages.length - 1];
  let y = PAGE_H - HEADER_H - 18;

  const newPage = () => {
    pages.push({ lines: [], shapes: [] });
    y = PAGE_H - MARGIN;
  };

  /** Agrega una línea (saltando de página si no entra) y devuelve su posición vertical. */
  const add = (spec: LineSpec): Line => {
    let before = spec.before ?? 0;
    if (y - (spec.height + before) < BOTTOM_LIMIT) {
      newPage();
      before = 0;
    }
    y -= spec.height + before;
    const line: Line = { font: spec.font, size: spec.size, text: spec.text, x: spec.x, y, color: spec.color };
    current().lines.push(line);
    return line;
  };

  const addWrapped = (text: string, font: Font, size: number, before = 0, color: Rgb = COLOR_TEXT) => {
    wrapText(text, font, size, CONTENT_W).forEach((t, i) =>
      add({ font, size, text: t, x: MARGIN, height: size * 1.35, before: i === 0 ? before : 0, color }),
    );
  };

  // encabezado: banda de color con el nombre del sistema, debajo título y datos de la exportación
  const first = pages[0];
  first.shapes.push({ x: 0, y: PAGE_H - HEADER_H, w: PAGE_W, h: HEADER_H, color: COLOR_BRAND });
  first.lines.push({ font: 'F2', size: 11, text: fitText(doc.systemName, 'F2', 11, CONTENT_W), x: MARGIN, y: PAGE_H - 38, color: COLOR_WHITE });

  addWrapped(doc.title, 'F2', 20, 0, COLOR_BRAND);
  addWrapped(doc.exportInfo, 'F1', 9, 4, COLOR_MUTED);
  addWrapped(`Secciones incluidas: ${doc.sectionsLabel}`, 'F1', 9, 0, COLOR_MUTED);

  // una sola columna de etiquetas para todo el documento: los valores quedan alineados entre secciones
  const labelWidth = labelColumnWidth(doc.sections.flatMap((s) => (s.fields ?? []).map((f) => f.label)));

  for (const section of doc.sections) {
    // título de sección con línea de acento debajo; si no entra junto con una fila, pasa de página
    if (y - (13 * 1.35 + 16 + ROW_H * 2) < BOTTOM_LIMIT) newPage();
    addWrapped(section.title, 'F2', 13, 16, COLOR_BRAND);
    current().shapes.push({ x: MARGIN, y: y - 5, w: CONTENT_W, h: 1.2, color: COLOR_RULE });
    y -= 6;

    const fields = section.fields ?? [];
    fields.forEach((field, index) => {
      const labelLines = wrapText(field.label, 'F2', 10, labelWidth - LABEL_GAP);
      const valueLines = wrapText(field.value || '-', 'F1', 10, CONTENT_W - labelWidth - 8);
      const rows = Math.max(labelLines.length, valueLines.length);
      const rowHeight = rows * ROW_H + 5;
      if (y - rowHeight < BOTTOM_LIMIT) newPage();

      const top = y;
      if (index % 2 === 0) {
        current().shapes.push({ x: MARGIN - 6, y: top - rowHeight, w: CONTENT_W + 12, h: rowHeight, color: COLOR_STRIPE });
      }
      for (let r = 0; r < rows; r++) {
        const baseline = top - 3 - (r + 1) * ROW_H + 3;
        if (labelLines[r]) current().lines.push({ font: 'F2', size: 10, text: labelLines[r], x: MARGIN, y: baseline, color: COLOR_TEXT });
        if (valueLines[r]) current().lines.push({ font: 'F1', size: 10, text: valueLines[r], x: MARGIN + labelWidth, y: baseline, color: COLOR_TEXT });
      }
      y = top - rowHeight;
    });

    const entries = section.entries ?? [];
    if (!section.fields && entries.length === 0 && section.emptyText) {
      addWrapped(section.emptyText, 'F1', 10, 4, COLOR_MUTED);
    }
    for (const entry of entries) {
      if (entry.heading) addWrapped(entry.heading, 'F2', 10, 8, COLOR_BRAND);
      addWrapped(entry.text, 'F1', 10, entry.heading ? 0 : 6);
    }
  }
  return pages;
}

/** Recorta un texto de una sola línea para que entre en `maxWidth` (agrega "..."). */
function fitText(text: string, font: Font, size: number, maxWidth: number): string {
  const codes = normalize(text);
  if (widthOf(codes, font, size) <= maxWidth) return fromCodes(codes);
  let n = codes.length;
  while (n > 0 && widthOf([...codes.slice(0, n), 0x2e, 0x2e, 0x2e], font, size) > maxWidth) n--;
  return fromCodes([...codes.slice(0, n), 0x2e, 0x2e, 0x2e]);
}

const rgb = (c: Rgb) => c.map((v) => v.toFixed(3)).join(' ');

/** Genera el PDF y lo devuelve como bytes. */
export function buildPdf(doc: RecordExportDocument): Uint8Array {
  const pages = layout(doc);
  const total = pages.length;
  const footer = `${doc.systemName} - ${doc.exportInfo}`;

  const objects: string[] = [];
  // 1 catálogo, 2 páginas, 3 y 4 fuentes; luego (página, contenido) por cada página
  const pageObjIds = pages.map((_, i) => 5 + i * 2);

  objects[1] = '<< /Type /Catalog /Pages 2 0 R >>';
  objects[2] = `<< /Type /Pages /Kids [${pageObjIds.map((id) => `${id} 0 R`).join(' ')}] /Count ${total} >>`;
  objects[3] = '<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>';
  objects[4] = '<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>';

  pages.forEach((page, i) => {
    const ops: string[] = [];
    // fondos primero, para que el texto quede por encima
    for (const s of page.shapes) {
      ops.push(`q ${rgb(s.color)} rg ${s.x.toFixed(2)} ${s.y.toFixed(2)} ${s.w.toFixed(2)} ${s.h.toFixed(2)} re f Q`);
    }
    ops.push(`q ${rgb(COLOR_RULE)} rg ${MARGIN} ${FOOTER_Y + 12} ${CONTENT_W} 0.8 re f Q`);

    ops.push('BT');
    const text = (font: Font, size: number, x: number, y: number, str: string, color: Rgb = COLOR_TEXT) =>
      ops.push(`${rgb(color)} rg /${font} ${size} Tf 1 0 0 1 ${x.toFixed(2)} ${y.toFixed(2)} Tm (${escapePdf(str)}) Tj`);

    for (const line of page.lines) text(line.font, line.size, line.x, line.y, line.text, line.color);

    // pie: texto del sistema a la izquierda y número de página a la derecha, en la misma línea
    const pageLabel = fromCodes(normalize(`Página ${i + 1} de ${total}`));
    const pageLabelWidth = widthOf(normalize(pageLabel), 'F1', 8);
    text('F1', 8, MARGIN, FOOTER_Y, fitText(footer, 'F1', 8, CONTENT_W - pageLabelWidth - 16), COLOR_MUTED);
    text('F1', 8, PAGE_W - MARGIN - pageLabelWidth, FOOTER_Y, pageLabel, COLOR_MUTED);
    ops.push('ET');

    const stream = ops.join('\n');
    const pageId = pageObjIds[i];
    objects[pageId] =
      `<< /Type /Page /Parent 2 0 R /MediaBox [0 0 ${PAGE_W} ${PAGE_H}] ` +
      `/Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents ${pageId + 1} 0 R >>`;
    objects[pageId + 1] = `<< /Length ${stream.length} >>\nstream\n${stream}\nendstream`;
  });

  // Todo el contenido son códigos 0-255, así que 1 carácter = 1 byte y los offsets son exactos
  let body = '%PDF-1.4\n';
  const offsets: number[] = [];
  for (let id = 1; id < objects.length; id++) {
    offsets[id] = body.length;
    body += `${id} 0 obj\n${objects[id]}\nendobj\n`;
  }
  const xrefAt = body.length;
  body += `xref\n0 ${objects.length}\n0000000000 65535 f \n`;
  for (let id = 1; id < objects.length; id++) {
    body += `${String(offsets[id]).padStart(10, '0')} 00000 n \n`;
  }
  body += `trailer\n<< /Size ${objects.length} /Root 1 0 R >>\nstartxref\n${xrefAt}\n%%EOF\n`;

  const bytes = new Uint8Array(body.length);
  for (let i = 0; i < body.length; i++) bytes[i] = body.charCodeAt(i) & 0xff;
  return bytes;
}
