// QA de UI Sprint 1-2 (Playwright): acceso y roles (EP-03) y fichas / observaciones / historia de vida (EP-01, EP-02, EP-12).
// Requiere el stack local (`docker compose up -d` en backend/) y `npm i playwright` en la carpeta desde donde se corre.
//   BASE=http://localhost:4200 SHOTS=./shots node sprint1-2-ui-flows.mjs [auth|fichas|all]
// Cada verificación imprime PASS/FAIL con el TC del plan al que corresponde.
import { chromium } from 'playwright';
import fs from 'fs';
const BASE = process.env.BASE || 'http://localhost:4200';
const SHOTS = process.env.SHOTS || './shots';
const ONLY = process.argv[2] || 'all';
fs.mkdirSync(SHOTS, { recursive: true });
const RND = String(Date.now()).slice(-7);
const PASS = 'Test1234!';
const browser = await chromium.launch();
const results = [];
const log = (tc, name, ok, note = '') => { results.push({ tc, name, ok, note }); console.log(`${ok ? 'PASS' : 'FAIL'}  ${tc}  ${name}${note ? ' — ' + note : ''}`); };
let bad = [], errs = [];
const mk = async (vp = { width: 1400, height: 950 }) => {
  const ctx = await browser.newContext({ viewport: vp });
  const page = await ctx.newPage();
  page.on('console', m => { if (m.type() === 'error' && !/ERR_ABORTED/.test(m.text())) errs.push(m.text().slice(0, 160)); });
  page.on('response', r => { if (r.url().includes('/api/') && r.status() >= 400) bad.push(`${r.status()} ${r.request().method()} ${r.url().replace(BASE, '')}`); });
  return page;
};
const body = async page => (await page.innerText('body')).replace(/\s+/g, ' ');
const shot = (page, n) => page.screenshot({ path: `${SHOTS}/${n}.png` });
const fillLogin = async (page, email, pass = PASS) => {
  await page.goto(BASE + '/auth'); await page.waitForSelector('input[formControlName="email"]');
  await page.fill('input[formControlName="email"]', email);
  await page.fill('input[formControlName="password"]', pass);
};
const login = async (page, email) => { await fillLogin(page, email); await page.click('button[type="submit"]'); await page.waitForURL(/dashboard/, { timeout: 15000 }); };
const logout = async page => { await page.getByText('Cerrar sesión').first().click(); await page.waitForURL(/auth/, { timeout: 8000 }).catch(() => {}); };
const step = async (tc, name, fn) => { bad = []; errs = []; try { await fn(); } catch (e) { log(tc, name, false, 'excepción: ' + e.message.split('\n')[0].slice(0, 200)); } };

