// Sprint 5: ejecución de los casos de interfaz TC-212 a TC-261 con navegador real (Playwright), contra el
// stack local de backend/docker-compose.yml. Cada caso maneja la pantalla y verifica además contra la API;
// los estados que la interfaz no puede fabricar (evaluación de hace 6 meses, persona sin asistir hace 31 días,
// respaldos guardados) se preparan por API o por SQL en el contenedor. Las personas se crean en cada corrida.
//   BASE=http://localhost:4200 node sprint5-ui-cases.mjs   (requiere `playwright` instalado donde se corra)
// Un caso puede quedar BLOQUEADO (blocked) cuando su precondición no se puede dar en el stack local.
import { chromium } from 'playwright';
import { execFileSync } from 'child_process';
import fs from 'fs';

const BASE = process.env.BASE || 'http://localhost:4200';
const SHOTS = process.env.SHOTS || './shots-s5ui';
const OUT = process.env.OUT || './sprint5-ui-results.json';
fs.mkdirSync(SHOTS, { recursive: true });
const PASS = 'Test1234!';
const TAG = Date.now().toString(36);
const res = [];
const sleep = ms => new Promise(r => setTimeout(r, ms));

const call = async (method, path, { token, body, form } = {}) => {
  const h = {}; if (token) h.Authorization = `Bearer ${token}`;
  if (body !== undefined) h['Content-Type'] = 'application/json';
  const r = await fetch(BASE + path, { method, headers: h, body: form ?? (body === undefined ? undefined : JSON.stringify(body)) });
  const buf = Buffer.from(await r.arrayBuffer()); const text = buf.toString('utf8');
  let json = null; try { json = JSON.parse(text); } catch { /* no es JSON */ }
  return { status: r.status, json, text, buf, headers: r.headers };
};
const login = async e => (await call('POST', '/api/auth/login', { body: { email: e, password: PASS } })).json?.token;
const tokens = async () => ({ ref: await login('referente@test.com'), dir: await login('directora@test.com'), esc: await login('escucha@test.com'), coo: await login('coordinador@test.com') });
let T = await tokens();

const sql = q => execFileSync('docker', ['exec', 'vicaria-sqlserver', 'bash', '-c',
  `/opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -d VicariaDb -h -1 -W -Q "SET NOCOUNT ON; ${q.replace(/"/g, '\\"')}"`],
  { encoding: 'utf8', env: { ...process.env, MSYS_NO_PATHCONV: '1' } }).trim();

const ar = (offsetDays = 0) => new Date(Date.now() - 3 * 36e5 + offsetDays * 864e5);
const iso = d => d.toISOString().slice(0, 10);
const today = iso(ar());
const monthsAgo = (months, plusDays = 0) => { const d = ar(); d.setUTCMonth(d.getUTCMonth() - months); d.setUTCDate(d.getUTCDate() + plusDays); return iso(d); };

const newPerson = async (firstName, extra = {}) => {
  const r = await call('POST', '/api/social-records', { token: T.ref, body: { firstName, lastName: `UI ${TAG}`, personType: 0, ...extra } });
  if (!r.json?.id) throw new Error(`no se pudo crear la ficha ${firstName}: ${r.status} ${r.text.slice(0, 120)}`);
  return { recordId: r.json.id, personId: r.json.personId, name: `${firstName} UI ${TAG}` };
};
const detail = async (recordId) => (await call('GET', `/api/social-records/${recordId}`, { token: T.ref })).json;
const evalPost = (p, o, tok = T.ref) => call('POST', `/api/persons/${p}/psychiatric-evaluations`, { token: tok, body: { date: today, professional: 'Dra. Gómez', diagnosis: 'Diagnóstico de prueba', ...o } });

// ---------- navegador ----------
const browser = await chromium.launch();
const mkPage = async email => {
  const ctx = await browser.newContext({ viewport: { width: 1400, height: 1000 }, acceptDownloads: true });
  const page = await ctx.newPage();
  await page.goto(BASE + '/auth'); await page.waitForSelector('input[formControlName="email"]');
  await page.fill('input[formControlName="email"]', email); await page.fill('input[formControlName="password"]', PASS);
  await page.click('button[type="submit"]'); await page.waitForURL(/dashboard/, { timeout: 15000 });
  return page;
};
const P = { ref: await mkPage('referente@test.com'), esc: await mkPage('escucha@test.com') };
const body = async p => (await p.innerText('body')).replace(/\s+/g, ' ');
const go = async (p, path, wait = 1800) => { await p.goto(BASE + path); await sleep(wait); };
const shot = (p, id) => p.screenshot({ path: `${SHOTS}/${id}.png` }).catch(() => {});
const expectText = (txt, re, what) => { if (!re.test(txt)) throw new Error(`no se vio ${what ?? re}`); };
const must = (cond, msg) => { if (!cond) throw new Error(msg); };
class Blocked extends Error {}

const tc = async (n, title, page, fn) => {
  const id = `TC-${n}`;
  let ok = true, blocked = false, note = '';
  try { note = (await fn()) ?? ''; } catch (e) { ok = false; blocked = e instanceof Blocked; note = e.message.split('\n')[0].slice(0, 260); }
  if (page) await shot(page, id);
  res.push({ id, title, ok, ...(blocked ? { blocked: true } : {}), note });
  console.log(`${ok ? 'PASS' : blocked ? 'BLOQ' : 'FAIL'}  ${id}  ${title}${note ? ' — ' + note : ''}`);
};
const openTab = async (p, tab, recordId) => {
  await go(p, `/dashboard/fichas/${recordId}`);
  await p.getByRole('button', { name: tab, exact: true }).first().click(); await sleep(1500);
};
// texto de un PDF (el de la ficha lo genera el backend con PDFsharp: se lee con PyMuPDF)
const pdfText = (file) => execFileSync('python', ['-c', 'import fitz,sys;print("\\n".join(p.get_text() for p in fitz.open(sys.argv[1])))', file], { encoding: 'utf8', env: { ...process.env, PYTHONIOENCODING: 'utf-8' } });
// descarga el PDF de la ficha con las secciones elegidas y devuelve su texto
const exportRecord = async (recordId, label) => {
  await go(P.ref, `/dashboard/fichas/${recordId}`);
  await P.ref.getByRole('button', { name: /Exportar Ficha/i }).first().click(); await sleep(600);
  await P.ref.getByText(label, { exact: true }).click();
  const [dl] = await Promise.all([P.ref.waitForEvent('download', { timeout: 15000 }), P.ref.getByRole('button', { name: 'Descargar PDF' }).click()]);
  const file = await dl.path();
  return { name: dl.suggestedFilename(), text: pdfText(file) };
};

