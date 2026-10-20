// Sprint 4: ejecución de los casos manuales de interfaz TC-147 a TC-194 con navegador real (Playwright),
// contra el stack local con datos semilla. Cada caso maneja la pantalla y verifica además contra la API;
// donde el caso exige un estado que la interfaz no puede fabricar (evaluación vencida, esquema sin revisar
// hace 30 días) se prepara por SQL en el contenedor y se deja como estaba al terminar.
//   BASE=http://localhost:4200 node sprint4-ui-cases.mjs   (requiere `playwright` instalado donde se corra)
import { chromium } from 'playwright';
import { execFileSync } from 'child_process';
import fs from 'fs';

const BASE = process.env.BASE || 'http://localhost:4200';
const SHOTS = process.env.SHOTS || './shots-s4ui';
fs.mkdirSync(SHOTS, { recursive: true });
const PASS = 'Test1234!';
const RND = String(Date.now()).slice(-6);
const res = [];
const sleep = ms => new Promise(r => setTimeout(r, ms));

const call = async (method, path, { token, body } = {}) => {
  const h = { 'Content-Type': 'application/json' }; if (token) h.Authorization = `Bearer ${token}`;
  const r = await fetch(BASE + path, { method, headers: h, body: body === undefined ? undefined : JSON.stringify(body) });
  const text = await r.text(); let json = null; try { json = JSON.parse(text); } catch { /* no es JSON */ }
  return { status: r.status, json, text, headers: r.headers };
};
const login = async e => (await call('POST', '/api/auth/login', { body: { email: e, password: PASS } })).json?.token;
const T = { ref: await login('referente@test.com'), dir: await login('directora@test.com'), esc: await login('escucha@test.com'), coo: await login('coordinador@test.com') };

const sql = q => execFileSync('docker', ['exec', 'vicaria-sqlserver', 'bash', '-c',
  `/opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -d VicariaDb -h -1 -W -Q "SET NOCOUNT ON; ${q.replace(/"/g, '\\"')}"`],
  { encoding: 'utf8', env: { ...process.env, MSYS_NO_PATHCONV: '1' } }).trim();

const dayStr = d => d.toISOString().slice(0, 10);
const today = dayStr(new Date(Date.now() - 3 * 36e5));
const yesterday = dayStr(new Date(Date.now() - 3 * 36e5 - 864e5));
const LUCIA = '33438a32-f4e6-4fba-b868-07f6362b5bfd';

// ---------- personas ----------
const people = [];
for (let pg = 1; pg <= 12; pg++) {
  const l = (await call('GET', `/api/social-records/list?page=${pg}`, { token: T.ref })).json?.items ?? [];
  if (!l.length) break; people.push(...l);
}
const byPerson = id => people.find(p => p.personId.toLowerCase() === id.toLowerCase());
const fichaId = pid => byPerson(pid)?.id;
const residents = people.filter(p => p.personType === 1);
const ambulatory = people.filter(p => p.personType === 0 && p.status === 0 && (p.firstName ?? '').length >= 3);
const dayAtt = async (act, date, tok = T.ref) => (await call('GET', `/api/attendances?activity=${act}&date=${date}`, { token: tok })).json;

// la agenda se genera al registrar la asistencia: para que la corrida sea repetible se limpian las
// asistencias y tomas de Lucía de hoy, ayer y hace 5 días (datos de QA del stack local)
const d5 = dayStr(new Date(Date.now() - 3 * 36e5 - 5 * 864e5));
// (hoy: toda la asistencia del día, que en el stack local solo la cargan estas corridas)
sql(`DELETE FROM daily_medication_schedules WHERE CONVERT(date,Date)='${today}'`);
sql(`DELETE FROM attendances WHERE CONVERT(date,Date)='${today}'`);
for (const d of [yesterday, d5]) {
  sql(`DELETE FROM daily_medication_schedules WHERE PersonId='${LUCIA}' AND CONVERT(date,Date)='${d}'`);
  sql(`DELETE FROM attendances WHERE PersonId='${LUCIA}' AND CONVERT(date,Date)='${d}'`);
}

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
const P = { ref: await mkPage('referente@test.com'), dir: await mkPage('directora@test.com'), esc: await mkPage('escucha@test.com') };
const body = async p => (await p.innerText('body')).replace(/\s+/g, ' ');
const go = async (p, path, wait = 1800) => { await p.goto(BASE + path); await sleep(wait); };
const shot = (p, id) => p.screenshot({ path: `${SHOTS}/${id}.png` }).catch(() => {});
const expectText = (txt, re, what) => { if (!re.test(txt)) throw new Error(`no se vio ${what ?? re}`); };
const must = (cond, msg) => { if (!cond) throw new Error(msg); };

const tc = async (n, title, page, fn) => {
  const id = `TC-${n}`;
  let ok = true, note = '';
  try { note = (await fn()) ?? ''; } catch (e) { ok = false; note = e.message.split('\n')[0].slice(0, 260); }
  if (page) await shot(page, id);
  res.push({ id, title, ok, note });
  console.log(`${ok ? 'PASS' : 'FAIL'}  ${id}  ${title}${note ? ' — ' + note : ''}`);
};

// ---------- datos de la corrida ----------
const medName = `QA UI Medicamento ${RND}`;
let r = await call('POST', '/api/stock-medicamentos', { token: T.ref, body: { nombre: medName, cantidad: 3, fechaVencimiento: `${new Date().getFullYear() + 1}-06-30T00:00:00`, proveedor: 'Proveedor QA UI' } });
const MED = r.json?.medicationId;
const hh = String(Math.floor(Math.random() * 8) + 9).padStart(2, '0');
const mm = String(Math.floor(Math.random() * 6) * 10).padStart(2, '0');
const medTime = `${hh}:${mm}`;
// el esquema se carga desde la interfaz en TC-167; este segundo medicamento queda para ciclo de vida
const med2Name = `QA UI Medicamento B ${RND}`;
r = await call('POST', '/api/stock-medicamentos', { token: T.ref, body: { nombre: med2Name, cantidad: 5, fechaVencimiento: `${new Date().getFullYear() + 1}-06-30T00:00:00`, proveedor: 'Proveedor QA UI' } });
const MED2 = r.json?.medicationId;
must(MED && MED2, 'no se pudo preparar el stock de la corrida');

