// Sprint 4: front cableado a la API real (asistencia, actividades, medicación y stock).
//   BASE=http://localhost:4200 node sprint4-wired-flows.mjs   (stack local con datos semilla; requiere `playwright` instalado donde se corra)
// Parte 1: cadena funcional por API (stock -> esquema -> asistencia -> agenda -> toma -> descuento de stock).
// Parte 2: las pantallas del Sprint 4 abren con la sesión del Referente sin errores de API ni de consola.
import { chromium } from 'playwright';
import fs from 'fs';
const BASE = process.env.BASE || 'http://localhost:4200';
const SHOTS = process.env.SHOTS || './shots-s4';
fs.mkdirSync(SHOTS, { recursive: true });
const PASS = 'Test1234!';
const res = [];
const rec = (id, name, ok, note = '') => { res.push({ id, name, ok, note }); console.log(`${ok ? 'PASS' : 'FAIL'}  ${id}  ${name}${note ? ' — ' + note : ''}`); };
const call = async (method, path, { token, body } = {}) => {
  const h = { 'Content-Type': 'application/json' }; if (token) h.Authorization = `Bearer ${token}`;
  const r = await fetch(BASE + path, { method, headers: h, body: body === undefined ? undefined : JSON.stringify(body) });
  const text = await r.text(); let json = null; try { json = JSON.parse(text); } catch { /* no es JSON */ }
  return { status: r.status, json, text, headers: r.headers };
};
const login = async e => (await call('POST', '/api/auth/login', { body: { email: e, password: PASS } })).json?.token;
const T = { ref: await login('referente@test.com'), dir: await login('directora@test.com'), esc: await login('escucha@test.com') };
const today = new Date(Date.now() - 3 * 36e5).toISOString().slice(0, 10);
const RND = String(Date.now()).slice(-6);
const hhmm = `${String(Math.floor(Math.random() * 10) + 12).padStart(2, '0')}:${String(Math.floor(Math.random() * 6) * 10).padStart(2, '0')}`;

// personas con evaluación vigente del seed
const people = (await call('GET', '/api/social-records?page=1&pageSize=50', { token: T.ref })).json;
const list = people?.items ?? people?.data ?? people ?? [];
const lucia = list.find(p => /Luc/i.test(p.firstName ?? p.name ?? p.fullName ?? ''));
const LUCIA = lucia?.personId ?? lucia?.id ?? '33438A32-F4E6-4FBA-B868-07F6362B5BFD';

// ---- Parte 1: cadena por API
const medName = `QA Medicamento ${RND}`;
let r = await call('GET', '/api/stock-medicamentos', { token: T.ref });
rec('S4W-01', 'GET /api/stock-medicamentos lista el inventario completo (Referente)', r.status === 200 && Array.isArray(r.json) && r.json.length > 0 && 'minimumStockThreshold' in r.json[0], `status=${r.status} items=${r.json?.length}`);
r = await call('GET', '/api/stock-medicamentos', { token: T.esc });
rec('S4W-02', 'Escucha no ve el inventario (403)', r.status === 403, `status=${r.status}`);
r = await call('POST', '/api/stock-medicamentos', { token: T.ref, body: { nombre: medName, cantidad: 3, fechaVencimiento: `${new Date().getFullYear() + 1}-06-30T00:00:00`, proveedor: 'Proveedor QA' } });
const MED = r.json?.medicationId;
rec('S4W-03', 'Ingreso de un medicamento nuevo (201 con id)', r.status === 201 && !!MED, `status=${r.status}`);
r = await call('GET', '/api/medication-schemes/catalog', { token: T.dir });
rec('S4W-04', 'Catálogo de medicamentos con stock (Directora) incluye el nuevo con 3 unidades', r.status === 200 && r.json?.some(m => m.id === MED && m.pharmacyStock === 3), `status=${r.status}`);
r = await call('POST', `/api/persons/${LUCIA}/medication-schemes`, { token: T.ref, body: { medicationId: MED, dose: '1 comprimido', time: hhmm } });
const SCHEME = r.json?.id;
rec('S4W-05', 'Se carga el esquema con el medicamento nuevo (201)', r.status === 201 && !!SCHEME, `status=${r.status} ${r.text.slice(0, 80)}`);
r = await call('POST', '/api/attendances', { token: T.ref, body: { personId: LUCIA, activity: 0, date: today, anonymousCount: 2 } });
rec('S4W-06', 'Se registra la asistencia de hoy (204) o ya estaba (409)', r.status === 204 || r.status === 409, `status=${r.status}`);
r = await call('GET', `/api/attendances?activity=0&date=${today}`, { token: T.ref });
rec('S4W-07', 'GET /api/attendances devuelve el día con la persona y el conteo sin ficha', r.status === 200 && r.json?.persons?.some(p => p.personId.toLowerCase() === LUCIA.toLowerCase() && p.fullName) && r.json?.anonymousCount >= 0, `status=${r.status} personas=${r.json?.persons?.length} sinFicha=${r.json?.anonymousCount}`);
r = await call('GET', `/api/attendances?activity=0&date=${today}`, { token: T.esc });
rec('S4W-08', 'Escucha también ve la asistencia del día (200)', r.status === 200, `status=${r.status}`);
r = await call('GET', `/api/medicamentos/agenda?fecha=${today}`, { token: T.ref });
const intake = r.json?.groups?.flatMap(g => g.intakes).find(i => i.medicationName === medName);
rec('S4W-09', 'La agenda del día incluye la toma del esquema nuevo, pendiente', r.status === 200 && intake && intake.status === 1, `status=${r.status} toma=${intake?.id ?? 'no está'}`);
if (intake) {
  r = await call('PATCH', `/api/medicamentos/agenda/${intake.id}`, { token: T.ref, body: { status: 2, observation: 'QA' } });
  rec('S4W-10', 'Marcar la toma como administrada (204)', r.status === 204, `status=${r.status}`);
  r = await call('GET', '/api/stock-medicamentos', { token: T.ref });
  rec('S4W-11', 'El stock se descontó solo en el backend (3 -> 2)', r.json?.find(m => m.medicationId === MED)?.currentStock === 2, `stock=${r.json?.find(m => m.medicationId === MED)?.currentStock}`);
  r = await call('GET', `/api/persons/${LUCIA}/medication-history`, { token: T.ref });
  rec('S4W-12', 'El historial de la persona refleja la toma administrada', r.status === 200 && r.json?.some(h => h.medicationName === medName && h.status === 'administered'), `status=${r.status}`);
}
r = await call('GET', `/api/personas/${LUCIA}/asistencias`, { token: T.ref });
rec('S4W-13', 'Historial de asistencia de la persona con indicadores', r.status === 200 && r.json?.indicators && Array.isArray(r.json?.groups), `status=${r.status}`);
r = await call('GET', `/api/attendances/statistics?from=${today.slice(0, 8)}01&to=${today}&activity=DayCenter`, { token: T.ref });
rec('S4W-14', 'Estadísticas del mes filtradas por actividad por nombre', r.status === 200 && r.json?.totalRecords >= 1, `status=${r.status} registros=${r.json?.totalRecords}`);
r = await call('GET', '/api/attendances/inactive?days=14', { token: T.ref });
rec('S4W-15', 'Listado de inactivos (200)', r.status === 200 && Array.isArray(r.json), `status=${r.status}`);
r = await call('GET', `/api/attendances/monthly-summary/pdf?year=${today.slice(0, 4)}&month=${Number(today.slice(5, 7))}`, { token: T.ref });
rec('S4W-16', 'Resumen mensual en PDF (application/pdf)', r.status === 200 && /pdf/.test(r.headers.get('content-type') ?? ''), `status=${r.status} ${r.headers.get('content-type')}`);
r = await call('POST', '/api/actividades/sincronizar-calendario', { token: T.ref });
rec('S4W-17', 'Sincronizar actividades del calendario (200)', r.status === 200 && typeof r.json?.syncedCount === 'number', `status=${r.status}`);
r = await call('GET', '/api/actividades?includeInactive=true', { token: T.ref });
rec('S4W-18', 'Catálogo de actividades configurable (200)', r.status === 200 && Array.isArray(r.json), `status=${r.status} n=${r.json?.length}`);

