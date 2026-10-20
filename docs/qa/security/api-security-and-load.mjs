// Pruebas de seguridad básicas y de carga liviana contra la API local (Node 18+, sin dependencias).
//   API=http://localhost:5000 WEB=http://localhost:4200 node api-security-and-load.mjs
// No es un pentest: cubre autenticación, autorización por rol, manipulación de JWT, inyección, XSS almacenado,
// cabeceras, política de contraseñas, enumeración de usuarios, payloads grandes y una carga de 20-50 usuarios concurrentes.
import fs from 'fs';
const API = process.env.API || 'http://localhost:5000';
const WEB = process.env.WEB || 'http://localhost:4200';
const PASS = 'Test1234!';
const results = [];
const log = (id, name, ok, note = '') => { results.push({ id, name, ok, note }); console.log(`${ok ? 'PASS' : 'FAIL'}  ${id}  ${name}${note ? ' — ' + note : ''}`); };
const call = async (method, path, { token, body, headers = {} } = {}) => {
  const h = { 'Content-Type': 'application/json', ...headers };
  if (token) h.Authorization = `Bearer ${token}`;
  const t0 = performance.now();
  const r = await fetch(API + path, { method, headers: h, body: body === undefined ? undefined : (typeof body === 'string' ? body : JSON.stringify(body)) });
  const text = await r.text();
  let json = null; try { json = JSON.parse(text); } catch { /* no es JSON */ }
  return { status: r.status, json, text, ms: performance.now() - t0, headers: r.headers };
};
const login = async email => (await call('POST', '/api/auth/login', { body: { email, password: PASS } })).json?.token;
const b64 = o => Buffer.from(JSON.stringify(o)).toString('base64url');

const tok = { Referente: await login('referente@test.com'), Directora: await login('directora@test.com'), Escucha: await login('escucha@test.com'), Coordinador: await login('coordinador@test.com') };

// SEC-01 sin token
const protectedRoutes = ['/api/social-records', '/api/social-records/list', '/api/auth/users/active', '/api/notifications', '/api/collaborators', '/api/general-calendar-events', '/api/personal-calendar-events'];
const noTok = await Promise.all(protectedRoutes.map(async p => [p, (await call('GET', p)).status]));
log('SEC-01', 'Los endpoints protegidos responden 401 sin token', noTok.every(([, s]) => s === 401), noTok.filter(([, s]) => s !== 401).map(x => x.join(':')).join(' ') || `${noTok.length} rutas`);

// SEC-02 autorización por rol
const r1 = await call('POST', '/api/social-records', { token: tok.Escucha, body: { firstName: 'X', lastName: 'Y' } });
const r2 = await call('GET', '/api/auth/users/active', { token: tok.Escucha });
const r3 = await call('GET', '/api/auth/users/active', { token: tok.Coordinador });
const r4 = await call('GET', '/api/auth/users/active', { token: tok.Directora });
log('SEC-02', 'Escucha no crea fichas (403) y Escucha / Coordinador / Directora no listan usuarios (403)', r1.status === 403 && r2.status === 403 && r3.status === 403 && r4.status === 403, `crear=${r1.status} escucha=${r2.status} coord=${r3.status} dir=${r4.status}`);

// SEC-03 JWT manipulado
const parts = tok.Escucha.split('.');
const payload = JSON.parse(Buffer.from(parts[1], 'base64url').toString());
const roleKey = Object.keys(payload).find(k => k.endsWith('/role')) || 'role';
const forged = { ...payload, [roleKey]: 'Referente' };
const tampered = `${parts[0]}.${b64(forged)}.${parts[2]}`;
const none = `${b64({ alg: 'none', typ: 'JWT' })}.${b64(forged)}.`;
const t1 = await call('GET', '/api/auth/users/active', { token: tampered });
const t2 = await call('GET', '/api/auth/users/active', { token: none });
const t3 = await call('GET', '/api/auth/users/active', { token: 'abc.def.ghi' });
log('SEC-03', 'Un JWT con el rol alterado, con alg=none o malformado se rechaza (401)', [t1, t2, t3].every(x => x.status === 401), `firma alterada=${t1.status} alg none=${t2.status} basura=${t3.status}`);