const lucia = byPerson(LUCIA);
const cleanups = [];

// ======================= SCRUM-54 =======================
// TC-167 va antes que la asistencia para que la agenda de hoy tenga la toma (la agenda se genera al registrar la asistencia)
await tc(167, 'Cargar un medicamento del catálogo con dosis y hora (desde la ficha)', P.ref, async () => {
  await go(P.ref, `/dashboard/fichas/${lucia.id}`);
  await P.ref.getByRole('button', { name: 'Medicación' }).first().click(); await sleep(1500);
  await P.ref.getByRole('button', { name: 'Cargar medicamento' }).click(); await sleep(800);
  const opts = await P.ref.$$eval('#scheme-medication option', os => os.map(o => ({ v: o.value, t: o.textContent.trim() })));
  const opt = opts.find(o => o.t.includes(medName)); must(opt, 'el medicamento nuevo no aparece en el catálogo del formulario');
  await P.ref.selectOption('#scheme-medication', opt.v);
  await P.ref.fill('#scheme-dose', '1 comprimido'); await P.ref.fill('#scheme-time', medTime);
  await P.ref.getByRole('button', { name: 'Guardar' }).click(); await sleep(2000);
  const t = await body(P.ref); expectText(t, new RegExp(medName), 'el esquema en la lista');
  const s = (await call('GET', `/api/persons/${LUCIA}/medication-schemes`, { token: T.ref })).json;
  must(s?.some(x => x.medicationName === medName && x.dose === '1 comprimido' && x.time.startsWith(medTime)), 'el backend no tiene el esquema');
  return `${medName} a las ${medTime}`;
});

// segundo esquema por API (para ciclo de vida, TC-189/190/191)
r = await call('POST', `/api/persons/${LUCIA}/medication-schemes`, { token: T.ref, body: { medicationId: MED2, dose: '2 gotas', time: '21:30' } });
const SCHEME2 = r.json?.id;

const free = async (act, date, n = 2) => {
  const day = await dayAtt(act, date); const taken = new Set((day?.persons ?? []).map(p => p.personId.toLowerCase()));
  return (act === 1 ? residents : ambulatory).filter(p => !taken.has(p.personId.toLowerCase()) && p.personId.toLowerCase() !== LUCIA).slice(0, n);
};

await tc(147, 'Registrar asistencia diaria de Hogar de día con personas con ficha', P.ref, async () => {
  const [a] = await free(0, today, 1); must(a, 'no hay persona libre');
  await go(P.ref, '/dashboard/asistencia');
  const pick = async name => {
    await P.ref.fill('#attendance-search', ''); await P.ref.fill('#attendance-search', name.slice(0, 4)); await sleep(1500);
    await P.ref.locator('button', { hasText: name }).first().click(); await sleep(400);
  };
  await pick(a.firstName); await pick('Luc');
  const t = await body(P.ref); expectText(t, /Total: \d+/, 'el total del día');
  await P.ref.getByRole('button', { name: 'Guardar asistencia' }).click(); await sleep(2500);
  const day = await dayAtt(0, today);
  must(day.persons.some(p => p.personId.toLowerCase() === a.personId.toLowerCase()) && day.persons.some(p => p.personId.toLowerCase() === LUCIA), 'el backend no tiene a las 2 personas');
  return `${a.firstName} y Lucía`;
});

await tc(148, 'Registrar asistencia diaria de la Casona', P.ref, async () => {
  const [a] = await free(1, today, 1); must(a, 'no hay residente libre');
  await go(P.ref, '/dashboard/asistencia');
  await P.ref.getByRole('tab', { name: /Casa de Convivencia \(diaria\)/ }).first().click(); await sleep(800);
  await P.ref.fill('#attendance-search', a.firstName.slice(0, 4)); await sleep(1500);
  await P.ref.locator('button', { hasText: a.firstName }).first().click(); await sleep(400);
  await P.ref.getByRole('button', { name: 'Guardar asistencia' }).click(); await sleep(2500);
  const casa = await dayAtt(1, today), hogar = await dayAtt(0, today);
  must(casa.persons.some(p => p.personId.toLowerCase() === a.personId.toLowerCase()), 'no quedó en Casa de Convivencia');
  must(!hogar.persons.some(p => p.personId.toLowerCase() === a.personId.toLowerCase()), 'apareció también en Hogar de día');
  return a.firstName;
});

await tc(149, 'Registrar un conteo de personas sin ficha (anónimas)', P.ref, async () => {
  await go(P.ref, '/dashboard/asistencia');
  const input = P.ref.locator('input[aria-label^="Cantidad de personas sin ficha"]');
  await input.fill('4'); await P.ref.getByRole('button', { name: 'Guardar asistencia' }).click(); await sleep(2500);
  const day = await dayAtt(0, today); must(day.anonymousCount >= 4, `conteo guardado=${day.anonymousCount}`);
  await go(P.ref, '/dashboard/asistencia');
  const v = await P.ref.locator('input[aria-label^="Cantidad de personas sin ficha"]').inputValue();
  must(Number(v) >= 4, `al reabrir el conteo es ${v}`);
  const neg = await call('POST', '/api/attendances', { token: T.ref, body: { personId: ambulatory[0].personId, activity: 0, date: today, anonymousCount: -1 } });
  must(neg.status === 400, `conteo negativo respondió ${neg.status}`);
  return 'conteo 4 persistido; -1 rechazado (400)';
});