// ======================= datos de la corrida =======================
const X = await newPerson('Exporta', { dni: '30111222', generalNotes: 'Nota general de prueba', contact: { firstName: 'Contacto', lastName: 'Tercero', phone: '3515550000', address: 'Calle Falsa 123' } });
await call('PUT', `/api/persons/${X.personId}/life-story/before-centro-barrial`, { token: T.ref, body: { content: 'Relato de infancia de prueba' } });
const cats = (await call('GET', '/api/observation-categories?onlyActive=true', { token: T.ref })).json ?? [];
await call('POST', `/api/persons/${X.personId}/observations`, { token: T.ref, body: { categoryId: cats[0]?.id, content: 'Observación cargada por la Referente' } });
await call('POST', `/api/persons/${X.personId}/observations`, { token: T.esc, body: { categoryId: cats[0]?.id, content: 'Observación cargada por la Escucha' } });

// ======================= SCRUM-23 exportar / imprimir =======================
await tc(212, 'Exportar la ficha con solo los datos personales', P.ref, async () => {
  const { name, text } = await exportRecord(X.recordId, 'Solo datos personales');
  must(/Datos personales/.test(text) && /Exporta/.test(text), 'el PDF no trae los datos de la ficha');
  must(!/Observaciones/.test(text) && !/Historia de vida/.test(text), 'el PDF trae secciones que no se eligieron');
  return name;
});
await tc(213, 'Exportar la ficha con datos y observaciones', P.ref, async () => {
  const { text } = await exportRecord(X.recordId, 'Datos y observaciones');
  must(/Observaciones/.test(text) && /cargada por la Referente/.test(text), 'faltan las observaciones');
  must(!/Historia de vida/.test(text), 'trae la historia de vida sin haberla elegido');
  return 'datos + observaciones, sin historia de vida';
});
await tc(214, 'Exportar la ficha completa', P.ref, async () => {
  const { text } = await exportRecord(X.recordId, 'Todo');
  must(/Historia de vida/.test(text) && /Relato de infancia/.test(text), 'falta la historia de vida');
  must(/Observaciones/.test(text), 'faltan las observaciones');
  return 'ficha, historia de vida y observaciones';
});
await tc(215, 'El PDF lleva nombre del sistema, fecha de exportación y referente', P.ref, async () => {
  const { text } = await exportRecord(X.recordId, 'Solo datos personales');
  const [y, m, d] = today.split('-');
  must(/Vicaria/.test(text), 'falta el nombre del sistema');
  must(text.includes(`Fecha de exportación: ${d}/${m}/${y}`), 'falta la fecha de exportación');
  must(/Generado por: Test referente/i.test(text), 'falta el nombre de quien exportó');
  return `Fecha de exportación: ${d}/${m}/${y}, generado por Test referente`;
});
await tc(216, 'El PDF no incluye datos de otros usuarios ni de terceros', P.ref, async () => {
  const { text } = await exportRecord(X.recordId, 'Todo');
  must(!/@test\.com/.test(text), 'aparece el email de un usuario');
  must(!/Calle Falsa|3515550000|Tercero/.test(text), 'aparecen datos del contacto (tercero)');
  return 'sin emails de usuarios ni datos del contacto';
});
await tc(217, 'La ficha se descarga directo y se puede imprimir', P.ref, async () => {
  const { name } = await exportRecord(X.recordId, 'Todo');
  must(/\.pdf$/.test(name), `el archivo descargado es ${name}`);
  await go(P.ref, `/dashboard/fichas/${X.recordId}`);
  await P.ref.evaluate(() => { window.__printed = 0; window.print = () => { window.__printed++; }; });
  await P.ref.getByRole('button', { name: /Exportar Ficha/i }).first().click(); await sleep(500);
  await P.ref.getByRole('button', { name: 'Imprimir' }).click(); await sleep(1200);
  must(await P.ref.evaluate(() => window.__printed) > 0, 'no se abrió la vista de impresión');
  return `${name} y vista de impresión`;
});
await tc(218, 'Queda registrado quién exportó la ficha y cuándo', P.ref, async () => {
  const exportLogs = () => sql(`SELECT u.Email + '|' + a.Action FROM audit_logs a JOIN users u ON u.Id = a.UserId WHERE a.Action LIKE 'Ficha exportada%' AND a.AffectedEntity = 'Person:${X.personId}' ORDER BY a.Date DESC`).split('\n').filter(Boolean);
  const before = exportLogs().length;
  await exportRecord(X.recordId, 'Solo datos personales');
  await sleep(1000);
  const logs = exportLogs();
  must(logs.length > before, 'la exportación no queda registrada en la auditoría');
  must(logs[0].startsWith('referente@test.com|') && /solo datos personales/.test(logs[0]), `el registro no indica usuario y secciones: ${logs[0]}`);
  return logs[0].split('|')[1];
});

// ======================= SCRUM-24 adjuntos =======================
const A = await newPerson('Adjuntos');
const tmp = `${SHOTS}/tmp-${TAG}`; fs.mkdirSync(tmp, { recursive: true });
const pngBytes = Buffer.from('89504e470d0a1a0a0000000d49484452000000010000000108060000001f15c4890000000d4944415478da63f8ffff3f0005fe02fea7d6a4ea0000000049454e44ae426082', 'hex');
fs.writeFileSync(`${tmp}/foto.jpg`, Buffer.from('ffd8ffe000104a46494600010100000100010000ffd9', 'hex'));
const realPng = await P.ref.screenshot({ clip: { x: 0, y: 0, width: 40, height: 40 } });
fs.writeFileSync(`${tmp}/recibo.png`, realPng);
fs.writeFileSync(`${tmp}/constancia.pdf`, '%PDF-1.4\n%%EOF\n');
fs.writeFileSync(`${tmp}/notas.txt`, 'hola');
fs.writeFileSync(`${tmp}/grande.pdf`, Buffer.concat([Buffer.from('%PDF-1.4\n'), Buffer.alloc(11 * 1024 * 1024, 0x20)]));
fs.writeFileSync(`${tmp}/nueve.pdf`, Buffer.concat([Buffer.from('%PDF-1.4\n'), Buffer.alloc(9 * 1024 * 1024, 0x20)]));
fs.writeFileSync(`${tmp}/multi-a.jpg`, Buffer.from('ffd8ffe000104a46494600010100000100010000ffd9', 'hex'));
fs.writeFileSync(`${tmp}/multi-b.png`, realPng);
fs.writeFileSync(`${tmp}/multi-c.pdf`, '%PDF-1.4\n%%EOF\n');
const upload = async (files, wait = 2500) => { await P.ref.setInputFiles('input[type="file"]', files.map(f => `${tmp}/${f}`)); await sleep(wait); };
const attachments = async () => (await call('GET', `/api/persons/${A.personId}/attachments`, { token: T.ref })).json ?? [];