// SEC-04 inyección SQL en la búsqueda
const full = await call('GET', '/api/social-records/list', { token: tok.Referente });
const inj = await call('GET', `/api/social-records/list?search=${encodeURIComponent("' OR 1=1 --")}`, { token: tok.Referente });
const inj2 = await call('GET', `/api/social-records/list?search=${encodeURIComponent("'; DROP TABLE Persons; --")}`, { token: tok.Referente });
const sizeOf = r => r.json?.totalCount ?? r.json?.total ?? r.json?.items?.length ?? (Array.isArray(r.json) ? r.json.length : -1);
log('SEC-04', 'Un intento de inyección SQL en la búsqueda no da 500 ni devuelve todo el listado', inj.status < 500 && inj2.status < 500 && sizeOf(inj) < sizeOf(full), `total=${sizeOf(full)} con inyección=${sizeOf(inj)} (coincidencias de texto, no un OR 1=1) drop=${inj2.status}`);

// SEC-05 enumeración de usuarios
const e1 = await call('POST', '/api/auth/login', { body: { email: 'no.existe@test.com', password: 'Mala1234!' } });
const e2 = await call('POST', '/api/auth/login', { body: { email: 'referente@test.com', password: 'Mala1234!' } });
log('SEC-05', 'El login no revela si el correo existe (misma respuesta)', e1.status === e2.status && e1.text === e2.text, `${e1.status} "${e1.text.slice(0, 50)}" vs ${e2.status} "${e2.text.slice(0, 50)}"`);

// SEC-06 política de contraseñas
const w = await call('POST', '/api/auth/register', { body: { firstName: 'Weak', lastName: 'Pass', email: `weak.${Date.now()}@test.com`, password: '123' } });
log('SEC-06', 'El registro rechaza una contraseña débil (400)', w.status === 400, `status=${w.status}`);

// SEC-07 asignación masiva de rol al registrarse
const mailMass = `mass.${Date.now()}@test.com`;
const m = await call('POST', '/api/auth/register', { body: { firstName: 'Mass', lastName: 'Assign', email: mailMass, password: PASS, role: 'Referente', roleId: '11111111-1111-1111-1111-111111111111', status: 'Active' } });
const ml = await call('POST', '/api/auth/login', { body: { email: mailMass, password: PASS } });
log('SEC-07', 'Enviar role/roleId/status en el registro no da una cuenta activa ni con rol', ml.status === 403 && !ml.json?.token, `registro=${m.status} login=${ml.status} ${ml.json?.status ?? ''}`);

// SEC-08 XSS almacenado (la API guarda texto; el escape lo hace Angular, se verifica en la UI)
const xss = '<img src=x onerror=window.__xss=1>';
const x = await call('POST', '/api/social-records', { token: tok.Referente, body: { firstName: xss, lastName: 'Xss' + Date.now() } });
log('SEC-08a', 'La API acepta o rechaza con 4xx un nombre con HTML, sin error 500', x.status < 500, `status=${x.status}`);

// SEC-09 cabeceras de seguridad
const web = await fetch(WEB + '/auth');
const hs = Object.fromEntries(web.headers.entries());
const api = await fetch(API + '/api/auth/login', { method: 'OPTIONS' });
const need = ['x-content-type-options', 'x-frame-options', 'content-security-policy', 'referrer-policy'];
const missing = need.filter(h => !hs[h]);
log('SEC-09', 'El frontend envía las cabeceras de seguridad (nosniff, X-Frame-Options, CSP, Referrer-Policy)', missing.length === 0, missing.length ? `faltan: ${missing.join(', ')}` : '');
log('SEC-10', 'La API no expone la versión del servidor (cabecera Server/X-Powered-By)', !api.headers.get('x-powered-by') && !/\d/.test(api.headers.get('server') ?? ''), `server=${api.headers.get('server')} x-powered-by=${api.headers.get('x-powered-by')}`);

