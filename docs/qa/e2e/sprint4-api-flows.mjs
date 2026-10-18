// Sprint 4: pruebas contra la API real de lo que ya tiene backend (asistencia diaria SCRUM-216/54 y esquema de medicación SCRUM-232/59).
//   API=http://localhost:5000 node sprint4-api-flows.mjs   (stack con datos semilla: Lucía y Carlos con evaluación vigente, Roberto sin vigente)
import fs from 'fs';
const API = process.env.API || 'http://localhost:5000';
const PASS = 'Test1234!';
const res = [];
const rec = (id, name, ok, note = '') => { res.push({ id, name, ok, note }); console.log(`${ok ? 'PASS' : 'FAIL'}  ${id}  ${name}${note ? ' — ' + note : ''}`); };
const call = async (method, path, { token, body } = {}) => {
  const h = { 'Content-Type': 'application/json' }; if (token) h.Authorization = `Bearer ${token}`;
  const r = await fetch(API + path, { method, headers: h, body: body === undefined ? undefined : JSON.stringify(body) });
  const text = await r.text(); let json = null; try { json = JSON.parse(text); } catch { /* no es JSON */ }
  return { status: r.status, json, text };
};
const login = async e => (await call('POST', '/api/auth/login', { body: { email: e, password: PASS } })).json?.token;
const T = { ref: await login('referente@test.com'), dir: await login('directora@test.com'), esc: await login('escucha@test.com'), coo: await login('coordinador@test.com') };

// personas semilla (ids fijos del seed)
const LUCIA = '33438A32-F4E6-4FBA-B868-07F6362B5BFD', CARLOS = '11A6D997-EE86-424F-87F3-B1A1EB564A31', ROBERTO = '8F563145-9CA6-4E3B-ADEB-DDA835611C64';
const MED_A = 'F013939A-D224-4CBF-B4A8-4896612379DE', MED_C = '7C99620A-26BE-476B-9D17-95D7F75F2DC4';
const day = n => new Date(Date.now() + n * 864e5).toISOString().slice(0, 10);
const rnd = n => Math.floor(Math.random() * n);
// fecha distinta por corrida para no chocar con el índice único persona+actividad+fecha de corridas anteriores
const base = -(rnd(300) + 5);
const att = (tok, o) => call('POST', '/api/attendances', { token: tok, body: { personId: LUCIA, activity: 0, date: day(base), anonymousCount: 0, ...o } });

// ---- SCRUM-54 asistencia diaria
let r = await att(T.ref, {});
rec('S4-A01', 'Referente registra la asistencia de una persona en Hogar de día (204)', r.status === 204, `status=${r.status} ${r.text.slice(0, 80)}`);
r = await att(T.ref, {});
rec('S4-A02', 'La misma persona, actividad y fecha no se registra dos veces (409)', r.status === 409, `status=${r.status} ${r.json?.message ?? ''}`);
r = await att(T.ref, { activity: 1 });
rec('S4-A03', 'La misma persona sí puede asistir a otra actividad el mismo día (204)', r.status === 204, `status=${r.status}`);
r = await att(T.ref, { personId: CARLOS, anonymousCount: 4 });
rec('S4-A04', 'Se acepta un conteo de personas sin ficha (204)', r.status === 204, `status=${r.status}`);
r = await att(T.ref, { personId: CARLOS, date: day(base - 400), anonymousCount: -1 });
rec('S4-A05', 'Un conteo negativo se rechaza con mensaje de validación (400)', r.status === 400 && /negativa/i.test(r.text), `status=${r.status}`);
r = await att(T.ref, { date: day(3) });
rec('S4-A06', 'No se registra asistencia con fecha futura (400)', r.status === 400 && /futura/i.test(r.text), `status=${r.status}`);
r = await att(T.ref, { personId: '00000000-0000-0000-0000-0000000000aa', date: day(base - 401) });
rec('S4-A07', 'Una persona inexistente responde 404', r.status === 404, `status=${r.status}`);
r = await att(T.ref, { personId: CARLOS, activity: 9, date: day(base - 402) });
rec('S4-A08', 'Una actividad inválida se rechaza sin error 500 (400)', r.status === 400, `status=${r.status}`);
r = await att(undefined, { date: day(base - 403) });
rec('S4-A09', 'Sin sesión responde 401', r.status === 401, `status=${r.status}`);
r = await att(T.esc, { personId: CARLOS, date: day(base - 404) });
rec('S4-A10', 'Escucha puede registrar asistencia del día a día del Centro Barrial (204)', r.status === 204, `status=${r.status}`);
r = await att(T.coo, { personId: LUCIA, date: day(base - 405) });
rec('S4-A11', 'Coordinador de Casona registra asistencia de un residente (204)', r.status === 204, `status=${r.status}`);

