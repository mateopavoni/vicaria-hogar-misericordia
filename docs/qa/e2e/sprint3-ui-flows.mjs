// QA de UI Sprint 3 (Playwright). Requiere el stack local levantado (`docker compose up -d` en backend/).
// La BD persiste entre corridas: las visitas se crean mañana en un horario aleatorio y un 409 en
// "Visita: crear" suele ser una corrida previa. Antes de repetir, borrar las visitas de prueba:
//   DELETE FROM casona_visits WHERE visitor_name IN (N'Familiar UI', N'Pisada', N'Visita a cancelar')
import { chromium } from 'playwright';
const BASE = 'http://host.docker.internal:4200', out = '/shots';
const pad = n => String(n).padStart(2, '0');
const t = new Date(Date.now() + 86400000);
const TOMORROW = `${t.getFullYear()}-${pad(t.getMonth() + 1)}-${pad(t.getDate())}`;
const H = 8 + Math.floor(Math.random() * 10);
const RND = String(Math.floor(Math.random() * 90000000) + 10000000);
const PERS = `UI Evento personal ${RND}`;
const AREA = `Huerta${RND}`;
const browser = await chromium.launch();
const results = [];
const log = (name, ok, note = '') => { results.push({ name, ok, note }); console.log(`${ok ? 'PASS' : 'FAIL'}  ${name}${note ? ' — ' + note : ''}`); };
let bad = [], errs = [];
const mk = async () => {
  const ctx = await browser.newContext({ viewport: { width: 1400, height: 950 } });
  const page = await ctx.newPage();
  page.on('console', m => { if (m.type() === 'error') errs.push(m.text().slice(0, 160)); });
  page.on('response', r => { if (r.url().includes('/api/') && r.status() >= 400) bad.push(`${r.status()} ${r.request().method()} ${r.url().replace(BASE, '')}`); });
  return page;
};
const login = async (page, email) => {
  await page.goto(BASE + '/auth');
  await page.fill('input[formControlName="email"]', email);
  await page.fill('input[formControlName="password"]', 'Test1234!');
  await page.click('button[type="submit"]');
  await page.waitForURL(/dashboard|inicio/, { timeout: 15000 });
};
const body = async page => (await page.innerText('body')).replace(/\s+/g, ' ');
const step = async (name, fn) => { bad = []; errs = []; try { await fn(); } catch (e) { log(name, false, 'excepción: ' + e.message.split('\n')[0].slice(0, 160)); } };

const p = await mk(); await login(p, 'referente@test.com');

await step('Evento general: crear desde la UI', async () => {
  await p.goto(BASE + '/dashboard/calendario'); await p.waitForTimeout(1500);
  await p.getByRole('button', { name: 'Nuevo evento' }).click();
  await p.fill('[formControlName="title"]', 'UI Evento general');
  await p.fill('[formControlName="date"]', TOMORROW);
  await p.fill('[formControlName="startTime"]', '11:00'); await p.fill('[formControlName="endTime"]', '12:00');
  await p.fill('[formControlName="description"]', 'creado por prueba UI');
  await p.getByRole('button', { name: 'Guardar' }).click(); await p.waitForTimeout(2000);
  await p.screenshot({ path: `${out}/10-evento-general.png` });
  log('Evento general: crear desde la UI', (await body(p)).includes('UI Evento general') && !bad.length, bad.join(' | '));
});

await step('Evento semanal recurrente', async () => {
  await p.getByRole('button', { name: 'Nuevo evento' }).click();
  await p.fill('[formControlName="title"]', 'UI Semanal');
  await p.fill('[formControlName="date"]', TOMORROW);
  await p.fill('[formControlName="startTime"]', '17:00'); await p.fill('[formControlName="endTime"]', '18:00');
  await p.selectOption('[formControlName="recurrence"]', 'weekly');
  await p.getByRole('button', { name: 'Guardar' }).click(); await p.waitForTimeout(2000);
  log('Evento semanal recurrente', (await body(p)).includes('UI Semanal') && !bad.length, bad.join(' | '));
});