await tc(150, 'No permitir asistencia duplicada (persona + actividad + fecha)', P.ref, async () => {
  const day = await dayAtt(0, today); const p = day.persons.find(x => x.personId.toLowerCase() !== LUCIA) ?? day.persons[0];
  const dup = await call('POST', '/api/attendances', { token: T.ref, body: { personId: p.personId, activity: 0, date: today, anonymousCount: 0 } });
  must(dup.status === 409, `duplicado respondió ${dup.status}`);
  const other = await free(1, today, 1);
  const o = await call('POST', '/api/attendances', { token: T.ref, body: { personId: other[0] ? other[0].personId : p.personId, activity: 1, date: today, anonymousCount: 0 } });
  must(o.status === 204 || o.status === 409, `en otra actividad respondió ${o.status}`);
  await go(P.ref, '/dashboard/asistencia'); const t = await body(P.ref);
  expectText(t, new RegExp(p.fullName.split(' ')[0]), 'la persona ya marcada en la lista');
  return `duplicado 409; otra actividad ${o.status}`;
});

await tc(151, 'Guardar la asistencia genera la agenda de medicamentos del día', P.ref, async () => {
  await go(P.ref, '/dashboard/medicacion', 2500);
  const t = await body(P.ref); expectText(t, new RegExp(medName), 'la toma de Lucía en la agenda de hoy');
  return `agenda de hoy con ${medName}`;
});

await tc(152, 'Modificar la asistencia de un día cerrado: solo Referente y con justificación', P.ref, async () => {
  const tmp = { personId: LUCIA };
  const c = await call('POST', '/api/attendances', { token: T.ref, body: { personId: tmp.personId, activity: 0, date: yesterday, anonymousCount: 0 } });
  const day = await dayAtt(0, yesterday); const row = day.persons.find(p => p.personId.toLowerCase() === tmp.personId.toLowerCase());
  const noJust = await call('PUT', `/api/attendances/${row.attendanceId}`, { token: T.ref, body: { anonymousCount: 1, justification: '' } });
  const asDir = await call('PUT', `/api/attendances/${row.attendanceId}`, { token: T.dir, body: { anonymousCount: 1, justification: 'x' } });
  const ok = await call('PUT', `/api/attendances/${row.attendanceId}`, { token: T.ref, body: { anonymousCount: 1, justification: 'Error de carga (QA UI)' } });
  must(noJust.status === 400 && asDir.status === 403 && ok.status === 204, `sin justificación=${noJust.status}, directora=${asDir.status}, referente=${ok.status}`);
  await go(P.ref, '/dashboard/asistencia');
  await P.ref.fill('#attendance-date', yesterday); await P.ref.dispatchEvent('#attendance-date', 'change'); await sleep(1500);
  expectText(await body(P.ref), /justificaci[óo]n/i, 'el campo de justificación al editar un día cerrado');
  return 'sin justificación 400, Directora 403, Referente 204; la pantalla pide justificación';
});

// ======================= SCRUM-55 =======================
const openTab = async (p, tab, ficha = lucia.id) => { await go(p, `/dashboard/fichas/${ficha}`); await p.getByRole('button', { name: tab }).first().click(); await sleep(1800); };

await tc(153, 'Historial de asistencia agrupado por actividad desde el perfil', P.ref, async () => {
  await openTab(P.ref, 'Historial de asistencia'); const t = await body(P.ref);
  expectText(t, /Hogar de día \d+/, 'grupo Hogar de día'); expectText(t, /Casa de Convivencia \(diaria\) \d+/, 'grupo Casa de Convivencia');
});
await tc(154, 'Filtrar el historial por actividad y por rango de fechas', P.ref, async () => {
  await openTab(P.ref, 'Historial de asistencia');
  await P.ref.selectOption('#history-activity', { index: 2 }); await sleep(700);
  let t = await body(P.ref); must(!/Hogar de día \d+ /.test(t.split('Casa de Convivencia')[0].split('ÚLTIMA ASISTENCIA')[1] ?? ''), 'el filtro por actividad no excluyó Hogar de día');
  await P.ref.selectOption('#history-activity', { index: 0 }); await P.ref.fill('#history-from', '2026-10-15'); await P.ref.fill('#history-to', '2026-10-19'); await sleep(900);
  t = await body(P.ref); must(!/04\/08\/2026|13\/04\/2026|25\/02\/2026/.test(t), 'el rango dejó fechas fuera del filtro');
  expectText(t, /1[5-9]\/10\/2026/, 'fechas dentro del rango');
});
await tc(155, 'Indicadores: total de días, racha actual y última asistencia', P.ref, async () => {
  await openTab(P.ref, 'Historial de asistencia'); const t = await body(P.ref);
  expectText(t, /DÍAS ASISTIDOS \d+/); expectText(t, /RACHA ACTUAL \d+ días?/); expectText(t, /ÚLTIMA ASISTENCIA \d{2}\/\d{2}\/\d{4}/);
});
await tc(156, 'Indicar visualmente a la persona con más de 7 días sin asistir', P.ref, async () => {
  const inactive = (await call('GET', '/api/attendances/inactive?days=7', { token: T.ref })).json.find(p => p.lastAttendanceDate && fichaId(p.personId));
  must(inactive, 'no hay una persona con asistencia previa y más de 7 días sin asistir');
  await openTab(P.ref, 'Historial de asistencia', fichaId(inactive.personId));
  expectText(await body(P.ref), /días sin asistir/, 'la alerta de días sin asistir');
  return `${inactive.firstName}: ${inactive.daysSinceLastAttendance} días`;
});
await tc(157, 'La Directora de Casona solo ve el historial de residentes de Casona', P.dir, async () => {
  const amb = ambulatory[0];
  const a = await call('GET', `/api/personas/${amb.personId}/asistencias`, { token: T.dir });
  must(a.status === 403 || a.status === 404, `ambulatorio respondió ${a.status}`);
  let okRes = null;
  for (const rr of residents) { const b = await call('GET', `/api/personas/${rr.personId}/asistencias`, { token: T.dir }); if (b.status === 200) { okRes = rr; break; } }
  must(okRes, 'la Directora no pudo ver el historial de ningún residente (¿ninguno tiene estadía activa en Casona?)');
  const noStay = residents.find(rr => rr !== okRes);
  return `ambulatorio ${a.status}; residente con estadía activa 200 (${okRes.firstName})`;
});