// SEC-11 payload grande
const big = await call('POST', '/api/social-records', { token: tok.Referente, body: { firstName: 'A'.repeat(2_000_000), lastName: 'Big' } });
log('SEC-11', 'Un payload de 2 MB se rechaza sin error 500', big.status >= 400 && big.status < 500, `status=${big.status}`);

// SEC-12 JSON inválido
const bad = await call('POST', '/api/social-records', { token: tok.Referente, body: '{"firstName": ' });
log('SEC-12', 'Un JSON malformado devuelve 400 y no expone stack trace', bad.status === 400 && !/at Vicaria|System\./.test(bad.text), `status=${bad.status}`);

// SEC-13 IDOR: ficha inexistente / id no guid
const id1 = await call('GET', '/api/social-records/00000000-0000-0000-0000-000000000000', { token: tok.Referente });
const id2 = await call('GET', '/api/social-records/1;DROP', { token: tok.Referente });
log('SEC-13', 'Un id inexistente da 404 y un id mal formado da 4xx, sin 500', id1.status === 404 && id2.status >= 400 && id2.status < 500, `inexistente=${id1.status} malformado=${id2.status}`);

// ---------- Carga ----------
const pct = (a, p) => a.sort((x, y) => x - y)[Math.min(a.length - 1, Math.floor(a.length * p))];
const load = async (name, n, total, fn) => {
  const lat = []; let errors = 0, next = 0; const t0 = performance.now();
  await Promise.all(Array.from({ length: n }, async () => {
    while (next < total) { next++; try { const r = await fn(); lat.push(r.ms); if (r.status >= 400) errors++; } catch { errors++; } }
  }));
  const secs = (performance.now() - t0) / 1000;
  const o = { name, concurrentes: n, requests: total, errores: errors, rps: +(total / secs).toFixed(1), p50: Math.round(pct(lat, .5)), p95: Math.round(pct(lat, .95)), max: Math.round(Math.max(...lat)) };
  console.log('LOAD', JSON.stringify(o)); return o;
};
const loads = [];
loads.push(await load('GET /api/social-records/list', 50, 500, () => call('GET', '/api/social-records/list', { token: tok.Referente })));
loads.push(await load('GET /api/notifications', 50, 500, () => call('GET', '/api/notifications', { token: tok.Referente })));
loads.push(await load('GET /api/general-calendar-events', 30, 300, () => call('GET', '/api/general-calendar-events', { token: tok.Referente })));
loads.push(await load('POST /api/auth/login (bcrypt)', 20, 100, () => call('POST', '/api/auth/login', { body: { email: 'directora@test.com', password: PASS } })));
log('PERF-01', 'Listado de fichas con 50 usuarios concurrentes: sin errores y p95 < 2000 ms', loads[0].errores === 0 && loads[0].p95 < 2000, `p95=${loads[0].p95} ms, ${loads[0].rps} req/s`);
log('PERF-02', 'Notificaciones con 50 usuarios concurrentes: sin errores y p95 < 2000 ms', loads[1].errores === 0 && loads[1].p95 < 2000, `p95=${loads[1].p95} ms, ${loads[1].rps} req/s`);
log('PERF-03', 'Calendario con 30 usuarios concurrentes: sin errores y p95 < 2000 ms', loads[2].errores === 0 && loads[2].p95 < 2000, `p95=${loads[2].p95} ms, ${loads[2].rps} req/s`);
log('PERF-04', 'Login con 20 usuarios concurrentes: sin errores y p95 < 3000 ms', loads[3].errores === 0 && loads[3].p95 < 3000, `p95=${loads[3].p95} ms, ${loads[3].rps} req/s`);

const failed = results.filter(r => !r.ok);
console.log(`\nTOTAL ${results.length}  PASS ${results.length - failed.length}  FAIL ${failed.length}`);
fs.writeFileSync(process.env.OUT || './security-results.json', JSON.stringify({ results, loads }, null, 1));