await tc(219, 'Adjuntar una imagen JPG y una PNG a la ficha', P.ref, async () => {
  await openTab(P.ref, 'Documentos', A.recordId);
  await upload(['foto.jpg']); await upload(['recibo.png']);
  const t = await body(P.ref); expectText(t, /foto\.jpg/, 'foto.jpg en el listado'); expectText(t, /recibo\.png/, 'recibo.png en el listado');
  const list = await attachments(); must(list.some(a => a.fileName === 'foto.jpg') && list.some(a => a.fileName === 'recibo.png'), 'el backend no tiene los archivos');
  return `${list.length} adjuntos en el backend`;
});
await tc(220, 'Adjuntar un documento PDF', P.ref, async () => {
  await upload(['constancia.pdf']);
  expectText(await body(P.ref), /constancia\.pdf/, 'el PDF en el listado');
  must(await P.ref.locator('[aria-label="PDF"]').count() > 0, 'no se ve el ícono de documento');
  return 'con ícono de PDF';
});
await tc(221, 'Rechazar un tipo de archivo no permitido', P.ref, async () => {
  await upload(['notas.txt'], 1200);
  expectText(await body(P.ref), /solo se aceptan/i, 'el mensaje de tipo no permitido');
  must(!(await attachments()).some(a => a.fileName === 'notas.txt'), 'el archivo se cargó igual');
  const api = await call('POST', `/api/persons/${A.personId}/attachments`, { token: T.ref, form: (() => { const f = new FormData(); f.append('files', new Blob(['hola'], { type: 'text/plain' }), 'notas.txt'); return f; })() });
  must(api.status === 400, `la API respondió ${api.status}`);
  return 'rechazado en pantalla y por API (400)';
});
await tc(222, 'Cada adjunto muestra nombre, fecha, usuario e ícono según el tipo', P.ref, async () => {
  await openTab(P.ref, 'Documentos', A.recordId);
  const t = await body(P.ref);
  const [y, m, d] = today.split('-');
  expectText(t, new RegExp(`Cargado el ${d}/${m}/${y} \\d\\d:\\d\\d por Test referente`), 'la fecha y el usuario');
  must(await P.ref.locator('[aria-label="Imagen"]').count() >= 2 && await P.ref.locator('[aria-label="PDF"]').count() >= 1, 'faltan los íconos por tipo');
  return 'nombre, fecha, usuario e ícono';
});
await tc(223, 'Rechazar un archivo de más de 10 MB', P.ref, async () => {
  await upload(['grande.pdf'], 1500);
  expectText(await body(P.ref), /10 MB/, 'el error de tamaño');
  must(!(await attachments()).some(a => a.fileName === 'grande.pdf'), 'el archivo grande se cargó');
  await upload(['nueve.pdf'], 6000);
  must((await attachments()).some(a => a.fileName === 'nueve.pdf'), 'el archivo de 9 MB no se cargó');
  return '11 MB rechazado, 9 MB aceptado';
});
await tc(224, 'Adjuntar varios archivos en una sola carga', P.ref, async () => {
  await upload(['multi-a.jpg', 'multi-b.png', 'multi-c.pdf'], 3500);
  const t = await body(P.ref);
  for (const f of ['multi-a.jpg', 'multi-b.png', 'multi-c.pdf']) expectText(t, new RegExp(f.replace('.', '\\.')), f);
  expectText(t, /3 archivos adjuntados/, 'el mensaje de carga múltiple');
  return `${(await attachments()).length} adjuntos en la ficha`;
});
await tc(225, 'Ver una imagen en el sistema y descargar un PDF', P.ref, async () => {
  await openTab(P.ref, 'Documentos', A.recordId);
  const row = P.ref.locator('li', { hasText: 'recibo.png' });
  await row.getByRole('button', { name: 'Ver' }).click(); await sleep(1000);
  const img = P.ref.locator('img[alt="recibo.png"]');
  const visible = await img.count() > 0 && await img.evaluate(i => i.complete && i.naturalWidth > 0);
  await shot(P.ref, 'TC-225-visor');
  await P.ref.getByRole('button', { name: 'Cerrar', exact: true }).last().click(); await sleep(400);
  must(visible, 'la imagen no se ve en el visor');
  const [dl] = await Promise.all([P.ref.waitForEvent('download'), P.ref.locator('li', { hasText: 'constancia.pdf' }).getByText('Descargar').click()]);
  must(dl.suggestedFilename() === 'constancia.pdf', `se descargó ${dl.suggestedFilename()}`);
  return 'visor de imagen y descarga de constancia.pdf';
});
await tc(226, 'Eliminar un adjunto como Referente con confirmación', P.ref, async () => {
  const row = () => P.ref.locator('li', { hasText: 'multi-c.pdf' });
  await row().getByRole('button', { name: 'Eliminar' }).click(); await sleep(500);
  expectText(await body(P.ref), /Seguro que querés eliminar/, 'la confirmación');
  await P.ref.getByRole('button', { name: 'Cancelar' }).last().click(); await sleep(500);
  must(await row().count() > 0, 'se borró al cancelar');
  await row().getByRole('button', { name: 'Eliminar' }).click(); await sleep(500);
  await P.ref.getByRole('button', { name: 'Eliminar' }).last().click(); await sleep(1800);
  must(await row().count() === 0, 'sigue en el listado');
  must(!(await attachments()).some(a => a.fileName === 'multi-c.pdf'), 'sigue en el backend');
  return 'cancelar no borra; confirmar borra';
});
await tc(227, 'Solo el Referente puede eliminar adjuntos', P.esc, async () => {
  await go(P.esc, `/dashboard/fichas/${A.recordId}`);
  const tab = P.esc.getByRole('button', { name: 'Documentos', exact: true });
  if (await tab.count()) { await tab.first().click(); await sleep(1500); }
  must(await P.esc.getByRole('button', { name: 'Eliminar' }).count() === 0, 'la Escucha ve Eliminar');
  const target = (await attachments()).find(a => a.fileName === 'recibo.png');
  const api = await call('DELETE', `/api/persons/${A.personId}/attachments/${target.id}`, { token: T.esc });
  must(api.status === 403, `la API respondió ${api.status}`);
  return 'sin botón Eliminar y 403 por API';
});