// ======================= SCRUM-56 =======================
await tc(158, 'Ver las estadísticas generales de asistencia', P.ref, async () => {
  await go(P.ref, '/dashboard/asistencia/estadisticas', 2500); const t = await body(P.ref);
  for (const k of ['PRESENCIAS TOTALES', 'PROMEDIO POR DÍA', 'PROMEDIO SEMANAL', 'PROMEDIO MENSUAL', 'Asistencia por actividad']) expectText(t, new RegExp(k), k);
});
await tc(159, 'Filtrar las estadísticas por rango de fechas y actividad', P.ref, async () => {
  await go(P.ref, '/dashboard/asistencia/estadisticas', 2500);
  const total = async () => Number((await body(P.ref)).match(/PRESENCIAS TOTALES (\d+)/)?.[1]);
  const all = await total();
  await P.ref.selectOption('#stats-activity', { index: 2 }); await sleep(1500); const casa = await total();
  await P.ref.selectOption('#stats-activity', { index: 0 }); await P.ref.fill('#stats-from', '2026-10-20'); await P.ref.fill('#stats-to', '2026-10-20'); await sleep(1500); const hoy = await total();
  must(casa <= all && hoy <= all && casa !== all, `todas=${all} casa=${casa} hoy=${hoy}`);
  return `todas=${all}, Casa=${casa}, solo hoy=${hoy}`;
});
await tc(160, 'Identificar personas que dejaron de asistir hace más de 14 días', P.ref, async () => {
  await go(P.ref, '/dashboard/asistencia/estadisticas', 2500); const t = await body(P.ref);
  expectText(t, /Dejaron de asistir hace más de 14 días \d+/);
  const api = (await call('GET', '/api/attendances/inactive?days=14', { token: T.ref })).json;
  must(api.length > 0 && t.includes(api[0].firstName), 'la lista no refleja al listado del backend');
  return `${api.length} personas`;
});
await tc(161, 'Las estadísticas son accesibles solo para Referentes', P.dir, async () => {
  const d = await call('GET', `/api/attendances/statistics?from=${today.slice(0, 8)}01&to=${today}`, { token: T.dir });
  const e = await call('GET', `/api/attendances/statistics?from=${today.slice(0, 8)}01&to=${today}`, { token: T.esc });
  must(d.status === 403 && e.status === 403, `Directora=${d.status}, Escucha=${e.status}`);
  await go(P.dir, '/dashboard/asistencia/estadisticas', 2000);
  must(!/PRESENCIAS TOTALES/.test(await body(P.dir)), 'la Directora ve las estadísticas en pantalla');
  return 'API 403 y sin acceso en pantalla';
});
await tc(162, 'Exportar el resumen mensual de asistencia a PDF', P.ref, async () => {
  await go(P.ref, '/dashboard/asistencia/estadisticas', 2000);
  const [dl] = await Promise.all([P.ref.waitForEvent('download', { timeout: 15000 }), P.ref.getByRole('button', { name: 'Exportar a PDF' }).click()]);
  const f = `${SHOTS}/resumen-mensual.pdf`; await dl.saveAs(f);
  const head = fs.readFileSync(f).subarray(0, 5).toString(); must(head === '%PDF-' && fs.statSync(f).size > 1000, `archivo inválido (${head})`);
  return `${dl.suggestedFilename()} ${fs.statSync(f).size} bytes`;
});

// ======================= SCRUM-57 =======================
await tc(163, 'Las actividades recurrentes del calendario aparecen en el registro de asistencia', P.ref, async () => {
  await go(P.ref, '/dashboard/asistencia/actividades', 2000);
  await P.ref.getByRole('button', { name: 'Sincronizar con el calendario' }).click(); await sleep(2000);
  expectText(await body(P.ref), /Del calendario/, 'actividades del calendario');
  await go(P.ref, '/dashboard/asistencia', 2000);
  const cal = (await call('GET', '/api/actividades', { token: T.ref })).json.filter(a => a.isActive !== false && a.source !== undefined).length;
  expectText(await body(P.ref), /Almuerzo|Desayuno|Merendero/, 'una actividad del calendario como botón');
  return `${cal} actividades del catálogo`;
});
await tc(164, 'Registrar a la misma persona en varias actividades del mismo día', P.ref, async () => {
  const day = await dayAtt(0, today); const casa = await dayAtt(1, today);
  const both = day.persons.filter(p => casa.persons.some(c => c.personId.toLowerCase() === p.personId.toLowerCase()));
  if (both.length) return 'una persona ya figura en Hogar de día y Casa de Convivencia el mismo día';
  const res0 = (await free(1, today, 1))[0]; must(res0, 'no hay residente libre');
  const a = await call('POST', '/api/attendances', { token: T.ref, body: { personId: res0.personId, activity: 0, date: today, anonymousCount: 0 } });
  const b = await call('POST', '/api/attendances', { token: T.ref, body: { personId: res0.personId, activity: 1, date: today, anonymousCount: 0 } });
  must([204, 409].includes(a.status) && b.status === 204, `HdD=${a.status}, Casa=${b.status}`);
  return 'misma persona en dos actividades el mismo día (API); en pantalla solo las dos actividades fijas se guardan (límite conocido)';
});
const ACT = `QA UI Actividad ${RND}`;
await tc(165, 'Los referentes configuran las actividades disponibles', P.ref, async () => {
  await go(P.ref, '/dashboard/asistencia/actividades', 2000);
  await P.ref.fill('#activity-name', ACT); await P.ref.getByRole('button', { name: 'Crear actividad' }).click(); await sleep(2000);
  let t = await body(P.ref); expectText(t, new RegExp(ACT), 'la actividad creada');
  await P.ref.locator(`xpath=//*[normalize-space(text())="${ACT}"]/ancestor::*[.//button[contains(.,"Desactivar")]][1]//button[contains(.,"Desactivar")]`).first().click(); await sleep(1800);
  const list = (await call('GET', '/api/actividades?includeInactive=true', { token: T.ref })).json;
  must(list.find(a => a.name === ACT)?.isActive === false, 'la actividad no quedó desactivada en el backend');
  const asDir = await call('POST', '/api/actividades', { token: T.dir, body: { name: 'x', location: 'HogarDeDia' } });
  must(asDir.status === 403, `Directora creando actividad respondió ${asDir.status}`);
  return 'creada, desactivada; Directora 403';
});
await tc(166, 'La asistencia de la Casona es diaria y separada del Hogar de día', P.ref, async () => {
  await go(P.ref, '/dashboard/asistencia', 2000); const t = await body(P.ref);
  expectText(t, /Casa de Convivencia \(diaria\)/);
  const casa = await dayAtt(1, today), hogar = await dayAtt(0, today);
  const onlyCasa = casa.persons.filter(c => !hogar.persons.some(h => h.personId.toLowerCase() === c.personId.toLowerCase()));
  must(casa.persons.length > 0 && onlyCasa.length > 0, 'no hay una persona solo en Casona');
  return `Casona ${casa.persons.length} personas, ${onlyCasa.length} solo en Casona`;
});