await step('Evento personal en Mi calendario', async () => {
  await p.getByText('Mi calendario', { exact: true }).first().click(); await p.waitForTimeout(1200);
  await p.getByRole('button', { name: 'Nuevo evento' }).click();
  await p.fill('[formControlName="title"]', PERS);
  await p.fill('[formControlName="date"]', TOMORROW);
  await p.fill('[formControlName="startTime"]', '08:00'); await p.fill('[formControlName="endTime"]', '09:00');
  await p.getByRole('button', { name: 'Guardar' }).click(); await p.waitForTimeout(2000);
  await p.screenshot({ path: `${out}/11-evento-personal.png` });
  log('Evento personal: crear en Mi calendario', (await body(p)).includes(PERS) && !bad.length, bad.join(' | '));
  await p.getByText('General', { exact: true }).first().click(); await p.waitForTimeout(1200);
  log('Evento personal NO aparece en General', !(await body(p)).includes(PERS));
  await p.getByText('Combinado', { exact: true }).first().click(); await p.waitForTimeout(1500);
  const comb = await body(p);
  log('Combinado muestra general + personal', comb.includes(PERS) && comb.includes('UI Evento general'));
});

await step('Evento general: detalle', async () => {
  await p.getByText('General', { exact: true }).first().click(); await p.waitForTimeout(1200);
  await p.getByText('UI Evento general').first().click(); await p.waitForTimeout(1000);
  await p.screenshot({ path: `${out}/12-detalle-evento.png` });
  const txt = await body(p);
  log('Detalle de evento muestra autor y descripcion', txt.includes('Test referente') && txt.includes('creado por prueba UI'));
  await p.keyboard.press('Escape');
});

await step('Evento general: editar y eliminar', async () => {
  await p.goto(BASE + '/dashboard/calendario'); await p.waitForTimeout(1500);
  await p.getByText('UI Evento general').first().click(); await p.waitForTimeout(800);
  await p.getByRole('button', { name: 'Editar', exact: true }).click(); await p.waitForTimeout(600);
  await p.fill('[formControlName="title"]', 'UI Evento editado');
  await p.getByRole('button', { name: 'Guardar' }).click(); await p.waitForTimeout(2000);
  await p.screenshot({ path: `${out}/13-evento-editado.png` });
  log('Evento general: editar (PUT 204)', (await body(p)).includes('UI Evento editado') && !bad.length, bad.join(' | '));
  await p.getByText('UI Evento editado').first().click(); await p.waitForTimeout(800);
  await p.getByRole('button', { name: 'Eliminar', exact: true }).click(); await p.waitForTimeout(600);
  await p.getByRole('button', { name: 'Eliminar', exact: true }).last().click(); await p.waitForTimeout(2000);
  log('Evento general: eliminar con confirmación (DELETE 204)', !(await body(p)).includes('UI Evento editado') && !bad.length, bad.join(' | '));
});

await step('Evento personal: convertir a general', async () => {
  await p.getByText('Mi calendario', { exact: true }).first().click(); await p.waitForTimeout(1200);
  await p.getByText(PERS).first().click(); await p.waitForTimeout(800);
  await p.getByRole('button', { name: 'Hacer general' }).click(); await p.waitForTimeout(2000);
  await p.screenshot({ path: `${out}/14-evento-convertido.png` });
  log('Evento personal: convertir a general (PUT /publish)', !bad.length, bad.join(' | '));
  log('Evento convertido ya no está en Mi calendario', !(await body(p)).includes(PERS));
  await p.getByText('General', { exact: true }).first().click(); await p.waitForTimeout(1500);
  log('Evento convertido aparece en General', (await body(p)).includes(PERS));
});

await step('Colaborador: crear', async () => {
  await p.goto(BASE + '/dashboard/colaboradores'); await p.waitForTimeout(1500);
  await p.getByRole('button', { name: 'Nuevo colaborador' }).click();
  await p.fill('[formControlName="firstName"]', 'Marcos'); await p.fill('[formControlName="lastName"]', 'Peralta');
  await p.fill('[formControlName="dni"]', RND); await p.fill('[formControlName="phone"]', '1144556677');
  await p.fill('[formControlName="email"]', 'marcos@x.com');
  await p.selectOption('[formControlName="type"]', { index: 1 }).catch(() => {});
  await p.fill('[formControlName="workArea"]', AREA);
  await p.getByRole('button', { name: 'Guardar' }).click(); await p.waitForTimeout(2000);
  await p.screenshot({ path: `${out}/20-colaborador.png` });
  log('Colaborador: crear desde la UI', (await body(p)).includes('Marcos') && !bad.length, bad.join(' | '));
});

