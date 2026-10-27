// TC-251 (SCRUM-77): el sistema sigue funcionando mientras corre el respaldo diario.
// Se lanza antes de la hora del respaldo (Backup__ScheduleHourUtc) y consulta el listado de
// fichas cada 250 ms desde 30 s antes hasta 2 min después; guarda fallas y latencias.
// Uso: AT=13 node sprint5-backup-availability.mjs  (hora UTC del respaldo)
import fs from 'fs';

const API = process.env.API || 'http://localhost:5000';
const hour = Number(process.env.AT ?? 3);
const now = new Date();
const runAt = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate(), hour, 0, 0));
if (runAt <= now) runAt.setUTCDate(runAt.getUTCDate() + 1);
const start = new Date(runAt.getTime() - 30_000), end = new Date(runAt.getTime() + 120_000);

const token = (await (await fetch(`${API}/api/auth/login`, {
  method: 'POST', headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ email: 'referente@test.com', password: 'Test1234!' }),
})).json()).token;

await new Promise(ok => setTimeout(ok, Math.max(0, start - Date.now())));
const samples = [];
while (Date.now() < end) {
  const t0 = Date.now();
  let status = 0;
  try { status = (await fetch(`${API}/api/social-records/list?page=1`, { headers: { Authorization: `Bearer ${token}` } })).status; } catch { /* caída */ }
  samples.push({ at: new Date(t0).toISOString(), status, ms: Date.now() - t0 });
  await new Promise(ok => setTimeout(ok, 250));
}

const failures = samples.filter(s => s.status !== 200);
const result = { runAt: runAt.toISOString(), requests: samples.length, failures: failures.length, maxMs: Math.max(...samples.map(s => s.ms)), failuresDetail: failures.slice(0, 10) };
fs.writeFileSync(process.env.OUT || new URL('./sprint5-backup-availability.json', import.meta.url), JSON.stringify(result, null, 1));
console.log(JSON.stringify(result));