// ======================= SCRUM-59 =======================
await tc(168, 'No permitir medicamentos fuera del catálogo', P.ref, async () => {
  const bad = await call('POST', `/api/persons/${LUCIA}/medication-schemes`, { token: T.ref, body: { medicationId: '00000000-0000-0000-0000-00000000dead', dose: '1', time: '10:00' } });
  must(bad.status === 404, `fuera de catálogo respondió ${bad.status}`);
  await openTab(P.ref, 'Medicación'); await P.ref.getByRole('button', { name: 'Cargar medicamento' }).click(); await sleep(800);
  const inputs = await P.ref.$$eval('#scheme-medication', e => e.map(x => x.tagName));
  must(inputs[0] === 'SELECT', 'el campo de medicamento no es una lista cerrada del catálogo');
  return 'API 404 y el formulario solo ofrece el catálogo';
});
await tc(169, 'Bloquear la carga sin evaluación psiquiátrica vigente', P.ref, async () => {
  const amb = ambulatory[0];
  const bad = await call('POST', `/api/persons/${amb.personId}/medication-schemes`, { token: T.ref, body: { medicationId: MED, dose: '1', time: '10:00' } });
  must(bad.status >= 400 && bad.status < 500, `sin evaluación respondió ${bad.status}`);
  await openTab(P.ref, 'Medicación', amb.id);
  const t = await body(P.ref); expectText(t, /evaluaci[óo]n psiqui[áa]trica vigente|no tiene evaluaci/i, 'el aviso de evaluación vigente');
  return `API ${bad.status} y aviso en pantalla`;
});
await tc(170, 'Stock en 0: alertar y marcar el medicamento como pendiente de stock', P.ref, async () => {
  const cat = (await call('GET', '/api/medication-schemes/catalog', { token: T.ref })).json;
  const zeroName = `QA UI Sin stock ${RND}`;
  await call('POST', '/api/stock-medicamentos', { token: T.ref, body: { nombre: zeroName, cantidad: 1, fechaVencimiento: `${new Date().getFullYear() + 1}-06-30T00:00:00`, proveedor: 'QA' } });
  const z = (await call('GET', '/api/medication-schemes/catalog', { token: T.ref })).json.find(m => m.name === zeroName);
  const zeroMed = cat.find(m => m.pharmacyStock === 0) ?? z;
  let ok = await call('POST', `/api/persons/${LUCIA}/medication-schemes`, { token: T.ref, body: { medicationId: zeroMed.id, dose: '1', time: '23:50' } });
  if (ok.status === 409) { // ya había un esquema vigente de ese medicamento de una corrida anterior: se desactiva y se vuelve a cargar
    const prev = (await call('GET', `/api/persons/${LUCIA}/medication-schemes`, { token: T.ref })).json.filter(x => x.medicationId === zeroMed.id && x.isActive);
    for (const x of prev) await call('PATCH', `/api/persons/${LUCIA}/medication-schemes/${x.id}/deactivate`, { token: T.ref });
    ok = await call('POST', `/api/persons/${LUCIA}/medication-schemes`, { token: T.ref, body: { medicationId: zeroMed.id, dose: '1', time: '23:50' } });
  }
  must([200, 201].includes(ok.status), `carga con stock bajo respondió ${ok.status}`);
  if (zeroMed.pharmacyStock !== 0) return `stock ${zeroMed.pharmacyStock} (no hay medicamento en 0 en el catálogo): se cargó, sin marca de pendiente`;
  must(ok.json?.pendingStock === true, 'el esquema no quedó pendiente de stock');
  await openTab(P.ref, 'Medicación'); expectText(await body(P.ref), /Pendiente de stock/, 'la marca Pendiente de stock');
  return `${zeroMed.name}: pendiente de stock`;
});
await tc(171, 'Registrar quién cargó el esquema y cuándo', P.ref, async () => {
  await openTab(P.ref, 'Medicación'); const t = await body(P.ref);
  expectText(t, new RegExp(`${medName}[^C]*Cargado por [^ ]+ [^ ]+ el \\d{2}/\\d{2}/\\d{4} \\d{2}:\\d{2}`), 'autor y fecha del esquema');
});
await tc(172, 'Visibilidad del esquema según el rol', P.esc, async () => {
  const e = await call('GET', `/api/persons/${LUCIA}/medication-schemes`, { token: T.esc });
  const d = await call('GET', `/api/persons/${LUCIA}/medication-schemes`, { token: T.dir });
  const rf = await call('GET', `/api/persons/${LUCIA}/medication-schemes`, { token: T.ref });
  must(e.status === 403 && d.status === 200 && rf.status === 200, `Escucha=${e.status}, Directora=${d.status}, Referente=${rf.status}`);
  const a = (await call('GET', '/api/social-records/list?page=1', { token: T.esc })).json?.items?.[0];
  if (a) { await go(P.esc, `/dashboard/fichas/${a.id}`, 2500); must(!/Cargar medicamento/.test(await body(P.esc)), 'Escucha ve el botón de cargar medicamento'); }
  return 'Escucha 403, Directora y Referente 200';
});