// ======================= SCRUM-66 evaluación psiquiátrica =======================
const E = await newPerson('Evaluacion');
const evStatus = async p => (await call('GET', `/api/persons/${p}/psychiatric-evaluations/status`, { token: T.ref })).json;
const fillEval = async (p, { date, professional, diagnosis }) => {
  await p.getByRole('button', { name: 'Registrar evaluación' }).first().click(); await sleep(600);
  if (date !== undefined) await p.fill('#eval-date', date);
  await p.fill('#eval-professional', professional); await p.fill('#eval-diagnosis', diagnosis);
  await p.getByRole('button', { name: 'Guardar' }).last().click(); await sleep(1800);
};
await tc(228, 'Registrar una evaluación psiquiátrica con sus campos', P.ref, async () => {
  await openTab(P.ref, 'Evaluaciones', E.recordId);
  await fillEval(P.ref, { date: iso(ar(-3)), professional: 'Dra. Gómez', diagnosis: 'Diagnóstico inicial' });
  const t = await body(P.ref); expectText(t, /Dra\. Gómez/, 'la evaluación cargada'); expectText(t, /Vence el/, 'la fecha de vencimiento');
  const st = await evStatus(E.personId); must(st?.hasEvaluation && st.professional === 'Dra. Gómez', 'el backend no la tiene como vigente');
  return `vence el ${st.expiresAt.slice(0, 10)}`;
});
await tc(229, 'Validar los campos obligatorios de la evaluación', P.ref, async () => {
  await openTab(P.ref, 'Evaluaciones', E.recordId);
  await fillEval(P.ref, { professional: '', diagnosis: 'Sin profesional' });
  expectText(await body(P.ref), /profesional/i, 'el aviso de profesional');
  await P.ref.getByRole('button', { name: 'Cancelar' }).last().click().catch(() => {}); await sleep(400);
  await fillEval(P.ref, { date: iso(ar(1)), professional: 'Dr. Futuro', diagnosis: 'Fecha futura' });
  expectText(await body(P.ref), /futura|fecha/i, 'el aviso de fecha futura');
  await P.ref.getByRole('button', { name: 'Cancelar' }).last().click().catch(() => {});
  must((await evStatus(E.personId)).professional === 'Dra. Gómez', 'se guardó una evaluación inválida');
  const api = await evalPost(E.personId, { date: iso(ar(1)) });
  must(api.status === 400, `la API respondió ${api.status} a la fecha futura`);
  return 'sin profesional y con fecha futura no se guarda';
});
await tc(230, 'Una nueva evaluación deja no vigentes a las anteriores', P.ref, async () => {
  await openTab(P.ref, 'Evaluaciones', E.recordId);
  await fillEval(P.ref, { date: today, professional: 'Dr. Paz', diagnosis: 'Control' });
  const t = await body(P.ref); expectText(t, /Evaluaciones anteriores \(1\)/, 'una evaluación en el historial');
  const vig = sql(`SELECT COUNT(*) FROM psychiatric_evaluations WHERE PersonId='${E.personId}' AND IsValid=1`);
  must(vig === '1', `hay ${vig} vigentes`);
  return 'una sola vigente; la anterior en el historial';
});
await tc(231, 'La ficha marca que tiene evaluación psiquiátrica', null, async () => {
  const flag = sql(`SELECT HasPsychiatricEvaluation FROM social_records WHERE Id='${E.recordId}'`);
  must(flag === '1', `HasPsychiatricEvaluation=${flag}`);
  const F = await newPerson('SinEvaluacion');
  must(sql(`SELECT HasPsychiatricEvaluation FROM social_records WHERE Id='${F.recordId}'`) === '0', 'una ficha sin evaluación figura con evaluación');
  return 'tiene_eval_psiq = true después de cargarla';
});
const V = await newPerson('Vencida'); await evalPost(V.personId, { date: monthsAgo(6, -1) });
const G = await newPerson('Vigente5m'); await evalPost(G.personId, { date: monthsAgo(5) });
await tc(232, 'La evaluación vence a los 6 meses de su fecha', P.ref, async () => {
  await openTab(P.ref, 'Evaluaciones', V.recordId);
  expectText(await body(P.ref), /está vencida/, 'el aviso de vencida');
  await shot(P.ref, 'TC-232-vencida');
  await openTab(P.ref, 'Evaluaciones', G.recordId);
  const t = await body(P.ref); must(!/está vencida/.test(t), 'la de 5 meses figura vencida'); expectText(t, /Vence el/, 'la fecha de vencimiento');
  return '6 meses y 1 día: vencida; 5 meses: vigente';
});
const M = await newPerson('Medicacion');
await evalPost(M.personId, { date: today });
await call('PUT', `/api/persons/${M.personId}/type`, { token: T.ref, body: { personType: 1 } });
await evalPost(M.personId, { date: monthsAgo(7) });
const medCatalog = (await call('GET', '/api/medication-schemes/catalog', { token: T.ref })).json ?? [];
await tc(233, 'Con evaluación vencida se bloquea cargar medicamentos nuevos', P.ref, async () => {
  await openTab(P.ref, 'Medicación', M.recordId);
  const t = await body(P.ref);
  const btn = P.ref.getByRole('button', { name: 'Cargar medicamento' });
  if (await btn.count() && await btn.isEnabled()) {
    await btn.click(); await sleep(700);
    const opts = await P.ref.$$eval('#scheme-medication option', os => os.map(o => o.value).filter(Boolean));
    await P.ref.selectOption('#scheme-medication', opts[0]); await P.ref.fill('#scheme-dose', '1 comprimido'); await P.ref.fill('#scheme-time', '10:20');
    await P.ref.getByRole('button', { name: 'Guardar' }).click(); await sleep(1800);
  }
  expectText(await body(P.ref) + t, /evaluaci[óo]n psiqui[áa]trica/i, 'el bloqueo por evaluación');
  const api = await call('POST', `/api/persons/${M.personId}/medication-schemes`, { token: T.ref, body: { medicationId: medCatalog[0]?.id, dose: '1', time: '10:20' } });
  must(api.status === 409, `la API respondió ${api.status}`);
  await evalPost(M.personId, { date: today });
  const ok = await call('POST', `/api/persons/${M.personId}/medication-schemes`, { token: T.ref, body: { medicationId: medCatalog[0]?.id, dose: '1', time: '10:20' } });
  must(ok.status === 201 || ok.status === 200, `con evaluación nueva respondió ${ok.status}`);
  return 'bloqueado (409); con evaluación nueva se permite';
});
await tc(234, 'Con evaluación vencida se bloquea el ingreso a la Casa de Convivencia', P.ref, async () => {
  await go(P.ref, `/dashboard/fichas/${V.recordId}`);
  const entry = P.ref.getByRole('button', { name: /Registrar ingreso/i });
  const uiBlocked = !(await entry.count()) || !(await entry.first().isEnabled()) || /vencida/i.test(await body(P.ref));
  const api = await call('PUT', `/api/persons/${V.personId}/type`, { token: T.ref, body: { personType: 1 } });
  if (api.status === 204) await call('PUT', `/api/persons/${V.personId}/type`, { token: T.ref, body: { personType: 0 } });
  must(uiBlocked, 'la pantalla permite registrar el ingreso');
  must(api.status === 400 && /evaluaci/i.test(api.text), `la API respondió ${api.status} al ingreso con la evaluación vencida`);
  return 'pantalla bloqueada y la API lo rechaza (400, falta evaluación vigente)';
});
const W = await newPerson('Alerta20'); await evalPost(W.personId, { date: monthsAgo(6, 20) });
const W40 = await newPerson('Alerta40'); await evalPost(W40.personId, { date: monthsAgo(6, 40) });
await tc(235, 'Alerta visual cuando la evaluación vence en menos de 30 días', P.ref, async () => {
  await openTab(P.ref, 'Evaluaciones', W.recordId);
  expectText(await body(P.ref), /La evaluación vence en \d+ días/, 'la alerta de vencimiento');
  await shot(P.ref, 'TC-235-alerta');
  await openTab(P.ref, 'Evaluaciones', W40.recordId);
  must(!/La evaluación vence en/.test(await body(P.ref)), 'se muestra la alerta a 40 días');
  return `${(await evStatus(W.personId)).diasParaVencer} días: alerta; 40 días: sin alerta`;
});
await tc(236, 'Ver el historial de evaluaciones anteriores', P.ref, async () => {
  await openTab(P.ref, 'Evaluaciones', E.recordId);
  const t = await body(P.ref);
  expectText(t, /Evaluaciones anteriores \(1\)/, 'el historial'); expectText(t, /Dra\. Gómez/, 'la evaluación anterior'); expectText(t, /Cargada por Test referente/, 'quién la cargó');
  return 'fecha, profesional, diagnóstico y quién la cargó';
});
await tc(237, 'El Escucha no puede cargar evaluaciones', P.esc, async () => {
  await go(P.esc, `/dashboard/fichas/${E.recordId}`);
  const tab = P.esc.getByRole('button', { name: 'Evaluaciones', exact: true });
  if (await tab.count()) { await tab.first().click(); await sleep(1500); }
  must(await P.esc.getByRole('button', { name: 'Registrar evaluación' }).count() === 0, 'la Escucha ve el botón de carga');
  const api = await evalPost(E.personId, {}, T.esc);
  must(api.status === 403, `la API respondió ${api.status}`);
  return 'sin formulario y 403 por API';
});