if (ONLY === 'auth' || ONLY === 'all') {
  const NEW = `qa.${RND}@test.com`;
  const p = await mk();

  await step('TC-01', 'Registro: la cuenta queda Pendiente', async () => {
    await p.goto(BASE + '/auth/register'); await p.waitForSelector('input[formControlName="name"]');
    await p.fill('input[formControlName="name"]', 'Qa');
    await p.fill('input[formControlName="lastname"]', `Prueba${RND}`);
    await p.fill('input[formControlName="email"]', NEW);
    await p.fill('input[formControlName="password"]', PASS);
    await p.click('button[type="submit"]'); await p.waitForTimeout(2000);
    await shot(p, 'TC-01-registro');
    const t = await body(p);
    log('TC-01', 'Registro: la cuenta queda Pendiente', /pendiente|aprobaci|revis/i.test(t) && !bad.length, `${p.url().replace(BASE, '')} ${bad.join(' | ')}`);
  });

  await step('TC-32', 'Registro con correo ya registrado', async () => {
    await p.goto(BASE + '/auth/register'); await p.waitForSelector('input[formControlName="name"]');
    await p.fill('input[formControlName="name"]', 'Qa'); await p.fill('input[formControlName="lastname"]', 'Dup');
    await p.fill('input[formControlName="email"]', NEW); await p.fill('input[formControlName="password"]', PASS);
    await p.click('button[type="submit"]'); await p.waitForTimeout(1800);
    await shot(p, 'TC-32-duplicado');
    const t = await body(p);
    log('TC-32', 'Registro con correo ya registrado', /ya (est[aá] )?registrad|ya existe|en uso/i.test(t) && p.url().includes('register'), t.slice(0, 160));
  });

  await step('TC-31', 'Login con campos vacíos no se envía', async () => {
    await p.goto(BASE + '/auth'); await p.waitForSelector('input[formControlName="email"]');
    let posted = false; p.on('request', r => { if (r.url().includes('/api/auth/login')) posted = true; });
    await p.click('button[type="submit"]'); await p.waitForTimeout(800);
    await shot(p, 'TC-31-login-vacio');
    log('TC-31', 'Login con campos vacíos no se envía', !posted && p.url().includes('auth'), (await body(p)).slice(0, 140));
  });

  await step('TC-03', 'Login con credenciales incorrectas', async () => {
    await fillLogin(p, 'referente@test.com', 'Incorrecta1!'); await p.click('button[type="submit"]'); await p.waitForTimeout(1500);
    await shot(p, 'TC-03-login-mal');
    log('TC-03', 'Login con credenciales incorrectas muestra error y no entra', p.url().includes('auth') && /incorrect|inv[aá]lid|credenciales/i.test(await body(p)), (await body(p)).slice(0, 140));
  });

  await step('TC-01b', 'Cuenta pendiente no puede entrar', async () => {
    await fillLogin(p, NEW); await p.click('button[type="submit"]'); await p.waitForTimeout(1800);
    await shot(p, 'TC-01b-pendiente-login');
    log('TC-01', 'Una cuenta Pendiente no inicia sesión', !p.url().includes('dashboard') && /pendiente|aprobaci/i.test(await body(p)), p.url().replace(BASE, ''));
  });

  const r = await mk();
  await login(r, 'referente@test.com');

  await step('TC-09', 'Notificación por cuenta pendiente', async () => {
    await r.getByRole('button', { name: 'Ver notificaciones' }).click(); await r.waitForTimeout(700);
    await shot(r, 'TC-09-notificaciones');
    const t = await body(r);
    log('TC-09', 'El Referente recibe notificación de cuenta pendiente', /pendiente|nueva cuenta|solicitud/i.test(t), t.slice(0, 200));
    await r.keyboard.press('Escape');
  });

  await step('TC-02', 'Aprobar cuenta pendiente', async () => {
    await r.goto(BASE + '/dashboard/users'); await r.waitForTimeout(1500);
    const row = r.locator('tr', { hasText: NEW });
    await shot(r, 'TC-02-pendientes');
    const visible = await row.count();
    await row.getByRole('button', { name: 'Aprobar' }).click(); await r.waitForTimeout(600);
    await shot(r, 'TC-02-modal-aprobar');
    await r.locator('app-approve-user-modal button', { hasText: /Escucha/i }).first().click();
    await r.locator('app-approve-user-modal').getByRole('button', { name: /Aprobar|Confirmar/ }).last().click(); await r.waitForTimeout(1800);
    await shot(r, 'TC-02-aprobada');
    const gone = (await r.locator('tr', { hasText: NEW }).count()) === 0;
    log('TC-02', 'Un Referente aprueba una cuenta pendiente asignando rol', visible > 0 && gone && !bad.length, bad.join(' | '));
  });

  await step('TC-02b', 'La cuenta aprobada ya puede ingresar', async () => {
    const q = await mk(); await login(q, NEW); await shot(q, 'TC-02b-ingreso');
    log('TC-02', 'La cuenta aprobada inicia sesión', q.url().includes('dashboard'), q.url().replace(BASE, ''));
    await q.context().close();
  });

  await step('TC-26', 'Referente no puede reasignarse su propio rol', async () => {
    await r.goto(BASE + '/dashboard/users'); await r.waitForTimeout(1200);
    await r.getByRole('button', { name: /Activos/ }).first().click().catch(() => {}); await r.waitForTimeout(1000);
    const own = r.locator('tr', { hasText: 'referente@test.com' });
    const dis = await own.getByRole('button', { name: 'Reasignar Rol' }).isDisabled().catch(() => null);
    const dis2 = await own.getByRole('button', { name: 'Inactivar' }).isDisabled().catch(() => null);
    await shot(r, 'TC-26-activos');
    log('TC-26', 'Los botones Reasignar Rol e Inactivar de la propia cuenta están deshabilitados', dis === true && dis2 === true, `rol=${dis} inactivar=${dis2}`);
  });

  r.on('dialog', d => d.accept());
  await step('TC-07', 'Desactivar una cuenta y verificar el mensaje al ingresar', async () => {
    await r.goto(BASE + '/dashboard/users'); await r.waitForTimeout(1200);
    await r.getByRole('button', { name: /Activos/ }).first().click(); await r.waitForTimeout(1000);
    await r.locator('tr', { hasText: NEW }).getByRole('button', { name: 'Inactivar' }).click(); await r.waitForTimeout(1800);
    await shot(r, 'TC-07-inactivada');
    await r.getByRole('button', { name: /Inactivos|Suspendidos/ }).first().click(); await r.waitForTimeout(1200);
    const inInactive = (await r.locator('tr', { hasText: NEW }).count()) > 0;
    log('TC-07', 'Al inactivar, la cuenta pasa a la pestaña de inactivos', inInactive && !bad.length, bad.join(' | '));
    const q = await mk(); await fillLogin(q, NEW); await q.click('button[type="submit"]'); await q.waitForTimeout(1500);
    await shot(q, 'TC-07-login-inactiva');
    const t = await body(q);
    log('TC-07', 'La cuenta desactivada no ingresa y el mensaje informa que está desactivada', !q.url().includes('dashboard') && /desactiv|inactiv|inhabilit/i.test(t), t.slice(40, 160));
    await q.context().close();
  });

  await step('TC-08', 'Reactivar una cuenta', async () => {
    await r.locator('tr', { hasText: NEW }).getByRole('button', { name: 'Activar' }).click(); await r.waitForTimeout(1800);
    await shot(r, 'TC-08-reactivada');
    const q = await mk(); await login(q, NEW);
    log('TC-08', 'Una cuenta reactivada vuelve a ingresar', q.url().includes('dashboard') && !bad.length, bad.join(' | '));
    await q.context().close();
  });

  await step('TC-04', 'Bloqueo tras 5 intentos fallidos', async () => {
    const q = await mk(); const statuses = [], msgs = [];
    q.on('response', r2 => { if (r2.url().includes('/api/auth/login')) statuses.push(r2.status()); });
    for (let i = 0; i < 5; i++) {
      await fillLogin(q, NEW, `Mala${i}!aaa`); await q.click('button[type="submit"]'); await q.waitForTimeout(900);
      msgs.push((await body(q)).match(/Correo o contraseña incorrectos|bloquead[^.]*\.|No pudimos[^.]*\./i)?.[0] ?? '?');
    }
    await shot(q, 'TC-04-quinto-intento');
    await fillLogin(q, NEW); await q.click('button[type="submit"]'); await q.waitForTimeout(1500);
    await shot(q, 'TC-04-clave-correcta');
    const t6 = await body(q);
    log('TC-04', 'Con la clave correcta la cuenta bloqueada no ingresa y se informa el bloqueo', !q.url().includes('dashboard') && /bloquead/i.test(t6), `URL ${q.url().replace(BASE, '')} | HTTP ${statuses.join(',')} | msgs: ${msgs.join(' / ')} | 6º: ${t6.slice(40, 130)}`);
    log('TC-04', 'El quinto intento fallido informa que se alcanzó el máximo de intentos', /bloquead/i.test(msgs[4]), `5º intento: ${msgs[4]}`);
    await q.context().close();
  });
}