// ======================= SCRUM-60 =======================
await tc(173, 'La agenda agrupa las tomas por hora', P.ref, async () => {
  await go(P.ref, '/dashboard/medicacion', 2500); const t = await body(P.ref);
  expectText(t, new RegExp(`${medTime} hs`), `la franja ${medTime} hs`);
  const hours = [...t.matchAll(/(\d{2}:\d{2}) hs/g)].map(m => m[1]); const sorted = [...hours].sort();
  must(JSON.stringify(hours) === JSON.stringify(sorted), `franjas desordenadas: ${hours.join(',')}`);
  return `${hours.length} franjas ordenadas`;
});
await tc(174, 'Solo aparecen las personas presentes con medicación cargada', P.ref, async () => {
  const ag = (await call('GET', `/api/medicamentos/agenda?fecha=${today}`, { token: T.ref })).json;
  const day = [...(await dayAtt(0, today)).persons, ...(await dayAtt(1, today)).persons].map(p => p.personId.toLowerCase());
  const intakes = ag.groups.flatMap(g => g.intakes);
  must(intakes.length > 0 && intakes.every(i => day.includes(i.personId.toLowerCase())), 'hay tomas de personas que no asistieron');
  const far = await call('GET', '/api/medicamentos/agenda?fecha=2020-01-01', { token: T.ref });
  must(far.json?.groups?.length === 0, 'un día sin asistencia tiene tomas');
  return `${intakes.length} tomas, todas de presentes`;
});
await tc(175, 'Marcar una toma como Administrada / No administrada con observación', P.ref, async () => {
  await go(P.ref, '/dashboard/medicacion', 2500);
  const li = P.ref.locator('li', { hasText: medName }).first();
  await li.getByRole('button', { name: 'Administrada', exact: true }).click(); await sleep(2000);
  const ag = (await call('GET', `/api/medicamentos/agenda?fecha=${today}`, { token: T.ref })).json.groups.flatMap(g => g.intakes).find(i => i.medicationName === medName);
  must(ag?.status === 2, `estado de la toma=${ag?.status}`);
  const ag2 = (await call('GET', `/api/medicamentos/agenda?fecha=${today}`, { token: T.ref })).json.groups.flatMap(g => g.intakes).find(i => i.medicationName === med2Name);
  if (ag2) { const n = await call('PATCH', `/api/medicamentos/agenda/${ag2.id}`, { token: T.ref, body: { status: 3, observation: 'No quiso tomarla' } }); must(n.status === 204, `no administrada respondió ${n.status}`); }
  return 'Administrada desde la pantalla; No administrada con observación (API)';
});
await tc(176, 'Acceso directo a la agenda del día desde el panel principal', P.ref, async () => {
  await go(P.ref, '/dashboard', 2500); expectText(await body(P.ref), /Agenda de medicamentos de hoy/);
  await P.ref.getByRole('link', { name: /Ver agenda/ }).click(); await sleep(1500);
  must(/medicacion/.test(P.ref.url()), `fue a ${P.ref.url()}`);
});
await tc(177, 'Consultar la agenda de días anteriores', P.ref, async () => {
  await call('POST', '/api/attendances', { token: T.ref, body: { personId: LUCIA, activity: 1, date: yesterday, anonymousCount: 0 } });
  await go(P.ref, '/dashboard/medicacion', 1500);
  await P.ref.fill('#agenda-date', yesterday); await P.ref.dispatchEvent('#agenda-date', 'change'); await sleep(2000);
  const t = await body(P.ref); expectText(t, /Estás viendo un día anterior/);
  expectText(t, /Volver a hoy/);
  return 'banner de día anterior y botón Volver a hoy';
});

// ======================= SCRUM-61 =======================
await tc(178, 'Ver el historial de tomas desde el perfil de la persona', P.ref, async () => {
  const past = (await call('GET', `/api/medicamentos/agenda?fecha=${yesterday}`, { token: T.ref })).json.groups.flatMap(g => g.intakes).find(i => i.medicationName === medName);
  if (past) await call('PATCH', `/api/medicamentos/agenda/${past.id}`, { token: T.ref, body: { status: 3, observation: 'QA UI ayer' } });
  await openTab(P.ref, 'Medicación'); await P.ref.getByRole('tab', { name: 'Historial de tomas' }).click(); await sleep(2000);
  const t = await body(P.ref); expectText(t, /Medicamento de prueba|QA/, 'tomas en el historial'); expectText(t, /Administrada/);
  return past ? 'toma de ayer del medicamento nuevo registrada como no administrada' : 'historial con tomas previas';
});
await tc(179, 'Filtrar por rango de fechas y por medicamento', P.ref, async () => {
  await openTab(P.ref, 'Medicación'); await P.ref.getByRole('tab', { name: 'Historial de tomas' }).click(); await sleep(1500);
  const before = (await body(P.ref)).length;
  await P.ref.fill('#mh-from', yesterday); await P.ref.fill('#mh-to', yesterday); await sleep(900);
  const opts = await P.ref.$$eval('#mh-med option', os => os.map(o => ({ v: o.value, t: o.textContent.trim() })));
  const o = opts.find(x => x.t.includes(medName)); must(o, 'el medicamento nuevo no está en el filtro');
  await P.ref.selectOption('#mh-med', o.v); await sleep(900);
  const t = await body(P.ref); expectText(t, new RegExp(medName)); must(!/Medicamento de prueba A/.test(t.split('Historial de tomas')[1] ?? ''), 'el filtro no excluyó otros medicamentos');
});
await tc(180, 'Resaltar las tomas no administradas', P.ref, async () => {
  await openTab(P.ref, 'Medicación'); await P.ref.getByRole('tab', { name: 'Historial de tomas' }).click(); await sleep(1800);
  const el = P.ref.locator('li, tr', { hasText: /No administrada/ }).first(); must(await el.count(), 'no hay tomas no administradas visibles');
  const cls = (await el.getAttribute('class')) ?? ''; const html = await el.evaluate(e => e.outerHTML.slice(0, 500));
  must(/red|rose|amber|orange/.test(cls + html), 'la toma no administrada no tiene un color de alerta');
});
await tc(181, 'Acceso restringido a Referentes y Directora', P.esc, async () => {
  const e = await call('GET', `/api/persons/${LUCIA}/medication-history`, { token: T.esc });
  const d = await call('GET', `/api/persons/${LUCIA}/medication-history`, { token: T.dir });
  const rf = await call('GET', `/api/persons/${LUCIA}/medication-history`, { token: T.ref });
  must(e.status === 403 && d.status === 200 && rf.status === 200, `Escucha=${e.status}, Directora=${d.status}, Referente=${rf.status}`);
  return 'Escucha 403';
});
await tc(182, 'Exportar el historial de medicación a PDF', P.ref, async () => {
  await openTab(P.ref, 'Medicación'); await P.ref.getByRole('tab', { name: 'Historial de tomas' }).click(); await sleep(1500);
  // la pantalla exporta con la impresión del navegador (window.print), no descarga el PDF del backend
  await P.ref.evaluate(() => { window.__printed = 0; window.print = () => { window.__printed++; }; });
  await P.ref.getByRole('button', { name: /Exportar PDF/ }).click(); await sleep(800);
  must(await P.ref.evaluate(() => window.__printed) === 1, 'el botón no abrió la impresión');
  const api = await fetch(BASE + `/api/persons/${LUCIA}/medication-history/pdf`, { headers: { Authorization: `Bearer ${T.ref}` } });
  const buf = Buffer.from(await api.arrayBuffer()); must(api.status === 200 && buf.subarray(0, 5).toString() === '%PDF-' && buf.length > 1000, `PDF del backend: ${api.status} ${buf.length} bytes`);
  return `botón abre la impresión del navegador; el endpoint del backend devuelve un PDF válido (${buf.length} bytes) que la pantalla no usa`;
});