// ======================= SCRUM-67 estado activo/inactivo =======================
const J31 = await newPerson('Sin31dias'), J29 = await newPerson('Con29dias');
const att = (p, date, tok = T.ref) => call('POST', '/api/attendances', { token: tok, body: { personId: p, activity: 0, date, anonymousCount: 0 } });
await att(J31.personId, iso(ar(-31))); await att(J29.personId, iso(ar(-29)));
// fichas dadas de alta hace 40 días: una ficha nueva tiene 30 días de gracia antes del pase automático
sql(`UPDATE social_records SET CreatedAt = DATEADD(day, -40, GETUTCDATE()) WHERE Id IN ('${J31.recordId}', '${J29.recordId}')`);
// los jobs diarios corren al arrancar la API
execFileSync('docker', ['restart', 'vicaria-api'], { encoding: 'utf8' });
for (let i = 0; i < 60; i++) { try { if ((await fetch(BASE + '/api/caritas-catalogs')).status === 401) break; } catch { /* arrancando */ } await sleep(1000); }
await sleep(3000); T = await tokens();
await tc(238, 'Pasa a Inactivo a los 30 días sin asistencia', P.ref, async () => {
  const a = await detail(J31.recordId), b = await detail(J29.recordId);
  must(a?.status === 1, `la persona con 31 días sin asistir quedó ${a?.status === 0 ? 'Activa' : a?.status}`);
  must(b?.status === 0, 'la persona con 29 días quedó Inactiva');
  await go(P.ref, `/dashboard/fichas/${J31.recordId}`);
  return '31 días: Inactiva; 29 días: Activa';
});
await tc(239, 'Vuelve a Activo al registrar una asistencia', P.ref, async () => {
  const r = await att(J31.personId, today);
  must(r.status === 204, `la asistencia respondió ${r.status}`);
  const d = await detail(J31.recordId); must(d?.status === 0, 'sigue Inactiva');
  await go(P.ref, `/dashboard/fichas/${J31.recordId}`);
  must(!/Marcar como Activo/.test(await body(P.ref)), 'el perfil sigue mostrando Inactivo');
  return 'Activa después de la asistencia';
});
const I = await newPerson('Inactiva');
await call('PATCH', `/api/persons/${I.personId}/status`, { token: T.ref, body: { status: 1, reason: 'Viaje' } });
await tc(240, 'La etiqueta Inactivo se ve en el perfil y en el listado', P.ref, async () => {
  await go(P.ref, `/dashboard/fichas?search=${encodeURIComponent(`Inactiva UI ${TAG}`)}`, 2200);
  let t = await body(P.ref);
  if (!t.includes(I.name)) { const s = P.ref.locator('input[type="search"], input[placeholder*="Buscar"]').first(); if (await s.count()) { await s.fill(`Inactiva UI ${TAG}`); await sleep(1800); t = await body(P.ref); } }
  must(new RegExp(`${I.name}.{0,200}Inactivo|Inactivo.{0,200}${I.name}`).test(t), 'el listado no muestra la etiqueta Inactivo');
  await shot(P.ref, 'TC-240-listado');
  await go(P.ref, `/dashboard/fichas/${I.recordId}`);
  expectText(await body(P.ref), /Inactivo/, 'la etiqueta en el perfil');
  return 'listado y perfil';
});
await tc(241, 'Los Referentes reciben una notificación cuando alguien pasa a Inactivo', P.ref, async () => {
  const n = (await call('GET', '/api/notifications', { token: T.ref })).json ?? [];
  const manual = n.some(x => x.eventType === 'PersonStatusInactive' && x.description.includes(I.name));
  const auto = n.some(x => x.eventType === 'PersonStatusInactive' && x.description.includes(J31.name));
  await go(P.ref, '/dashboard');
  must(manual, 'no llegó la notificación del cambio manual');
  must(auto, 'el cambio manual notifica, pero el pase automático del proceso diario no genera notificación');
});
const C = await newPerson('Cambio');
await tc(242, 'Cambiar el estado manualmente con justificación', P.ref, async () => {
  await go(P.ref, `/dashboard/fichas/${C.recordId}`);
  await P.ref.getByRole('button', { name: 'Marcar como Inactivo' }).click(); await sleep(600);
  await P.ref.getByRole('button', { name: 'Confirmar' }).last().click(); await sleep(800);
  must(await P.ref.locator('#status-reason').count() > 0, 'el modal se cerró sin motivo');
  must((await detail(C.recordId)).status === 0, 'cambió sin motivo');
  await P.ref.fill('#status-reason', 'Internación'); await P.ref.getByRole('button', { name: 'Confirmar' }).last().click(); await sleep(2000);
  must((await detail(C.recordId)).status === 1, 'no cambió a Inactivo');
  expectText(await body(P.ref), /Marcar como Activo/, 'el perfil actualizado');
  return 'sin motivo no guarda; con motivo pasa a Inactivo';
});
await tc(243, 'No permite cambiar al mismo estado', P.ref, async () => {
  expectText(await body(P.ref), /Marcar como Activo/, 'que la pantalla solo ofrezca el estado contrario');
  const api = await call('PATCH', `/api/persons/${C.personId}/status`, { token: T.ref, body: { status: 1, reason: 'prueba' } });
  must(api.status === 409 && /ya se encuentra/i.test(api.text), `la API respondió ${api.status}`);
  return 'la pantalla ofrece solo el contrario; la API responde 409';
});
await tc(244, 'El historial de estados guarda fecha y motivo', P.ref, async () => {
  await go(P.ref, `/dashboard/fichas/${J31.recordId}`);
  const t = await body(P.ref);
  expectText(t, /Historial de Estado/, 'el historial'); expectText(t, /Paso automático a inactivo/, 'el pase automático con su motivo');
  expectText(t, /reactivada por nueva asistencia/i, 'la reactivación');
  await go(P.ref, `/dashboard/fichas/${C.recordId}`);
  expectText(await body(P.ref), /Internación/, 'el motivo del cambio manual');
  const h = (await detail(J31.recordId)).statusHistory;
  must(h.some(x => x.modifiedBy === 'Sistema') && h.every(x => x.modifiedAt && x.reason), 'faltan fecha, motivo o usuario');
  return `${h.length} cambios con fecha, motivo y usuario`;
});
await tc(245, 'El Escucha no puede cambiar el estado', P.esc, async () => {
  await go(P.esc, `/dashboard/fichas/${C.recordId}`);
  must(await P.esc.getByRole('button', { name: /Marcar como/ }).count() === 0, 'la Escucha ve Cambiar estado');
  const api = await call('PATCH', `/api/persons/${C.personId}/status`, { token: T.esc, body: { status: 0, reason: 'x' } });
  must(api.status === 403, `la API respondió ${api.status}`);
  return 'sin botón y 403 por API';
});