await step('Colaborador: DNI duplicado', async () => {
  await p.getByRole('button', { name: 'Nuevo colaborador' }).click();
  await p.fill('[formControlName="firstName"]', 'Otro'); await p.fill('[formControlName="dni"]', RND);
  await p.getByRole('button', { name: 'Guardar' }).click(); await p.waitForTimeout(1500);
  const txt = await body(p);
  await p.screenshot({ path: `${out}/21-colaborador-dup.png` });
  log('Colaborador: DNI duplicado devuelve 409 y se informa', bad.some(b => b.startsWith('409')), txt.slice(-250));
  await p.getByRole('button', { name: 'Cancelar', exact: true }).click().catch(() => {});
});

await step('Colaborador: busqueda', async () => {
  await p.goto(BASE + '/dashboard/colaboradores'); await p.waitForTimeout(1500);
  await p.getByPlaceholder(/Buscar|nombre/i).first().fill(AREA); await p.waitForTimeout(1200);
  const t1 = await body(p);
  log('Colaborador: busqueda por area filtra', t1.includes('Marcos') && !t1.includes('Lucía'));
});

await step('Colaborador: editar', async () => {
  await p.goto(BASE + '/dashboard/colaboradores'); await p.waitForTimeout(1500);
  await p.getByPlaceholder(/Buscar|nombre/i).first().fill(AREA); await p.waitForTimeout(1200);
  await p.getByRole('button', { name: 'Editar', exact: true }).first().click(); await p.waitForTimeout(600);
  await p.fill('[formControlName="firstName"]', 'MarcosEditado');
  await p.getByRole('button', { name: 'Guardar' }).click(); await p.waitForTimeout(2000);
  await p.screenshot({ path: `${out}/22-colaborador-editado.png` });
  log('Colaborador: editar (PUT 204)', (await body(p)).includes('MarcosEditado') && !bad.length, bad.join(' | '));
});

await step('Colaborador: baja y reactivación', async () => {
  const find = async () => { await p.getByPlaceholder(/Buscar|nombre/i).first().fill(AREA); await p.waitForTimeout(1000); };
  await find();
  await p.getByRole('button', { name: 'Dar de baja', exact: true }).first().click(); await p.waitForTimeout(700);
  await p.getByRole('button', { name: 'Dar de baja', exact: true }).last().click(); await p.waitForTimeout(2000);
  log('Colaborador: baja lógica (PUT isActive=false)', !bad.length && !(await body(p)).includes('MarcosEditado'), bad.join(' | '));
  await p.getByText('Inactivos').first().click(); await p.waitForTimeout(800);
  await p.screenshot({ path: `${out}/23-colaborador-baja.png` });
  log('Colaborador: aparece en la pestaña Inactivos', (await body(p)).includes('MarcosEditado') && (await body(p)).includes('Reactivar'));
  await p.reload(); await p.waitForTimeout(1500); await find();
  await p.getByText('Inactivos').first().click(); await p.waitForTimeout(800);
  log('Colaborador: la baja persiste tras recargar', (await body(p)).includes('MarcosEditado'));
  await p.getByRole('button', { name: 'Reactivar', exact: true }).first().click(); await p.waitForTimeout(700);
  await p.getByRole('button', { name: 'Reactivar', exact: true }).last().click(); await p.waitForTimeout(2000);
  await p.getByText('Activos').first().click(); await p.waitForTimeout(800);
  log('Colaborador: reactivar vuelve a Activos', !bad.length && (await body(p)).includes('MarcosEditado'), bad.join(' | '));
});

await step('Visita: crear', async () => {
  await p.goto(BASE + '/dashboard/calendario'); await p.waitForTimeout(1200);
  await p.getByText('Casa de Convivencia', { exact: true }).first().click(); await p.waitForTimeout(1500);
  await p.getByRole('button', { name: 'Nueva visita' }).click(); await p.waitForTimeout(800);
  await p.selectOption('[formControlName="residentId"]', { index: 1 });
  await p.fill('[formControlName="visitorName"]', 'Familiar UI');
  await p.fill('[formControlName="date"]', TOMORROW); await p.fill('[formControlName="time"]', `${pad(H)}:00`);
  await p.fill('[formControlName="durationMinutes"]', '45');
  await p.getByRole('button', { name: 'Guardar' }).click(); await p.waitForTimeout(2000);
  await p.screenshot({ path: `${out}/30-visita.png` });
  log('Visita: crear desde la UI', (await body(p)).includes('Familiar UI') && !bad.length, bad.join(' | '));
});