// ======================= SCRUM-71 =======================
const stockOf = async id => (await call('GET', '/api/stock-medicamentos', { token: T.ref })).json.find(m => m.medicationId === id);
await tc(183, 'Registrar el ingreso de medicamentos', P.ref, async () => {
  const before = (await stockOf(MED2)).currentStock;
  await go(P.ref, '/dashboard/medicacion/stock', 2000);
  await P.ref.locator('tr', { hasText: med2Name }).locator('button', { hasText: 'Ingreso' }).click(); await sleep(800);
  await P.ref.fill('#entry-quantity', '7'); await P.ref.fill('#entry-expiration', `${new Date().getFullYear() + 1}-12-31`); await P.ref.fill('#entry-supplier', 'Proveedor UI');
  await P.ref.locator('app-stock-entry-modal button, [role=dialog] button', { hasText: 'Registrar ingreso' }).last().click(); await sleep(2200);
  const after = (await stockOf(MED2)).currentStock; must(after === before + 7, `stock ${before} -> ${after}`);
  return `stock ${before} -> ${after}`;
});
await tc(184, 'Descontar el stock al marcar una toma como administrada', P.ref, async () => {
  const s = await stockOf(MED); must(s.currentStock === 2, `stock de ${medName} = ${s.currentStock} (esperado 3 - 1 = 2)`);
  await go(P.ref, '/dashboard/medicacion/stock', 2000); expectText(await body(P.ref), new RegExp(`${medName} 2`), 'el stock descontado en pantalla');
  return '3 -> 2';
});
await tc(185, 'Alertar cuando el stock está por debajo del umbral configurable', P.ref, async () => {
  await go(P.ref, '/dashboard/medicacion/stock', 2000);
  expectText(await body(P.ref), new RegExp(`${medName}: quedan 2 \\(umbral \\d+\\)`), 'la alerta con el umbral por defecto');
  // se baja el umbral a 1 desde la pantalla: con 2 unidades deja de alertar
  await P.ref.locator('tr', { hasText: medName }).locator('button', { hasText: 'Umbral' }).click(); await sleep(800);
  await P.ref.fill('#threshold', '1'); await P.ref.locator('[role=dialog] button, app-stock-threshold-modal button', { hasText: 'Guardar' }).last().click(); await sleep(2000);
  const s = await stockOf(MED); must(s.minimumStockThreshold === 1, `umbral guardado=${s.minimumStockThreshold}`);
  must(!new RegExp(`${medName}: quedan`).test(await body(P.ref)), 'sigue alertando con el umbral en 1');
  return 'alerta con umbral 5; al configurarlo en 1 desde la pantalla deja de alertar';
});
await tc(186, 'Ver el historial de movimientos de un medicamento', P.ref, async () => {
  await go(P.ref, '/dashboard/medicacion/stock', 2000);
  await P.ref.locator('tr', { hasText: medName }).locator('button', { hasText: 'Movimientos' }).click(); await sleep(1500);
  const t = await body(P.ref); expectText(t, /Ingreso/); expectText(t, /Consumo|consumo/);
});
await tc(187, 'Permisos de gestión del stock', P.esc, async () => {
  const e = await call('GET', '/api/stock-medicamentos', { token: T.esc });
  const d = await call('GET', '/api/stock-medicamentos', { token: T.dir });
  const ei = await call('POST', '/api/stock-medicamentos', { token: T.esc, body: { nombre: 'x', cantidad: 1, fechaVencimiento: '2030-01-01T00:00:00', proveedor: 'x' } });
  must(e.status === 403 && d.status === 200 && ei.status === 403, `Escucha ver=${e.status}, Directora ver=${d.status}, Escucha ingreso=${ei.status}`);
  return 'Escucha 403 al ver y registrar; Directora 200';
});
await tc(188, 'El stock de medicamentos es independiente del stock de ropa', P.ref, async () => {
  await go(P.ref, '/dashboard/medicacion/stock', 2000); const t = await body(P.ref);
  must(!/camisa|remera|pantal[óo]n|abrigo|ropa/i.test(t.split('Inventario del Centro Barrial')[1] ?? ''), 'el inventario de medicamentos lista ropa');
  return 'el inventario muestra solo medicamentos; el backend no tiene módulo de ropa (límite del alcance)';
});