// ======================= SCRUM-77 respaldos =======================
const backups = async () => (await call('GET', '/api/admin/backups?page=1&pageSize=30', { token: T.ref })).json?.items ?? [];
const allBackups = await backups();
const succeeded = allBackups.filter(b => b.status === 2), failedBackups = allBackups.filter(b => b.status === 3);
const lastFailure = failedBackups[0];
await tc(246, 'El sistema genera el respaldo diario', P.ref, async () => {
  await go(P.ref, '/dashboard/configuracion/respaldos');
  must(allBackups.length > 0, 'todavía no corrió ningún respaldo diario');
  must(succeeded.length > 0, `el respaldo diario corrió ${allBackups.length} veces y falló siempre: ${lastFailure?.errorMessage ?? ''}`);
});
// el último exitoso es el real del proceso diario (los qa-ret-* son respaldos viejos sembrados para la retención)
const realBackup = succeeded.find(b => !b.externalKey.startsWith('qa-'));
await tc(247, 'El respaldo se guarda fuera de la base', null, async () => {
  if (!realBackup) throw new Blocked('no hay respaldos exitosos para revisar el almacenamiento (ver TC-246)');
  const head = execFileSync('docker', ['exec', 'vicaria-api', 'sh', '-c', `head -c 4 /app/uploads/backups/${realBackup.externalKey}; echo; stat -c %s /app/uploads/backups/${realBackup.externalKey}`], { encoding: 'utf8', env: { ...process.env, MSYS_NO_PATHCONV: '1' } }).split('\n');
  must(/^(TAPE|MSSQ)/.test(head[0]), `el archivo guardado no es un .bak de SQL Server (${head[0]})`);
  must(Number(head[1]) === realBackup.sizeBytes, 'el tamaño del archivo no coincide con el registrado');
  return `archivo ${realBackup.externalKey} de ${realBackup.sizeBytes} bytes en el almacenamiento de respaldos`;
});
await tc(248, 'Se conservan los últimos 30 respaldos', null, async () => {
  const seeded = Number(sql(`SELECT COUNT(*) FROM backups WHERE ExternalKey LIKE 'qa-ret-%'`));
  if (!realBackup || !seeded) throw new Blocked('hace falta un respaldo exitoso del proceso diario con 30 respaldos anteriores sembrados');
  const files = execFileSync('docker', ['exec', 'vicaria-api', 'sh', '-c', 'ls /app/uploads/backups'], { encoding: 'utf8', env: { ...process.env, MSYS_NO_PATHCONV: '1' } });
  // el listado pagina de a 30 y mezcla los fallidos: se cuenta en la base
  const kept = Number(sql('SELECT COUNT(*) FROM backups WHERE Status = 2'));
  must(kept === 30, `quedan ${kept} respaldos exitosos`);
  must(sql(`SELECT COUNT(*) FROM backups WHERE ExternalKey = 'qa-ret-60.bak'`) === '0' && !files.includes('qa-ret-60.bak'), 'el respaldo más viejo no se eliminó');
  must(files.includes('qa-ret-31.bak'), 'se borró un respaldo que debía conservarse');
  return '30 exitosos; el más viejo se borró de la lista y del almacenamiento';
});
await tc(249, 'Descargar un respaldo desde el panel', P.ref, async () => {
  // respaldo exitoso de prueba: fila y archivo en el almacenamiento local de la API
  const id = '00000000-0000-4000-9000-' + Date.now().toString().slice(-12);
  const key = `qa-${TAG}.bak`;
  execFileSync('docker', ['exec', 'vicaria-api', 'sh', '-c', `mkdir -p /app/uploads/backups && printf 'respaldo QA' > /app/uploads/backups/${key}`], { encoding: 'utf8', env: { ...process.env, MSYS_NO_PATHCONV: '1' } });
  sql(`INSERT INTO backups (Id, Status, CreatedAt, SizeBytes, ExternalKey, ErrorMessage) VALUES ('${id}', 2, SYSUTCDATETIME(), 11, '${key}', NULL)`);
  try {
    await go(P.ref, '/dashboard/configuracion/respaldos');
    const [dl] = await Promise.all([P.ref.waitForEvent('download', { timeout: 10000 }), P.ref.getByRole('button', { name: 'Descargar' }).first().click()]);
    const content = fs.readFileSync(await dl.path(), 'utf8');
    must(content === 'respaldo QA', 'el archivo descargado no es el del respaldo');
    if (lastFailure) {
      const r = await call('GET', `/api/admin/backups/${lastFailure.id}/download`, { token: T.ref });
      must(r.status === 409, `descargar un respaldo fallido respondió ${r.status}`);
      expectText(await body(P.ref), /Sin archivo/, 'los fallidos sin opción de descarga');
    }
    return `${dl.suggestedFilename()}; los fallidos no se descargan (409)`;
  } finally {
    sql(`DELETE FROM backups WHERE Id='${id}'`);
    execFileSync('docker', ['exec', 'vicaria-api', 'rm', '-f', `/app/uploads/backups/${key}`], { env: { ...process.env, MSYS_NO_PATHCONV: '1' } });
  }
});
await tc(250, 'Si el respaldo falla se avisa al administrador', P.ref, async () => {
  if (!lastFailure) throw new Blocked('todavía no corrió el respaldo diario');
  // el banner mira el último respaldo: si el último fue exitoso, se simula una falla posterior
  const fakeId = '00000000-0000-4000-9100-' + Date.now().toString().slice(-12);
  if (allBackups[0]?.status !== 3) sql(`INSERT INTO backups (Id, Status, CreatedAt, SizeBytes, ExternalKey, ErrorMessage) VALUES ('${fakeId}', 3, SYSUTCDATETIME(), 0, '', 'falla simulada QA')`);
  try {
    await go(P.ref, '/dashboard/configuracion/respaldos');
    expectText(await body(P.ref), /El último respaldo falló/, 'el banner de falla');
  } finally {
    sql(`DELETE FROM backups WHERE Id='${fakeId}'`);
  }
  const n = (await call('GET', '/api/notifications', { token: T.ref })).json ?? [];
  must(n.some(x => /respaldo/i.test(x.description)), 'no llegó la notificación interna de la falla');
  const env = execFileSync('docker', ['exec', 'vicaria-api', 'printenv'], { encoding: 'utf8' });
  const mail = /Smtp__|Backup__Smtp/i.test(env) ? 'email enviado al administrador' : 'el email depende de la configuración SMTP y de destinatarios, que el stack local no tiene';
  return `banner, notificación y ${mail}`;
});
await tc(251, 'El sistema sigue funcionando durante el respaldo', null, async () => {
  const file = process.env.AVAIL || new URL('./sprint5-backup-availability.json', import.meta.url);
  if (!realBackup || !fs.existsSync(file)) throw new Blocked('falta la medición de sprint5-backup-availability.mjs durante el respaldo diario');
  const a = JSON.parse(fs.readFileSync(file, 'utf8'));
  must(a.failures === 0, `${a.failures} consultas fallaron durante el respaldo`);
  return `${a.requests} consultas al listado de fichas durante el respaldo de las ${a.runAt.slice(11, 16)} UTC, sin fallas (máx. ${a.maxMs} ms)`;
});
await tc(252, 'Solo el Referente accede a los respaldos', P.esc, async () => {
  await go(P.esc, '/dashboard/configuracion/respaldos');
  must(/access-denied/.test(P.esc.url()) || /Acceso denegado|no ten[ée]s permiso/i.test(await body(P.esc)), `la Escucha entró a ${P.esc.url()}`);
  await go(P.esc, '/dashboard');
  must(!/Respaldos/.test(await body(P.esc)), 'la Escucha ve Respaldos en el menú');
  const api = await call('GET', '/api/admin/backups', { token: T.esc });
  must(api.status === 403, `la API respondió ${api.status}`);
  return 'sin menú, Acceso denegado y 403 por API';
});