if (ONLY === 'fichas' || ONLY === 'all') {
  const NAME = `José${RND}`, SURN = 'Núñez';
  const DNI = String(30000000 + Number(RND) % 9999999);
  const r = await mk(); await login(r, 'referente@test.com');
  r.on('dialog', d => d.accept());
  let fichaUrl = '';
  const futureDate = new Date(Date.now() + 5 * 86400000).toISOString().slice(0, 10);
  const firstMatch = (t, re) => (t.match(re)?.[0] ?? '').slice(0, 140);

  await step('TC-11', 'Crear una ficha', async () => {
    await r.goto(BASE + '/dashboard/fichas/crear'); await r.waitForSelector('input[formControlName="firstName"]');
    await r.fill('input[formControlName="firstName"]', NAME); await r.fill('input[formControlName="lastName"]', SURN);
    await r.fill('input[formControlName="dni"]', DNI);
    await shot(r, 'TC-11-form');
    await r.getByRole('button', { name: 'Guardar ficha' }).click(); await r.waitForTimeout(2500);
    await shot(r, 'TC-11-guardada');
    const t0 = await body(r);
    await r.goto(BASE + '/dashboard/fichas'); await r.waitForTimeout(1000);
    await r.locator('input[placeholder^="Buscar por nombre"]').fill(DNI); await r.waitForTimeout(1800);
    await r.locator('tr', { hasText: NAME }).first().getByLabel('Ver ficha').click(); await r.waitForURL(/fichas\/[0-9a-f-]{36}/, { timeout: 8000 }).catch(() => {});
    fichaUrl = r.url();
    await shot(r, 'TC-11-detalle');
    const t = await body(r);
    log('TC-11', 'Un Referente crea una ficha con los datos mínimos y la ve en el listado y el detalle', /fichas\/[0-9a-f-]{36}/.test(fichaUrl) && t.includes(NAME) && !bad.length , `${fichaUrl.replace(BASE, '')} ${bad.join(' | ')}`);
  });

  await step('TC-11b', 'Validaciones del alta', async () => {
    await r.goto(BASE + '/dashboard/fichas/crear'); await r.waitForSelector('input[formControlName="firstName"]');
    await r.getByRole('button', { name: 'Guardar ficha' }).click(); await r.waitForTimeout(700);
    await shot(r, 'TC-11b-vacio');
    const t = await body(r);
    log('TC-11', 'No se guarda una ficha vacía y se muestran los obligatorios', /obligatori|requerid/i.test(t) && r.url().includes('crear'), firstMatch(t, /[^.]*obligatori[^.]*\./i));
    // Decisión de producto pendiente: el backend acepta fichas con DNI repetido (ver hoja "Defectos y pendientes").
    await r.fill('input[formControlName="dni"]', 'abc123'); await r.waitForTimeout(400);
    const t3 = await body(r);
    log('TC-11', 'El DNI solo admite dígitos', /d[ií]gitos|n[uú]meros|solo/i.test(t3), firstMatch(t3, /[^.]*(d[ií]gitos|n[uú]meros)[^.]*\./i));
  });

  await step('TC-14', 'Fechas futuras no se aceptan', async () => {
    await r.goto(BASE + '/dashboard/fichas/crear'); await r.waitForSelector('input[formControlName="firstName"]');
    await r.fill('input[formControlName="firstName"]', 'Fut'); await r.fill('input[formControlName="lastName"]', 'Uro');
    await r.getByText(/Más información|Mas informaci/i).first().click().catch(() => {}); await r.waitForTimeout(400);
    await r.fill('input[formControlName="dateOfBirth"]', futureDate).catch(() => {});
    await r.fill('input[formControlName="entryDate"]', futureDate).catch(() => {});
    await r.getByRole('button', { name: 'Guardar ficha' }).click(); await r.waitForTimeout(1500);
    await shot(r, 'TC-14-futura');
    const t = await body(r);
    log('TC-14', 'No se acepta una fecha futura en la ficha', r.url().includes('crear') && /futur|posterior|hoy/i.test(t), firstMatch(t, /[^.]*(futur|posterior)[^.]*\./i));
  });

  await step('TC-13', 'Búsqueda sin tildes ni mayúsculas', async () => {
    await r.goto(BASE + '/dashboard/fichas'); await r.waitForTimeout(1200);
    const q = r.locator('input[placeholder^="Buscar por nombre"]');
    await q.fill(`jose${RND} nunez`); await r.waitForTimeout(1800);
    await shot(r, 'TC-13-busqueda');
    log('TC-13', 'La búsqueda ignora tildes y mayúsculas ("jose nunez")', (await body(r)).includes(NAME), '');
    await q.fill(DNI); await r.waitForTimeout(1500);
    log('TC-13', 'La búsqueda por DNI encuentra la ficha', (await body(r)).includes(NAME), '');
    await q.fill('zzzzsinresultado'); await r.waitForTimeout(1500); await shot(r, 'TC-13-sin-resultados');
    const t = await body(r);
    log('TC-13', 'Una búsqueda sin coincidencias no lista fichas y lo informa', !t.includes(NAME) && /sin resultados|no se encontr|no hay|ninguna/i.test(t), t.slice(0, 0));
  });

  await step('TC-15', 'Filtros combinables del listado', async () => {
    await r.goto(BASE + '/dashboard/fichas'); await r.waitForTimeout(1200);
    await r.getByRole('button', { name: /Filtros|Filtrar/ }).first().click().catch(() => {}); await r.waitForTimeout(500);
    await shot(r, 'TC-15-filtros');
    const hasDni = r.locator('select[formControlName="hasDni"]');
    const ok1 = await hasDni.count();
    if (ok1) await hasDni.selectOption({ index: 1 });
    await r.getByRole('button', { name: /Aplicar/ }).first().click().catch(() => {}); await r.waitForTimeout(1500);
    await shot(r, 'TC-15-aplicados');
    log('TC-15', 'El listado se filtra por DNI sin errores de API', ok1 > 0 && !bad.length, bad.join(' | '));
  });

  await step('TC-34', 'Editar ficha carga los datos', async () => {
    await r.goto(fichaUrl + '/edit'); await r.waitForTimeout(1800);
    await shot(r, 'TC-34-edicion');
    const v = await r.inputValue('input[formControlName="firstName"]').catch(() => '');
    log('TC-34', 'La edición de una ficha carga los datos guardados', v === NAME, `firstName="${v}"`);
    await r.fill('input[formControlName="firstName"]', NAME + 'X');
    await r.getByRole('button', { name: 'Guardar cambios' }).click(); await r.waitForTimeout(2000);
    log('TC-34', 'Los cambios editados se guardan', (await body(r)).includes(NAME + 'X') && !bad.length, bad.join(' | '));
  });

  const openTab = async (name) => { await r.goto(fichaUrl); await r.waitForTimeout(1200); await r.getByRole('button', { name }).first().click(); await r.waitForTimeout(900); };
  const addObservation = async (content) => {
    await r.getByRole('button', { name: /Nueva observación/ }).click(); await r.waitForTimeout(500);
    await r.locator('app-create-observation-modal textarea').fill(content);
    await r.locator('app-create-observation-modal button[type="submit"]').click(); await r.waitForTimeout(1500);
  };

  await step('TC-16', 'Registrar una observación categorizada', async () => {
    await openTab(/Observaciones/);
    await r.getByRole('button', { name: /Nueva observación/ }).click(); await r.waitForTimeout(500);
    await shot(r, 'TC-16-modal');
    const sel = r.locator('app-create-observation-modal select[formControlName="categoryId"]');
    const opts = await sel.locator('option').count();
    if (opts > 1) await sel.selectOption({ index: 1 });
    await r.locator('app-create-observation-modal textarea').fill(`Observación UI ${RND}`);
    await r.locator('app-create-observation-modal button[type="submit"]').click(); await r.waitForTimeout(1800);
    await shot(r, 'TC-16-registrada');
    log('TC-16', 'Se registra una observación con categoría y aparece en la línea de tiempo', (await body(r)).includes(`Observación UI ${RND}`) && !bad.length, `categorías=${opts - 1} ${bad.join(' | ')}`);
  });

  await step('TC-37', 'No se guarda una observación vacía', async () => {
    await openTab(/Observaciones/);
    await r.getByRole('button', { name: /Nueva observación/ }).click(); await r.waitForTimeout(500);
    const submit = r.locator('app-create-observation-modal button[type="submit"]');
    const disabled = await submit.isDisabled();
    if (!disabled) { await submit.click(); await r.waitForTimeout(800); }
    await shot(r, 'TC-37-vacia');
    const stillOpen = await r.locator('app-create-observation-modal').count();
    log('TC-37', 'Una observación sin texto no se guarda', (disabled || stillOpen > 0) && !bad.length, `botón deshabilitado=${disabled} modal abierto=${stillOpen > 0} ${bad.join(' | ')}`);
    await r.locator('app-create-observation-modal textarea').fill('   '); await r.waitForTimeout(300);
    const dis2 = await submit.isDisabled();
    if (!dis2) { await submit.click(); await r.waitForTimeout(800); }
    log('TC-38', 'Un texto solo con espacios en blanco tampoco se guarda', (dis2 || (await r.locator('app-create-observation-modal').count()) > 0) && !bad.length, `botón deshabilitado=${dis2} ${bad.join(' | ')}`);
    await r.getByRole('button', { name: 'Cancelar' }).last().click().catch(() => {}); await r.waitForTimeout(400);
  });

  await step('TC-17', 'Filtros de la línea de tiempo', async () => {
    await openTab(/Observaciones/);
    await addObservation(`Segunda observación ${RND}`);
    const cat = r.locator('app-observation-filters select[formControlName="categoryId"]');
    const n = await cat.locator('option').count();
    if (n > 1) await cat.selectOption({ index: 1 });
    await r.waitForTimeout(1200); await shot(r, 'TC-17-filtro-categoria');
    const t = await body(r);
    log('TC-17', 'Filtrar por categoría deja solo las observaciones de esa categoría', t.includes(`Observación UI ${RND}`) && !t.includes(`Segunda observación ${RND}`), `opciones=${n}`);
    await cat.selectOption('all'); await r.waitForTimeout(800);
    const author = r.locator('app-observation-filters select[formControlName="author"]');
    if (await author.locator('option').count() > 1) await author.selectOption({ index: 1 });
    await r.waitForTimeout(800);
    log('TC-17', 'Filtrar por autor muestra las observaciones del autor sin errores de API', !bad.length, bad.join(' | '));
  });

  await step('TC-18', 'Exportar observaciones', async () => {
    await openTab(/Observaciones/);
    const [dl] = await Promise.all([r.waitForEvent('download', { timeout: 8000 }).catch(() => null), r.getByRole('button', { name: /Exportar informe/ }).click()]);
    const name = dl?.suggestedFilename() ?? '';
    log('TC-18', 'Exportar informe descarga un archivo con las observaciones', !!dl && /\.(csv|xlsx|pdf)$/i.test(name), `archivo=${name}`);
  });

  await step('TC-20', 'Historia de vida por etapa', async () => {
    await openTab(/Historia de vida/);
    await shot(r, 'TC-20-historia');
    const etapas = await r.getByRole('button', { name: /Agregar entrada/ }).count();
    await r.getByRole('button', { name: /Agregar entrada/ }).first().click(); await r.waitForTimeout(600);
    await r.locator('app-life-history-editor-modal textarea').fill(`Entrada historia ${RND}`);
    await r.locator('app-life-history-editor-modal').getByRole('button', { name: /Guardar/ }).click(); await r.waitForTimeout(1800);
    await shot(r, 'TC-20-guardada');
    log('TC-20', 'La historia de vida tiene 3 etapas y se agrega una entrada', etapas === 3 && (await body(r)).includes(`Entrada historia ${RND}`) && !bad.length, `etapas=${etapas} ${bad.join(' | ')}`);
  });

  await step('SEC-08b', 'XSS almacenado en una observación', async () => {
    await openTab(/Observaciones/);
    const payload = `<img src=x onerror="window.__xss=1"> <script>window.__xss=2</script> ${RND}`;
    await addObservation(payload); await r.waitForTimeout(800);
    await shot(r, 'SEC-08b-xss');
    const executed = await r.evaluate(() => window.__xss);
    log('SEC-08b', 'Una observación con HTML/script se muestra como texto y no ejecuta código', executed === undefined && (await body(r)).includes('<img src=x'), `__xss=${executed}`);
  });

  const e = await mk(); await login(e, 'escucha@test.com');
  await step('TC-12', 'Escucha no crea ni edita fichas', async () => {
    await e.goto(BASE + '/dashboard/fichas'); await e.waitForTimeout(1200);
    const newBtn = await e.getByRole('link', { name: /Nueva ficha/ }).count() + await e.getByRole('button', { name: /Nueva ficha/ }).count();
    await e.goto(fichaUrl); await e.waitForTimeout(1500); await shot(e, 'TC-12-detalle-escucha');
    const editBtn = await e.getByText('Editar ficha').count();
    await e.goto(fichaUrl + '/edit'); await e.waitForTimeout(1000);
    log('TC-12', 'Escucha no ve "Nueva ficha" ni "Editar ficha" y /edit lo deniega', newBtn === 0 && editBtn === 0 && e.url().includes('access-denied'), `nueva=${newBtn} editar=${editBtn} url=${e.url().replace(BASE, '')}`);
  });

  const m = await mk({ width: 390, height: 780 }); await login(m, 'referente@test.com');
  await step('TC-39', 'Pantalla de celular', async () => {
    await m.waitForTimeout(800); await shot(m, 'TC-39-celular-inicio');
    const overflow = await m.evaluate(() => document.documentElement.scrollWidth > window.innerWidth + 2);
    await m.goto(BASE + '/dashboard/fichas'); await m.waitForTimeout(1500); await shot(m, 'TC-39-celular-fichas');
    const overflow2 = await m.evaluate(() => document.documentElement.scrollWidth > window.innerWidth + 2);
    log('TC-39', 'En 390 px no hay desborde horizontal en inicio ni en fichas', !overflow && !overflow2, `inicio=${overflow} fichas=${overflow2}`);
  });
  await step('TC-40', 'Menú lateral se cierra al navegar', async () => {
    await m.goto(BASE + '/dashboard'); await m.waitForTimeout(1000);
    const burger = m.locator('button[aria-label*="enú" i], button[aria-label*="menu" i]').first();
    const hasBurger = await burger.count();
    if (hasBurger) await burger.click();
    await m.waitForTimeout(500); await shot(m, 'TC-40-menu-abierto');
    await m.getByRole('link', { name: 'Fichas' }).first().click(); await m.waitForTimeout(1200); await shot(m, 'TC-40-menu-cerrado');
    const box = await m.locator('aside').first().boundingBox().catch(() => null);
    log('TC-40', 'Al elegir una opción el menú lateral se oculta en pantalla chica', hasBurger > 0 && (!box || box.x + box.width <= 5), `burger=${hasBurger} box=${JSON.stringify(box)}`);
  });
}
await browser.close();
const failed = results.filter(x => !x.ok);
console.log(`\nTOTAL ${results.length}  PASS ${results.length - failed.length}  FAIL ${failed.length}`);
fs.writeFileSync(`${SHOTS}/results-${ONLY}.json`, JSON.stringify(results, null, 1));