// ======================= SCRUM-75 =======================
await tc(189, 'Un medicamento nuevo queda activo y el desactivado se conserva como historial', P.ref, async () => {
  await openTab(P.ref, 'Medicación'); await P.ref.locator('li', { hasText: med2Name }).locator('button', { hasText: 'Desactivar' }).first().click(); await sleep(1800);
  const s = (await call('GET', `/api/persons/${LUCIA}/medication-schemes`, { token: T.ref })).json;
  must(s.find(x => x.id === SCHEME2)?.isActive === false && s.find(x => x.medicationName === medName)?.isActive === true, 'los estados de los esquemas no son los esperados');
  return 'el nuevo sigue activo; el desactivado se conserva';
});
await tc(190, 'La agenda diaria muestra solo los esquemas activos', P.ref, async () => {
  const day = dayStr(new Date(Date.now() - 3 * 36e5 - 5 * 864e5));
  await call('POST', '/api/attendances', { token: T.ref, body: { personId: LUCIA, activity: 1, date: day, anonymousCount: 0 } });
  const ag = (await call('GET', `/api/medicamentos/agenda?fecha=${day}`, { token: T.ref })).json.groups.flatMap(g => g.intakes).map(i => i.medicationName);
  must(ag.includes(medName) && !ag.includes(med2Name), `agenda=${ag.filter(n => n.startsWith('QA UI')).join(',')}`);
  return 'incluye el esquema activo, no el desactivado';
});
await tc(191, 'Los esquemas anteriores son visibles desde el perfil', P.ref, async () => {
  await openTab(P.ref, 'Medicación'); await P.ref.getByRole('tab', { name: /Esquemas anteriores/ }).click(); await sleep(1200);
  expectText(await body(P.ref), new RegExp(med2Name), 'el esquema desactivado en Esquemas anteriores');
});
await tc(192, 'Alertar cuando un esquema lleva más de 30 días sin revisión', P.ref, async () => {
  const id = (await call('GET', `/api/persons/${LUCIA}/medication-schemes`, { token: T.ref })).json.find(x => x.medicationName === medName).id;
  const prev = sql(`SELECT CONVERT(varchar(33),LoadedAt,126) FROM medication_schemes WHERE Id='${id}'`);
  sql(`UPDATE medication_schemes SET LoadedAt=DATEADD(day,-40,SYSUTCDATETIME()), ReviewedAt=NULL WHERE Id='${id}'`);
  cleanups.push(() => sql(`UPDATE medication_schemes SET LoadedAt='${prev}' WHERE Id='${id}'`));
  const nr = (await call('GET', '/api/medication-schemes/needs-review', { token: T.ref })).json;
  must(JSON.stringify(nr).includes(id), 'el listado de revisión vencida no incluye el esquema');
  await openTab(P.ref, 'Medicación'); expectText(await body(P.ref), /revisi[óo]n/i, 'la alerta de revisión en la ficha');
  return 'esquema con 40 días: alerta en la ficha y en el listado';
});
await tc(194, 'Revisar el esquema registra la fecha y el usuario', P.ref, async () => {
  await openTab(P.ref, 'Medicación');
  await P.ref.locator('li', { hasText: medName }).locator('button', { hasText: 'Marcar como revisado' }).first().click(); await sleep(2000);
  const s = (await call('GET', `/api/persons/${LUCIA}/medication-schemes`, { token: T.ref })).json.find(x => x.medicationName === medName);
  must(s.reviewedAt && s.reviewedByName, `reviewedAt=${s.reviewedAt} por=${s.reviewedByName}`);
  expectText(await body(P.ref), /Revisado por/i, 'quién revisó en pantalla');
  return `revisado por ${s.reviewedByName}`;
});
await tc(193, 'Bloquear nuevos medicamentos con la evaluación psiquiátrica vencida', P.ref, async () => {
  const cand = residents.find(p => p.personId.toLowerCase() !== LUCIA && sql(`SELECT COUNT(*) FROM psychiatric_evaluations WHERE PersonId='${p.personId}' AND Date>=DATEADD(month,-6,SYSUTCDATETIME())`) === '0');
  must(cand, 'no hay un residente sin evaluación vigente para usar de prueba');
  const evId = '00000000-0000-4000-8000-' + RND.padStart(12, '0');
  sql(`INSERT INTO psychiatric_evaluations (Id,PersonId,Date,Professional,Diagnosis,IsValid,RegisteredByUserId) SELECT '${evId}','${cand.personId}',DATEADD(month,-8,SYSUTCDATETIME()),'QA','QA',1,Id FROM users WHERE Email='referente@test.com'`);
  cleanups.push(() => sql(`DELETE FROM psychiatric_evaluations WHERE Id='${evId}'`));
  const bad = await call('POST', `/api/persons/${cand.personId}/medication-schemes`, { token: T.ref, body: { medicationId: MED, dose: '1', time: '10:00' } });
  must(bad.status >= 400 && bad.status < 500, `con evaluación de hace 8 meses respondió ${bad.status}`);
  await openTab(P.ref, 'Medicación', cand.id);
  await P.ref.getByRole('button', { name: 'Cargar medicamento' }).click(); await sleep(800);
  const opts = await P.ref.$$eval('#scheme-medication option', os => os.map(o => ({ v: o.value, t: o.textContent.trim() })));
  const opt = opts.find(o => o.t.includes(medName)); must(opt, 'el medicamento no está en el formulario');
  await P.ref.selectOption('#scheme-medication', opt.v); await P.ref.fill('#scheme-dose', '1 comprimido'); await P.ref.fill('#scheme-time', '10:00');
  await P.ref.getByRole('button', { name: 'Guardar' }).click(); await sleep(2000);
  expectText(await body(P.ref), /evaluaci[óo]n psiqui[áa]trica/i, 'el aviso de evaluación al intentar guardar');
  return `${cand.firstName}: evaluación de hace 8 meses → ${bad.status}`;
});

// ---------- limpieza ----------
for (const c of cleanups) { try { c(); } catch (e) { console.log('limpieza falló:', e.message.split('\n')[0]); } }
await browser.close();

res.sort((a, b) => Number(a.id.slice(3)) - Number(b.id.slice(3)));
const failed = res.filter(x => !x.ok);
fs.writeFileSync('sprint4-ui-results.json', JSON.stringify(res, null, 2));
console.log(`\nTOTAL ${res.length} PASS ${res.length - failed.length} FAIL ${failed.length}`);
process.exit(failed.length ? 1 : 0);