// ---- Parte 2: pantallas del Sprint 4 en el navegador
const browser = await chromium.launch();
const ctx = await browser.newContext({ viewport: { width: 1400, height: 950 } });
const page = await ctx.newPage();
let bad = [], errs = [];
page.on('console', m => { if (m.type() === 'error' && !/ERR_ABORTED/.test(m.text())) errs.push(m.text().slice(0, 160)); });
page.on('response', x => { if (x.url().includes('/api/') && x.status() >= 400) bad.push(`${x.status()} ${x.request().method()} ${x.url().replace(BASE, '')}`); });
await page.goto(BASE + '/auth'); await page.waitForSelector('input[formControlName="email"]');
await page.fill('input[formControlName="email"]', 'referente@test.com');
await page.fill('input[formControlName="password"]', PASS);
await page.click('button[type="submit"]'); await page.waitForURL(/dashboard/, { timeout: 15000 });
const visit = async (id, name, path, expected) => {
  bad = []; errs = [];
  try {
    await page.goto(BASE + path); await page.waitForTimeout(2500);
    const text = (await page.innerText('body')).replace(/\s+/g, ' ');
    await page.screenshot({ path: `${SHOTS}/${id}.png` });
    rec(id, name, expected.test(text) && !bad.length && !errs.length, `${bad.join(' | ')} ${errs.join(' | ')}`.trim());
  } catch (e) { rec(id, name, false, 'excepción: ' + e.message.split('\n')[0].slice(0, 160)); }
};
await visit('S4W-20', 'Registro de asistencia diaria abre sin errores', '/asistencia', /asistencia/i);
await visit('S4W-21', 'Configuración de actividades abre y sincroniza sin errores', '/asistencia/actividades', /actividad/i);
await visit('S4W-22', 'Panel de estadísticas de asistencia con datos reales', '/asistencia/estadisticas', /Presencias totales/i);
await visit('S4W-23', 'Agenda diaria de medicamentos con la toma del esquema', '/medicacion', new RegExp(medName.split(' ').slice(0, 2).join(' '), 'i'));
await visit('S4W-24', 'Inventario de medicamentos lista el stock real', '/medicacion/stock', new RegExp(medName, 'i'));
await browser.close();

const failed = res.filter(x => !x.ok);
fs.writeFileSync('sprint4-wired-results.json', JSON.stringify(res, null, 2));
console.log(`\nTOTAL ${res.length} PASS ${res.length - failed.length} FAIL ${failed.length}`);
process.exit(failed.length ? 1 : 0);