// ======================= SCRUM-70 campos del informe (antes del informe, para tener datos) =======================
const catalogAll = async () => (await call('GET', '/api/caritas-catalogs?onlyActive=false', { token: T.ref })).json ?? [];
const newValue = `Situación QA ${TAG}`;
const sectionOf = key => P.ref.locator('section, div').filter({ has: P.ref.locator(`text=${key}`) });
await tc(257, 'Agregar un valor al catálogo de un campo del informe', P.ref, async () => {
  await go(P.ref, '/dashboard/configuracion/campos-informe');
  const inputs = P.ref.getByPlaceholder('Nuevo valor');
  must(await inputs.count() === 5, `hay ${await inputs.count()} campos`);
  await inputs.nth(4).fill(newValue); await P.ref.getByRole('button', { name: 'Agregar' }).nth(4).click(); await sleep(1500);
  expectText(await body(P.ref), new RegExp(newValue), 'el valor nuevo');
  must((await catalogAll()).some(v => v.name === newValue && v.category === 4 && v.isActive), 'el backend no tiene el valor');
  return `"${newValue}" en Motivo principal de asistencia`;
});
await tc(258, 'Editar y desactivar un valor del catálogo', P.ref, async () => {
  const row = () => P.ref.locator('li', { hasText: newValue }).first();
  await row().getByRole('button', { name: 'Editar' }).click(); await sleep(400);
  await P.ref.getByLabel(`Nuevo nombre de ${newValue}`).fill(`${newValue} editado`); await P.ref.getByRole('button', { name: 'Guardar' }).first().click(); await sleep(1500);
  must((await catalogAll()).some(v => v.name === `${newValue} editado`), 'no se guardó el nombre nuevo');
  await P.ref.locator('li', { hasText: `${newValue} editado` }).first().getByRole('button', { name: 'Desactivar' }).click(); await sleep(1500);
  const v = (await catalogAll()).find(x => x.name === `${newValue} editado`);
  must(v && v.isActive === false, 'no quedó desactivado en el backend');
  expectText(await body(P.ref), new RegExp(`${newValue} editado`), 'el valor inactivo en la configuración');
  return 'editado y desactivado (sigue visible como inactivo)';
});
await tc(259, 'No permitir valores duplicados en el mismo campo', P.ref, async () => {
  const inputs = P.ref.getByPlaceholder('Nuevo valor');
  await inputs.nth(4).fill('violencia'); await P.ref.getByRole('button', { name: 'Agregar' }).nth(4).click(); await sleep(1000);
  expectText(await body(P.ref), /Ya existe/, 'el aviso de duplicado');
  const api = await call('POST', '/api/caritas-catalogs', { token: T.ref, body: { category: 4, name: 'Violencia' } });
  must(api.status === 409, `la API respondió ${api.status}`);
  return 'aviso en pantalla y 409 por API';
});
const R = await newPerson('Caritas');
await att(R.personId, today);
await tc(260, 'Indicadores de campos del informe sin completar en la ficha', P.ref, async () => {
  await go(P.ref, `/dashboard/fichas/${R.recordId}`);
  expectText(await body(P.ref), /5 datos pendientes para el informe/, 'el indicador de pendientes');
  await P.ref.getByRole('button', { name: 'Completar' }).click(); await sleep(800);
  for (const key of ['gender', 'housing', 'origin', 'document', 'mainReason']) {
    const sel = P.ref.locator(`#modal-report-field-${key}`);
    const values = await sel.locator('option').evaluateAll(os => os.map(o => o.value).filter(Boolean));
    must(values.length, `el campo ${key} no tiene opciones`);
    await sel.selectOption(values[0]);
  }
  await P.ref.getByRole('button', { name: 'Guardar' }).last().click(); await sleep(2000);
  must(!/datos? pendientes? para el informe/.test(await body(P.ref)), 'siguen los indicadores');
  const f = (await detail(R.recordId)).reportFields;
  must(Object.values(f).every(Boolean), `el backend no tiene los 5 campos: ${JSON.stringify(f)}`);
  return 'pendientes → completos';
});
await tc(261, 'Solo el Referente modifica el catálogo', P.esc, async () => {
  const api = await call('POST', '/api/caritas-catalogs', { token: T.esc, body: { category: 4, name: `Escucha ${TAG}` } });
  must(api.status === 403, `la API respondió ${api.status}`);
  await go(P.esc, '/dashboard/configuracion/campos-informe');
  must(/access-denied/.test(P.esc.url()), `la Escucha entró a ${P.esc.url()}`);
  return '403 por API y Acceso denegado';
});