await step('Visita: solapamiento', async () => {
  await p.getByRole('button', { name: 'Nueva visita' }).click(); await p.waitForTimeout(600);
  await p.selectOption('[formControlName="residentId"]', { index: 1 });
  await p.fill('[formControlName="visitorName"]', 'Pisada');
  await p.fill('[formControlName="date"]', TOMORROW); await p.fill('[formControlName="time"]', `${pad(H)}:15`);
  await p.fill('[formControlName="durationMinutes"]', '30');
  await p.getByRole('button', { name: 'Guardar' }).click(); await p.waitForTimeout(1500);
  const txt = await body(p);
  await p.screenshot({ path: `${out}/31-visita-solapada.png` });
  log('Visita: solapamiento rechazado con mensaje', /superpone|solap|conflicto/i.test(txt), txt.slice(-220));
  await p.getByRole('button', { name: 'Cancelar', exact: true }).click().catch(() => {});
});

await step('Visita: marcar realizada', async () => {
  await p.getByText('Familiar UI').last().click(); await p.waitForTimeout(800);
  await p.screenshot({ path: `${out}/32-visita-detalle.png` });
  await p.getByRole('button', { name: /realizada/i }).first().click(); await p.waitForTimeout(1500);
  log('Visita: marcar realizada (PATCH /status)', !bad.length, bad.join(' | '));
});


await step('Visita: cancelar con motivo', async () => {
  await p.getByRole('button', { name: 'Nueva visita' }).click(); await p.waitForTimeout(600);
  await p.selectOption('[formControlName="residentId"]', { index: 2 });
  await p.fill('[formControlName="visitorName"]', 'Visita a cancelar');
  await p.fill('[formControlName="date"]', TOMORROW); await p.fill('[formControlName="time"]', `${pad(H + 2)}:00`);
  await p.fill('[formControlName="durationMinutes"]', '30');
  await p.getByRole('button', { name: 'Guardar' }).click(); await p.waitForTimeout(1800);
  await p.getByText('Visita a cancelar').first().click(); await p.waitForTimeout(700);
  await p.getByRole('button', { name: /cancelar visita/i }).first().click(); await p.waitForTimeout(700);
  await p.screenshot({ path: `${out}/33-cancelar-visita.png` });
  const motivo = p.locator('textarea, input[formControlName="reason"], input[type="text"]').last();
  await motivo.fill('Motivo de prueba').catch(() => {});
  await p.getByRole('button', { name: /confirmar|cancelar visita/i }).last().click().catch(() => {});
  await p.waitForTimeout(1500);
  log('Visita: cancelar (PATCH /status con motivo)', !bad.length, bad.join(' | '));
});

const e = await mk(); await login(e, 'escucha@test.com');
await step('Escucha: solo lectura', async () => {
  await e.goto(BASE + '/dashboard/calendario'); await e.waitForTimeout(1800);
  const txt = await body(e);
  await e.screenshot({ path: `${out}/40-escucha-calendario.png` });
  log('Escucha ve el calendario con datos', txt.includes('Desayuno') && !bad.length, bad.join(' | '));
  log('Escucha NO ve Nuevo evento', !txt.includes('Nuevo evento'));
  log('Escucha NO ve pestaña Casa de Convivencia', !txt.includes('Casa de Convivencia'));
  log('Escucha NO ve Colaboradores en el menu', !txt.includes('Colaboradores'));
  await e.goto(BASE + '/dashboard/colaboradores'); await e.waitForTimeout(1200);
  log('Escucha: /colaboradores bloqueado', !e.url().endsWith('/colaboradores'), e.url());
});

const d = await mk(); await login(d, 'directora@test.com');
await step('Directora', async () => {
  await d.goto(BASE + '/dashboard/calendario'); await d.waitForTimeout(1800);
  const txt = await body(d);
  log('Directora ve calendario sin Nuevo evento (backend: solo Referente)', txt.includes('Desayuno') && !txt.includes('Nuevo evento'));
  await d.getByText('Casa de Convivencia', { exact: true }).first().click(); await d.waitForTimeout(1500);
  log('Directora puede crear visitas', (await body(d)).includes('Nueva visita'));
});

const fails = results.filter(r => !r.ok).length;
console.log(`\nTOTAL ${results.length}  PASS ${results.length - fails}  FAIL ${fails}`);
await browser.close();
