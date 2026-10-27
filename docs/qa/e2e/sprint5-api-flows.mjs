// Sprint 5: pruebas contra la API real de adjuntos (SCRUM-24), evaluación psiquiátrica (SCRUM-66), estado
// activo/inactivo (SCRUM-67), respaldos (SCRUM-77), informe y catálogo de Cáritas (SCRUM-69/70) y registro
// de exportación de la ficha (SCRUM-23).
//   API=http://localhost:5000 node sprint5-api-flows.mjs   (stack de backend/docker-compose.yml con datos semilla)
// Crea personas nuevas en cada corrida para no depender del estado de corridas anteriores. La parte del job
// diario de inactivación reinicia el contenedor vicaria-api (el job corre al arrancar).
import fs from 'fs';
import { execFileSync } from 'child_process';

const API = process.env.API || 'http://localhost:5000';
const PASS = 'Test1234!';
const res = [];
const rec = (id, name, ok, note = '') => { res.push({ id, name, ok, note }); console.log(`${ok ? 'PASS' : 'FAIL'}  ${id}  ${name}${note ? ' — ' + note : ''}`); };
const call = async (method, path, { token, body, form } = {}) => {
  const h = {}; if (token) h.Authorization = `Bearer ${token}`;
  if (body !== undefined) h['Content-Type'] = 'application/json';
  const r = await fetch(API + path, { method, headers: h, body: form ?? (body === undefined ? undefined : JSON.stringify(body)) });
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

// fechas en hora de Argentina (UTC-3), igual que el backend
const ar = (offsetDays = 0) => new Date(Date.now() - 3 * 36e5 + offsetDays * 864e5);
const iso = d => d.toISOString().slice(0, 10);
const today = iso(ar());
const monthsAgo = (months, plusDays = 0) => { const d = ar(); d.setUTCMonth(d.getUTCMonth() - months); d.setUTCDate(d.getUTCDate() + plusDays); return iso(d); };
const tag = Date.now().toString(36);
const newPerson = async (name) => {
  const r = await call('POST', '/api/social-records', { token: T.ref, body: { firstName: name, lastName: `QA ${tag}` } });
  return { recordId: r.json?.id, personId: r.json?.personId };
};
const detail = async (recordId, tok = T.ref) => (await call('GET', `/api/social-records/${recordId}`, { token: tok })).json;

// ---------------- SCRUM-24 adjuntos
const A = await newPerson('Adjuntos');
const pngBytes = Buffer.from('89504e470d0a1a0a0000000d49484452000000010000000108060000001f15c4890000000d4944415478da63f8ffff3f0005fe02fea7d6a4ea0000000049454e44ae426082', 'hex');
const formOf = files => { const f = new FormData(); for (const [name, type, data] of files) f.append('files', new Blob([data], { type }), name); return f; };
let r = await call('POST', `/api/persons/${A.personId}/attachments`, { token: T.ref, form: formOf([
  ['foto.jpg', 'image/jpeg', Buffer.from('ffd8ffe000104a464946', 'hex')],
  ['recibo.png', 'image/png', pngBytes],
  ['constancia.pdf', 'application/pdf', Buffer.from('%PDF-1.4\n%%EOF\n')],
]) });
const uploaded = r.json ?? [];
rec('S5-A01', 'Referente adjunta JPG, PNG y PDF en una sola carga (201 con los 3 archivos)', r.status === 201 && uploaded.length === 3, `status=${r.status} ${uploaded.map(a => a.fileName).join(', ')}`);
rec('S5-A02', 'Cada adjunto trae nombre, fecha de carga, usuario e ícono por tipo',
  uploaded.length === 3 && uploaded.every(a => a.fileName && a.uploadedAt && a.uploadedByName && a.fileTypeIcon), uploaded.map(a => `${a.fileName}:${a.fileTypeIcon}:${a.uploadedByName}`).join(' | '));
r = await call('POST', `/api/persons/${A.personId}/attachments`, { token: T.ref, form: formOf([['notas.txt', 'text/plain', Buffer.from('hola')]]) });
rec('S5-A03', 'Un archivo de texto se rechaza (400)', r.status === 400, `status=${r.status} ${r.json?.message ?? ''}`);
r = await call('POST', `/api/persons/${A.personId}/attachments`, { token: T.ref, form: formOf([['grande.pdf', 'application/pdf', Buffer.alloc(11 * 1024 * 1024, 0x20)]]) });
rec('S5-A04', 'Un archivo de 11 MB se rechaza con un mensaje que nombra el máximo de 10 MB (400)', r.status === 400 && /10 ?MB/i.test(r.text), `status=${r.status} ${r.json?.message ?? ''}`);
r = await call('POST', `/api/persons/${A.personId}/attachments`, { token: T.ref, form: formOf([['nueve.pdf', 'application/pdf', Buffer.concat([Buffer.from('%PDF-1.4\n'), Buffer.alloc(9 * 1024 * 1024, 0x20)])]]) });
rec('S5-A05', 'Un archivo de 9 MB se acepta (201)', r.status === 201, `status=${r.status}`);
r = await call('GET', `/api/persons/${A.personId}/attachments`, { token: T.esc });
rec('S5-A06', 'El listado devuelve los 4 adjuntos de la ficha (Escucha puede verlos)', r.status === 200 && r.json?.length === 4, `status=${r.status} n=${r.json?.length}`);
const png = uploaded.find(a => a.fileName === 'recibo.png'), pdf = uploaded.find(a => a.fileName === 'constancia.pdf');
r = await call('GET', `/api/persons/${A.personId}/attachments/${png?.id}`, { token: T.ref });
rec('S5-A07', 'La imagen se puede ver: se sirve con su tipo image/png', r.status === 200 && /image\/png/.test(r.headers.get('content-type') ?? '') && r.buf.equals(pngBytes), `status=${r.status} type=${r.headers.get('content-type')}`);
r = await call('GET', `/api/persons/${A.personId}/attachments/${pdf?.id}?download=true`, { token: T.ref });
rec('S5-A08', 'El PDF se descarga con su nombre original', r.status === 200 && /constancia\.pdf/.test(r.headers.get('content-disposition') ?? ''), `status=${r.status} ${r.headers.get('content-disposition')}`);
for (const [tok, role] of [[T.esc, 'Escucha'], [T.dir, 'Directora'], [T.coo, 'Coordinador']]) {
  r = await call('DELETE', `/api/persons/${A.personId}/attachments/${pdf?.id}`, { token: tok });
  rec(`S5-A09-${role.slice(0, 3)}`, `${role} no puede eliminar adjuntos (403)`, r.status === 403, `status=${r.status}`);
}
r = await call('DELETE', `/api/persons/${A.personId}/attachments/${pdf?.id}`, { token: T.ref });
const after = await call('GET', `/api/persons/${A.personId}/attachments`, { token: T.ref });
rec('S5-A10', 'Referente elimina un adjunto (204) y deja de figurar', r.status === 204 && !after.json?.some(a => a.id === pdf?.id), `status=${r.status} quedan=${after.json?.length}`);
r = await call('GET', `/api/persons/${A.personId}/attachments`);
rec('S5-A11', 'Sin sesión responde 401', r.status === 401, `status=${r.status}`);

// ---------------- SCRUM-66 evaluación psiquiátrica
const E = await newPerson('Evaluacion');
const evalPost = (p, o, tok = T.ref) => call('POST', `/api/persons/${p}/psychiatric-evaluations`, { token: tok, body: { date: today, professional: 'Dra. Gómez', diagnosis: 'Diagnóstico de prueba', ...o } });
const evalStatus = p => call('GET', `/api/persons/${p}/psychiatric-evaluations/status`, { token: T.ref });
const firstDate = iso(ar(-10));
r = await evalPost(E.personId, { date: firstDate });
const expectedExpiry = (() => { const d = new Date(`${firstDate}T00:00:00Z`); d.setUTCMonth(d.getUTCMonth() + 6); return iso(d); })();
rec('S5-E01', 'Referente registra una evaluación: queda vigente y vence a los 6 meses de su fecha (201)', r.status === 201 && r.json?.isValid === true && r.json?.expiresAt?.slice(0, 10) === expectedExpiry, `status=${r.status} vence=${r.json?.expiresAt?.slice(0, 10)} esperado=${expectedExpiry}`);
const hasFlag = () => sql(`SELECT HasPsychiatricEvaluation FROM social_records WHERE Id='${E.recordId}'`);
rec('S5-E02', 'Al guardar la evaluación la ficha queda con tiene_eval_psiq = true', hasFlag() === '1', `HasPsychiatricEvaluation=${hasFlag()}`);
r = await evalPost(E.personId, { date: today, professional: 'Dr. Paz' });
const st = await evalStatus(E.personId);
const hist = await call('GET', `/api/persons/${E.personId}/psychiatric-evaluations/history`, { token: T.ref });
const validCount = sql(`SELECT COUNT(*) FROM psychiatric_evaluations WHERE PersonId='${E.personId}' AND IsValid=1`);
rec('S5-E03', 'Una nueva evaluación deja no vigente a la anterior: una sola vigente por persona', r.status === 201 && validCount === '1' && st.json?.professional === 'Dr. Paz', `status=${r.status} vigentes=${validCount} actual=${st.json?.professional}`);
rec('S5-E04', 'El historial muestra la evaluación anterior como no vigente, con quién la cargó', hist.status === 200 && hist.json?.length === 1 && hist.json[0].isValid === false && !!hist.json[0].registeredByName, `status=${hist.status} ${JSON.stringify(hist.json?.map(h => [h.professional, h.isValid, h.registeredByName]))}`);
r = await evalPost(E.personId, { date: iso(ar(2)) });
rec('S5-E05', 'Una fecha futura se rechaza (400)', r.status === 400 && /futura/i.test(r.text), `status=${r.status}`);
r = await evalPost(E.personId, {}, T.esc);
rec('S5-E06', 'Escucha no puede registrar evaluaciones (403)', r.status === 403, `status=${r.status}`);

const V = await newPerson('Vencida');
r = await evalPost(V.personId, { date: monthsAgo(6, -1) });
const vst = await evalStatus(V.personId);
rec('S5-E08', 'Una evaluación de hace 6 meses y 1 día figura vencida', vst.json?.estaVencida === true, `estaVencida=${vst.json?.estaVencida} dias=${vst.json?.diasParaVencer}`);
r = await call('PUT', `/api/persons/${V.personId}/type`, { token: T.ref, body: { personType: 1 } });
rec('S5-E09', 'Con la evaluación vencida se bloquea el ingreso a la Casa de Convivencia (400, falta evaluación vigente)', r.status === 400 && /evaluaci/i.test(r.text), `status=${r.status} ${r.json?.message ?? ''}`);
const M = await newPerson('Medicacion');
await evalPost(M.personId, { date: today });
r = await call('PUT', `/api/persons/${M.personId}/type`, { token: T.ref, body: { personType: 1 } });
rec('S5-E07', 'Con evaluación vigente la persona ingresa a la Casa de Convivencia como Residente (204)', r.status === 204, `status=${r.status} ${r.json?.message ?? ''}`);
const catalog = (await call('GET', '/api/medication-schemes/catalog', { token: T.ref })).json ?? [];
const medId = catalog[0]?.id;
r = await evalPost(M.personId, { date: today }, T.coo);
rec('S5-E14', 'Coordinador registra la evaluación de un residente (201)', r.status === 201, `status=${r.status}`);
await evalPost(M.personId, { date: monthsAgo(7) });
r = await call('POST', `/api/persons/${M.personId}/medication-schemes`, { token: T.ref, body: { medicationId: medId, dose: '1 comprimido', time: '09:15' } });
rec('S5-E10', 'Con la evaluación vencida se bloquea cargar un medicamento nuevo (409)', r.status === 409 && /evaluaci/i.test(r.text), `status=${r.status} ${r.json?.message ?? ''}`);
await evalPost(M.personId, { date: today });
r = await call('POST', `/api/persons/${M.personId}/medication-schemes`, { token: T.ref, body: { medicationId: medId, dose: '1 comprimido', time: '09:15' } });
rec('S5-E11', 'Con una evaluación nueva vigente se vuelve a permitir cargar el medicamento (201/200)', r.status === 201 || r.status === 200, `status=${r.status}`);
const W = await newPerson('Alerta');
await evalPost(W.personId, { date: monthsAgo(6, 20) });
const wst = await evalStatus(W.personId);
rec('S5-E12', 'Una evaluación que vence en 20 días activa la alerta de vencimiento próximo', wst.json?.alertaVencimientoProximo === true && wst.json?.diasParaVencer > 0 && wst.json?.diasParaVencer <= 30, `alerta=${wst.json?.alertaVencimientoProximo} dias=${wst.json?.diasParaVencer}`);
const X = await newPerson('SinAlerta');
await evalPost(X.personId, { date: monthsAgo(6, 40) });
const xst = await evalStatus(X.personId);
rec('S5-E13', 'Una evaluación que vence en 40 días no activa la alerta', xst.json?.alertaVencimientoProximo === false && xst.json?.estaVencida === false, `alerta=${xst.json?.alertaVencimientoProximo} dias=${xst.json?.diasParaVencer}`);

// ---------------- SCRUM-67 estado activo/inactivo (cambio manual, notificación, reactivación)
const S = await newPerson('Estado');
const fullName = `Estado QA ${tag}`;
const patch = (o, tok = T.ref) => call('PATCH', `/api/persons/${S.personId}/status`, { token: tok, body: { status: 1, reason: 'Internación', ...o } });
r = await patch({ reason: '' });
rec('S5-S01', 'El cambio manual sin motivo se rechaza (400)', r.status === 400, `status=${r.status}`);
r = await patch({}, T.esc);
rec('S5-S02', 'Escucha no puede cambiar el estado (403)', r.status === 403, `status=${r.status}`);
r = await patch({});
let d = await detail(S.recordId);
rec('S5-S03', 'Referente pasa la persona a Inactivo con motivo (204) y la ficha queda Inactiva', r.status === 204 && d?.status === 1, `status=${r.status} estado=${d?.status}`);
const h0 = d?.statusHistory?.[0];
rec('S5-S04', 'El historial de estados guarda estado anterior y nuevo, fecha, motivo y usuario', h0?.previousValue === 'Activo' && h0?.newValue === 'Inactivo' && h0?.reason === 'Internación' && !!h0?.modifiedAt && h0?.modifiedBy && h0.modifiedBy !== 'Sistema', JSON.stringify(h0));
r = await patch({ reason: 'Otra vez' });
rec('S5-S05', 'Cambiar al mismo estado se rechaza (409)', r.status === 409, `status=${r.status} ${r.json?.message ?? ''}`);
const notif = await call('GET', '/api/notifications', { token: T.ref });
rec('S5-S06', 'Los Referentes reciben una notificación al pasar a Inactivo', notif.json?.some?.(n => n.eventType === 'PersonStatusInactive' && n.description?.includes(fullName)), `n=${notif.json?.length}`);
r = await call('POST', '/api/attendances', { token: T.ref, body: { personId: S.personId, activity: 0, date: today, anonymousCount: 0 } });
d = await detail(S.recordId);
rec('S5-S07', 'Al registrar una asistencia la persona vuelve a Activo y queda en el historial', r.status === 204 && d?.status === 0 && d?.statusHistory?.[0]?.newValue === 'Activo', `asistencia=${r.status} estado=${d?.status} ultimo=${JSON.stringify(d?.statusHistory?.[0])}`);
const list = await call('GET', `/api/social-records/list?status=Inactive&q=${encodeURIComponent('Estado')}`, { token: T.ref });
rec('S5-S08', 'El listado de fichas permite filtrar por estado (la persona reactivada no figura como Inactiva)', list.status === 200 && !(list.json?.items ?? []).some(i => i.id === S.recordId), `status=${list.status}`);

// ---------------- SCRUM-70 catálogo de Cáritas y campos de la ficha
const all = (await call('GET', '/api/caritas-catalogs?onlyActive=false', { token: T.ref })).json ?? [];
const byCat = c => all.filter(v => v.category === c);
rec('S5-C01', 'El catálogo trae valores para los 5 campos del informe', [0, 1, 2, 3, 4].every(c => byCat(c).length > 0), [0, 1, 2, 3, 4].map(c => byCat(c).length).join('/'));
const valueName = `Situación QA ${tag}`;
r = await call('POST', '/api/caritas-catalogs', { token: T.ref, body: { category: 4, name: valueName } });
const created = r.json;
rec('S5-C02', 'Referente agrega un valor al motivo de asistencia (201)', r.status === 201 && created?.name === valueName, `status=${r.status}`);
r = await call('POST', '/api/caritas-catalogs', { token: T.ref, body: { category: 4, name: valueName } });
rec('S5-C03', 'Un valor repetido en el mismo campo se rechaza (409)', r.status === 409, `status=${r.status} ${r.json?.message ?? ''}`);
r = await call('POST', '/api/caritas-catalogs', { token: T.esc, body: { category: 4, name: `Otro ${tag}` } });
rec('S5-C04', 'Escucha no puede modificar el catálogo (403)', r.status === 403, `status=${r.status}`);
r = await call('PUT', `/api/caritas-catalogs/${created?.id}`, { token: T.ref, body: { name: `${valueName} editado` } });
rec('S5-C05', 'Referente edita el nombre de un valor (200)', r.status === 200 && r.json?.name === `${valueName} editado`, `status=${r.status}`);
const R = await newPerson('Caritas');
const reason = byCat(4).find(v => v.isActive), gender = byCat(0).find(v => v.isActive);
r = await call('PUT', `/api/social-records/${R.recordId}/report-fields`, { token: T.ref, body: { genderId: gender?.id, attendanceReasonId: reason?.id } });
d = await detail(R.recordId);
rec('S5-C06', 'Los campos del informe se guardan en la ficha y se leen en el detalle', r.status === 204 && d?.reportFields?.genderId === gender?.id && d?.reportFields?.attendanceReasonId === reason?.id && d?.reportFields?.housingSituationId === null, `status=${r.status} ${JSON.stringify(d?.reportFields)}`);
r = await call('PATCH', `/api/caritas-catalogs/${created?.id}/status`, { token: T.ref, body: { isActive: false } });
const active = (await call('GET', '/api/caritas-catalogs?category=AttendanceReason', { token: T.ref })).json ?? [];
const inactiveRow = (await call('GET', '/api/caritas-catalogs?onlyActive=false', { token: T.ref })).json?.find(v => v.id === created?.id);
rec('S5-C07', 'Desactivar un valor (204) lo saca de las opciones activas sin borrarlo', r.status === 204 && !active.some(v => v.id === created?.id) && inactiveRow?.isActive === false, `status=${r.status} sigue=${!!inactiveRow}`);
r = await call('PUT', `/api/social-records/${R.recordId}/report-fields`, { token: T.ref, body: { attendanceReasonId: created?.id } });
rec('S5-C08', 'Un valor desactivado no se puede asignar a una ficha (400)', r.status === 400, `status=${r.status} ${r.json?.message ?? ''}`);
r = await call('PUT', `/api/social-records/${R.recordId}/report-fields`, { token: T.esc, body: { genderId: gender?.id } });
rec('S5-C09', 'Escucha no puede cargar los campos del informe (403)', r.status === 403, `status=${r.status}`);

// ---------------- SCRUM-69 informe de Cáritas
await call('POST', '/api/attendances', { token: T.ref, body: { personId: R.personId, activity: 0, date: today, anonymousCount: 0 } });
r = await call('GET', `/api/caritas-reports?type=trimestral&to=${today}`, { token: T.ref });
const rep = r.json;
const sum = items => (items ?? []).reduce((s, i) => s + i.count, 0);
const total = rep?.totals?.attendedPersons;
rec('S5-R01', 'Referente genera el informe trimestral con totales y desglose por motivo (200, datos reales)', r.status === 200 && rep?.isSample === false && total > 0 && sum(rep?.reasons) === total, `status=${r.status} personas=${total} motivos=${sum(rep?.reasons)}`);
rec('S5-R02', 'Cada categoría del informe suma el total de personas atendidas', sum(rep?.gender) === total && sum(rep?.housing) === total && sum(rep?.dni) === total && sum(rep?.geography) === total, `género=${sum(rep?.gender)} vivienda=${sum(rep?.housing)} dni=${sum(rep?.dni)}`);
r = await call('GET', `/api/caritas-reports?type=anual&to=${today}`, { token: T.ref });
rec('S5-R03', 'Un período que no es trimestral ni semestral se rechaza (400)', r.status === 400, `status=${r.status}`);
r = await call('GET', `/api/caritas-reports?type=semestral&to=${iso(ar(5))}`, { token: T.ref });
rec('S5-R04', 'Una fecha de corte futura se rechaza (400)', r.status === 400, `status=${r.status}`);
r = await call('GET', `/api/caritas-reports/pdf?type=semestral&to=${today}`, { token: T.ref });
rec('S5-R05', 'El informe se exporta a PDF (200, application/pdf)', r.status === 200 && /pdf/.test(r.headers.get('content-type') ?? '') && r.buf.subarray(0, 4).toString() === '%PDF', `status=${r.status} type=${r.headers.get('content-type')} bytes=${r.buf.length}`);
r = await call('GET', `/api/caritas-reports?type=trimestral&to=${today}`, { token: T.esc });
rec('S5-R06', 'Escucha no puede generar el informe (403)', r.status === 403, `status=${r.status}`);

// ---------------- SCRUM-77 respaldos (listado y permisos; la generación se prueba aparte)
r = await call('GET', '/api/admin/backups?page=1&pageSize=30', { token: T.ref });
rec('S5-B01', 'Referente consulta el listado de respaldos (200)', r.status === 200 && Array.isArray(r.json?.items), `status=${r.status} n=${r.json?.items?.length}`);
for (const [tok, role] of [[T.esc, 'Escucha'], [T.dir, 'Directora'], [T.coo, 'Coordinador']]) {
  r = await call('GET', '/api/admin/backups', { token: tok });
  rec(`S5-B02-${role.slice(0, 3)}`, `${role} no accede a los respaldos (403)`, r.status === 403, `status=${r.status}`);
}
r = await call('GET', '/api/admin/backups/00000000-0000-0000-0000-000000000001/download', { token: T.ref });
rec('S5-B03', 'Descargar un respaldo inexistente responde 404', r.status === 404, `status=${r.status}`);

// ---------------- SCRUM-23 registro de la exportación de la ficha
const auditBefore = sql(`SELECT COUNT(*) FROM audit_logs WHERE Action LIKE 'Ficha exportada%'`);
r = await call('POST', `/api/social-records/${A.recordId}/exports`, { token: T.ref, body: { sections: 'todo', format: 'pdf' } });
rec('S5-X01', 'El backend registra quién exportó la ficha y cuándo (204 y auditoría)', r.status === 204 && Number(sql(`SELECT COUNT(*) FROM audit_logs WHERE Action LIKE 'Ficha exportada%'`)) > Number(auditBefore), `status=${r.status}`);
r = await call('POST', `/api/social-records/${A.recordId}/exports`, { token: T.esc, body: { sections: 'todo', format: 'pdf' } });
rec('S5-X02', 'Escucha no puede exportar la ficha (403)', r.status === 403, `status=${r.status}`);

// ---------------- SCRUM-67 job diario de inactivación (corre al arrancar la API)
const J = await newPerson('Job');
sql(`UPDATE social_records SET CreatedAt = DATEADD(day, -40, GETUTCDATE()) WHERE Id='${J.recordId}'`);
const N = await newPerson('JobNueva');
const K = await newPerson('JobReciente');
await call('POST', '/api/attendances', { token: T.ref, body: { personId: K.personId, activity: 0, date: iso(ar(-29)), anonymousCount: 0 } });
execFileSync('docker', ['restart', 'vicaria-api'], { encoding: 'utf8' });
for (let i = 0; i < 60; i++) {
  try { if ((await fetch(API + '/api/caritas-catalogs')).status === 401) break; } catch { /* arrancando */ }
  await new Promise(ok => setTimeout(ok, 1000));
}
await new Promise(ok => setTimeout(ok, 3000));
T = await tokens();
const dj = await detail(J.recordId), dk = await detail(K.recordId);
rec('S5-J01', 'El proceso diario pasa a Inactivo a quien no asiste hace más de 30 días', dj?.status === 1, `estado=${dj?.status}`);
rec('S5-J02', 'El pase automático queda en el historial como Sistema, con fecha y motivo', dj?.statusHistory?.[0]?.modifiedBy === 'Sistema' && /30 días/.test(dj?.statusHistory?.[0]?.reason ?? ''), JSON.stringify(dj?.statusHistory?.[0]));
rec('S5-J03', 'Quien asistió hace 29 días sigue Activo', dk?.status === 0, `estado=${dk?.status}`);
const dn = await detail(N.recordId);
rec('S5-J05', 'Una ficha dada de alta hoy, todavía sin asistencias, no pasa a Inactivo', dn?.status === 0, `estado=${dn?.status}`);
const notifJob = await call('GET', '/api/notifications', { token: T.ref });
rec('S5-J04', 'Los Referentes reciben una notificación cuando el proceso diario pasa a alguien a Inactivo', notifJob.json?.some?.(n => n.eventType === 'PersonStatusInactive' && n.description?.includes(`Job QA ${tag}`)), `n=${notifJob.json?.length}`);

const f = res.filter(x => !x.ok);
console.log(`\nTOTAL ${res.length} PASS ${res.length - f.length} FAIL ${f.length}`);
fs.writeFileSync(process.env.OUT || new URL('./sprint5-api-results.json', import.meta.url), JSON.stringify(res, null, 1));