// ======================= SCRUM-69 informe de Cáritas =======================
await tc(253, 'Generar el informe de Cáritas de un período', P.ref, async () => {
  await go(P.ref, '/dashboard/informes/caritas');
  await P.ref.locator('input[name="caritas-period"][value="trimestral"]').check();
  await P.ref.fill('#caritas-to', today);
  await P.ref.getByRole('button', { name: 'Generar informe' }).click(); await sleep(2500);
  const t = await body(P.ref);
  expectText(t, /Totales del período|Personas atendidas/i, 'los totales'); expectText(t, /motivo/i, 'el desglose por motivo');
  must(!/DATOS DE EJEMPLO|de ejemplo/i.test(t), 'el informe sigue mostrando datos de ejemplo');
  const rep = (await call('GET', `/api/caritas-reports?type=trimestral&to=${today}`, { token: T.ref })).json;
  const total = rep.totals.attendedPersons;
  const sum = items => items.reduce((s, i) => s + i.count, 0);
  const off = [['género', sum(rep.gender)], ['situación habitacional', sum(rep.housing)], ['documento', sum(rep.dni)], ['motivo', sum(rep.reasons)], ['origen', sum(rep.geography)]].filter(([, n]) => n !== total);
  must(!off.length, `con ${total} personas atendidas, ${off.map(([k, n]) => `${k} suma ${n}`).join(', ')}`);
  return `${total} personas`;
});
await tc(254, 'Validar el período del informe', P.ref, async () => {
  await go(P.ref, '/dashboard/informes/caritas');
  await P.ref.fill('#caritas-to', iso(ar(2)));  await sleep(400);
  const t = await body(P.ref);
  const disabled = await P.ref.getByRole('button', { name: 'Generar informe' }).isDisabled();
  must(/no puede ser futura/.test(t) || disabled, 'acepta una fecha de corte futura');
  const api = await call('GET', `/api/caritas-reports?type=anual&to=${today}`, { token: T.ref });
  must(api.status === 400, `la API respondió ${api.status} al período anual`);
  return 'fecha futura rechazada; período inválido 400 por API';
});
await tc(255, 'Exportar el informe de Cáritas a PDF', P.ref, async () => {
  await go(P.ref, '/dashboard/informes/caritas');
  await P.ref.fill('#caritas-to', today);
  await P.ref.getByRole('button', { name: 'Generar informe' }).click(); await sleep(2500);
  const [dl] = await Promise.all([P.ref.waitForEvent('download', { timeout: 15000 }), P.ref.getByRole('button', { name: 'Exportar PDF' }).click()]);
  const text = fs.readFileSync(await dl.path()).toString('latin1');
  must(/^%PDF/.test(text), 'el archivo no es un PDF');
  const api = await call('GET', `/api/caritas-reports/pdf?type=trimestral&to=${today}`, { token: T.ref });
  must(api.status === 200 && api.buf.subarray(0, 4).toString() === '%PDF', `el PDF de la API respondió ${api.status}`);
  return dl.suggestedFilename();
});
await tc(256, 'Solo el Referente genera el informe de Cáritas', P.esc, async () => {
  await go(P.esc, '/dashboard');
  must(!/Cáritas/.test(await body(P.esc)), 'la Escucha ve el informe en el menú');
  const api = await call('GET', `/api/caritas-reports?type=trimestral&to=${today}`, { token: T.esc });
  must(api.status === 403, `la API respondió ${api.status}`);
  return 'sin menú y 403 por API';
});

await browser.close();
fs.rmSync(tmp, { recursive: true, force: true });
res.sort((a, b) => Number(a.id.slice(3)) - Number(b.id.slice(3)));
const failed = res.filter(x => !x.ok);
fs.writeFileSync(OUT, JSON.stringify(res, null, 2));
console.log(`\nTOTAL ${res.length} PASS ${res.length - failed.length} FAIL ${failed.filter(x => !x.blocked).length} BLOQ ${failed.filter(x => x.blocked).length}`);