// ---- SCRUM-59 esquema de medicación
const med = (tok, p, o) => call('POST', `/api/persons/${p}/medication-schemes`, { token: tok, body: { medicationId: MED_A, dose: '1 comprimido', time: '08:00', ...o } });
const hhmm = h => `${String(h).padStart(2, '0')}:${String(rnd(50) + 10)}`;
// asegurar que las tres son Residentes (si ya lo son, el cambio no rompe)
for (const p of [LUCIA, CARLOS, ROBERTO]) await call('PUT', `/api/persons/${p}/type`, { token: T.ref, body: { personType: 'Resident' } });
const m1 = await med(T.ref, LUCIA, { time: hhmm(rnd(9)) });
rec('S4-M01', 'Referente carga un esquema a un Residente con evaluación vigente (201 con el esquema completo)', m1.status === 201 && !!m1.json?.medicationName && m1.json?.isActive === true, `status=${m1.status} ${m1.json?.medicationName ?? m1.text.slice(0, 100)} pendingStock=${m1.json?.pendingStock}`);
const m2 = await med(T.ref, LUCIA, { time: m1.json?.time ?? '08:00' });
rec('S4-M02', 'Un esquema duplicado vigente se rechaza (409)', m2.status === 409, `status=${m2.status} ${m2.json?.message ?? ''}`);
const m3 = await med(T.dir, CARLOS, { medicationId: MED_C, time: hhmm(10 + rnd(9)) });
rec('S4-M03', 'Un medicamento sin stock se carga igual y queda marcado como pendiente de stock (201)', m3.status === 201 && m3.json?.pendingStock === true, `status=${m3.status} pendingStock=${m3.json?.pendingStock}`);
const m4 = await med(T.ref, ROBERTO, {});
rec('S4-M04', 'Sin evaluación psiquiátrica vigente no se puede cargar el esquema (4xx)', m4.status >= 400 && m4.status < 500, `status=${m4.status} ${m4.json?.message ?? m4.text.slice(0, 120)}`);
const m5 = await med(T.esc, LUCIA, { time: '07:15' });
rec('S4-M05', 'Escucha no puede cargar esquemas (403)', m5.status === 403, `status=${m5.status}`);
const m6 = await med(T.ref, LUCIA, { time: '25:00' });
rec('S4-M06', 'Una hora inválida se rechaza (400)', m6.status === 400, `status=${m6.status}`);
const m7 = await med(T.ref, LUCIA, { dose: '' });
rec('S4-M07', 'La dosis vacía se rechaza (400)', m7.status === 400, `status=${m7.status}`);
const m8 = await med(T.ref, LUCIA, { medicationId: 'aaaaaaaa-0000-0000-0000-000000000000', time: '06:30' });
rec('S4-M08', 'Un medicamento fuera del catálogo responde 404', m8.status === 404, `status=${m8.status}`);
const m9 = await med(T.ref, '00000000-0000-0000-0000-0000000000bb', {});
rec('S4-M09', 'Una persona inexistente responde 404', m9.status === 404, `status=${m9.status}`);
const m10 = await med(undefined, LUCIA, {});
rec('S4-M10', 'Sin sesión responde 401', m10.status === 401, `status=${m10.status}`);

const f = res.filter(x => !x.ok);
console.log(`\nTOTAL ${res.length} PASS ${res.length - f.length} FAIL ${f.length}`);
fs.writeFileSync(process.env.OUT || './sprint4-api-results.json', JSON.stringify(res, null, 1));
