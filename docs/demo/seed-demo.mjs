// Seed de demostración (Sprint 3, 4 y 5): carga muchos datos de ejemplo realistas por la API del
// stack local de Docker, para mostrar el sistema. Se corre sobre una base recién creada (ver
// docs/demo/README.md). Lo que la API no permite cargar con fecha pasada (alta de la ficha,
// fecha de las observaciones, visitas ya realizadas, historial de respaldos) se ajusta por SQL.
// Uso: node docs/demo/seed-demo.mjs
import { execFileSync } from 'child_process';

const API = process.env.API || 'http://localhost:5000';
const PASS = 'Test1234!';

// ----------------------------------------------------------------- utilidades
let seed = 20261028;
const rnd = () => ((seed = (seed * 1103515245 + 12345) % 2147483648) / 2147483648);
const pick = (arr) => arr[Math.floor(rnd() * arr.length)];
const chance = (p) => rnd() < p;

const call = async (method, path, { token, body, form } = {}) => {
  const headers = token ? { Authorization: `Bearer ${token}` } : {};
  if (body) headers['Content-Type'] = 'application/json';
  const r = await fetch(API + path, { method, headers, body: form ?? (body ? JSON.stringify(body) : undefined) });
  const text = await r.text();
  let json = null; try { json = JSON.parse(text); } catch { /* sin cuerpo */ }
  return { status: r.status, json, text };
};
const ok = (r, what) => {
  if (r.status >= 300) throw new Error(`${what}: ${r.status} ${r.text.slice(0, 300)}`);
  return r.json;
};
const sql = (q) => execFileSync('docker', ['exec', 'vicaria-sqlserver', 'bash', '-c',
  `/opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -d VicariaDb -h -1 -W -Q "SET NOCOUNT ON; ${q.replace(/"/g, '\\"')}"`],
  { encoding: 'utf8', env: { ...process.env, MSYS_NO_PATHCONV: '1' } }).trim();

// fechas de Córdoba (UTC-3)
const ar = (days = 0) => new Date(Date.now() - 3 * 36e5 + days * 864e5);
const iso = (d) => d.toISOString().slice(0, 10);
const today = iso(ar());
const daysAgo = (n) => iso(ar(-n));
const monthsAgo = (m, extraDays = 0) => { const d = ar(-extraDays); d.setUTCMonth(d.getUTCMonth() - m); return iso(d); };

const login = async (email) => ok(await call('POST', '/api/auth/login', { body: { email, password: PASS } }), `login ${email}`).token;

// ----------------------------------------------------------------- usuarios con nombres reales de demo
const USERS = [
  ['referente@test.com', 'Laura', 'Giménez'],
  ['directora@test.com', 'Silvia', 'Ramos'],
  ['escucha@test.com', 'Diego', 'Ferreyra'],
  ['coordinador@test.com', 'Martín', 'Castro'],
  ['pendiente1@test.com', 'Paula', 'Olmedo'],
  ['pendiente2@test.com', 'Javier', 'Luna'],
];
for (const [email, first, last] of USERS) sql(`UPDATE users SET FirstName = N'${first}', LastName = N'${last}' WHERE Email = '${email}'`);
console.log('usuarios renombrados');

const T = { ref: await login('referente@test.com'), dir: await login('directora@test.com'), esc: await login('escucha@test.com'), coo: await login('coordinador@test.com') };

// ----------------------------------------------------------------- catálogos
const catalog = ok(await call('GET', '/api/caritas-catalogs?onlyActive=false', { token: T.ref }), 'catálogo de Cáritas');
for (const name of ['Córdoba Capital', 'Villa Carlos Paz', 'Río Ceballos', 'Malagueño', 'Alta Gracia', 'Jesús María']) {
  const r = await call('POST', '/api/caritas-catalogs', { token: T.ref, body: { category: 2, name } });
  if (r.status < 300) catalog.push(r.json);
}
const byCat = (c) => catalog.filter((v) => (v.category === c || v.category === ['Gender', 'HousingSituation', 'OriginLocality', 'DocumentationStatus', 'AttendanceReason'][c]) && v.isActive);
const catByName = (c, name) => byCat(c).find((v) => v.name === name)?.id ?? null;

const obsCats = ok(await call('GET', '/api/observation-categories?onlyActive=true', { token: T.ref }), 'categorías de observaciones');
const obsCat = (name) => obsCats.find((c) => c.name === name)?.id ?? obsCats[0].id;

// ----------------------------------------------------------------- personas
// [nombre, apellido, género, edad, situación, ocupación, motivo de asistencia, localidad, documento, tipo]
const PEOPLE = [
  ['Ramón', 'Acosta', 'M', 54, 'En situación de calle', 'Changas', 'Consumo de alcohol', 'Córdoba Capital', 'Con DNI', 'res'],
  ['Héctor', 'Benítez', 'M', 47, 'En situación de calle', 'Cartonero', 'Consumo de drogas', 'Córdoba Capital', 'Con DNI', 'res'],
  ['Gustavo', 'Cabrera', 'M', 39, 'Transitorio', 'Albañil', 'Múltiple', 'Villa Carlos Paz', 'En trámite', 'res'],
  ['Sergio', 'Díaz', 'M', 61, 'En situación de calle', null, 'Consumo de alcohol', 'Otra provincia', 'Sin DNI', 'res'],
  ['Mónica', 'Escobar', 'F', 44, 'Transitorio', 'Empleada doméstica', 'Violencia', 'Córdoba Capital', 'Con DNI', 'res'],
  ['Walter', 'Funes', 'M', 36, 'En situación de calle', 'Lavacoches', 'Consumo de drogas', 'Malagueño', 'Con DNI', 'res'],
  ['Claudio', 'Godoy', 'M', 58, 'En situación de calle', 'Changas', 'Consumo de alcohol', 'Córdoba Capital', 'En trámite', 'res'],
  ['Norma', 'Herrera', 'F', 63, 'Transitorio', 'Jubilada', 'Múltiple', 'Alta Gracia', 'Con DNI', 'res'],
  ['Luis', 'Ibarra', 'M', 29, 'En situación de calle', 'Vendedor ambulante', 'Consumo de drogas', 'Córdoba Capital', 'Con DNI', 'amb'],
  ['Rosa', 'Juárez', 'F', 52, 'Con domicilio', 'Costurera', 'No especificado', 'Córdoba Capital', 'Con DNI', 'amb'],
  ['Daniel', 'Ledesma', 'M', 41, 'En situación de calle', 'Changas', 'Consumo de alcohol', 'Río Ceballos', 'Sin DNI', 'amb'],
  ['Patricia', 'Medina', 'F', 38, 'Con domicilio', 'Desocupada', 'Violencia', 'Córdoba Capital', 'Con DNI', 'amb'],
  ['Jorge', 'Navarro', 'M', 66, 'En situación de calle', 'Jubilado', 'Consumo de alcohol', 'Córdoba Capital', 'Con DNI', 'amb'],
  ['Cristian', 'Ojeda', 'M', 24, 'Transitorio', 'Repartidor', 'Consumo de drogas', 'Villa Carlos Paz', 'Con DNI', 'amb'],
  ['Graciela', 'Paz', 'F', 57, 'Con domicilio', 'Ama de casa', 'No especificado', 'Jesús María', 'Con DNI', 'amb'],
  ['Marcelo', 'Quiroga', 'M', 49, 'En situación de calle', 'Cuidacoches', 'Múltiple', 'Córdoba Capital', 'En trámite', 'amb'],
  ['Silvana', 'Romero', 'F', 33, 'Transitorio', 'Empleada de limpieza', 'Consumo de drogas', 'Córdoba Capital', 'Con DNI', 'amb'],
  ['Alberto', 'Sosa', 'M', 70, 'En situación de calle', 'Jubilado', 'Consumo de alcohol', 'Otra provincia', 'Sin DNI', 'amb'],
  ['Brenda', 'Toledo', 'F', 22, 'Con domicilio', 'Estudiante', 'Violencia', 'Córdoba Capital', 'Con DNI', 'amb'],
  ['Hugo', 'Vera', 'M', 55, 'En situación de calle', 'Changas', 'Consumo de alcohol', 'Malagueño', 'Con DNI', 'amb'],
  ['Fabián', 'Zárate', 'M', 45, 'Transitorio', 'Pintor', 'Múltiple', 'Córdoba Capital', 'Con DNI', 'amb'],
  ['Elena', 'Aguirre', 'F', 68, 'Con domicilio', 'Jubilada', 'No especificado', 'Alta Gracia', 'Con DNI', 'amb'],
  ['Nicolás', 'Bustos', 'M', 31, 'En situación de calle', 'Malabarista', 'Consumo de drogas', 'Otro país', 'Sin DNI', 'amb'],
  ['Carina', 'Castillo', 'F', 42, 'Transitorio', 'Vendedora', 'Violencia', 'Córdoba Capital', 'En trámite', 'amb'],
  ['Oscar', 'Domínguez', 'M', 59, 'En situación de calle', 'Changas', 'Consumo de alcohol', 'Córdoba Capital', 'Con DNI', 'amb'],
  ['Valeria', 'Farías', 'F', 35, 'Con domicilio', 'Niñera', 'Consumo de drogas', 'Río Ceballos', 'Con DNI', 'amb'],
  ['Rubén', 'Guzmán', 'M', 50, 'En situación de calle', 'Herrero', 'Múltiple', 'Córdoba Capital', 'Con DNI', 'amb'],
  ['Mirta', 'Luna', 'F', 64, 'Con domicilio', 'Jubilada', 'No especificado', 'Córdoba Capital', 'Con DNI', 'amb'],
  ['Ezequiel', 'Molina', 'M', 27, 'En situación de calle', null, 'Consumo de drogas', 'Córdoba Capital', 'Sin DNI', 'amb'],
  ['Andrea', 'Núñez', 'F', 46, 'Transitorio', 'Cocinera', 'Violencia', 'Villa Carlos Paz', 'Con DNI', 'amb'],
  ['Raúl', 'Peralta', 'M', 62, 'En situación de calle', 'Changas', 'Consumo de alcohol', 'Jesús María', 'Con DNI', 'amb'],
  ['Lorena', 'Ríos', 'F', 29, 'Con domicilio', 'Peluquera', 'No especificado', 'Córdoba Capital', null, 'amb'],
  ['Mario', 'Suárez', 'M', 53, 'En situación de calle', 'Cartonero', 'Múltiple', 'Córdoba Capital', 'En trámite', 'amb'],
  ['Teresa', 'Villalba', 'F', 71, 'Con domicilio', 'Jubilada', null, 'Malagueño', 'Con DNI', 'amb'],
  ['Federico', 'Arias', 'M', 34, 'Transitorio', 'Mozo', 'Consumo de alcohol', null, 'Con DNI', 'amb'],
  ['Juana', 'Bravo', 'F', 48, 'En situación de calle', null, 'Violencia', 'Córdoba Capital', 'Sin DNI', 'amb'],
  ['Pablo', 'Correa', 'M', 43, 'En situación de calle', 'Changas', 'Consumo de drogas', 'Córdoba Capital', 'Con DNI', 'amb'],
  ['Susana', 'Figueroa', 'F', 56, 'Con domicilio', 'Empleada', null, null, null, 'amb'],
];
// los últimos 6 dejan de venir hace más de 30 días: el proceso diario los pasa a Inactivo
const STOPPED = new Set(['Federico', 'Juana', 'Pablo', 'Susana', 'Teresa', 'Mario']);

const REASONS = {
  'Consumo de alcohol': 'Consumo problemático de alcohol; se acerca por el comedor y pide acompañamiento.',
  'Consumo de drogas': 'Consumo de pasta base; derivado por el equipo de calle de la parroquia.',
  'Violencia': 'Situación de violencia familiar; llega acompañada por una vecina.',
  'Múltiple': 'Consumo de alcohol y otras sustancias, sin red familiar.',
  'No especificado': 'Se acerca por el comedor y la ropería.',
};
const OVERNIGHT = { 'En situación de calle': ['Plaza Colón', 'Terminal de ómnibus', 'Bajo puente Sarmiento', 'Galería del centro'], 'Transitorio': ['Pensión de barrio Alberdi', 'Casa de un familiar', 'Parador municipal'], 'Con domicilio': ['Domicilio propio', 'Casa alquilada'] };

const OBS = {
  Salud: ['Control de presión en el centro de salud del barrio; valores normales.', 'Refiere dolor de muela; se gestiona turno odontológico en el dispensario.', 'Se lo acompaña al hospital por una herida en la pierna; curación y vacuna antitetánica.', 'Retomó el tratamiento en el CPA; asiste a los grupos de los martes.', 'Buen estado general; se lo ve más tranquilo y descansado.'],
  'Documentación': ['Inició el trámite de DNI en el Registro Civil con acompañamiento.', 'Se le entregó la constancia de CUIL impresa.', 'Retiró el DNI nuevo; se escaneó para la ficha.', 'Se averigua la pensión no contributiva en ANSES.'],
  'Situación habitacional': ['Durmió en el parador municipal toda la semana.', 'Consiguió una pieza en una pensión; se coordina ayuda con el alquiler.', 'Volvió a dormir en la plaza después de una discusión en la pensión.'],
  General: ['Participó del almuerzo y ayudó a ordenar el salón.', 'Se sumó al taller de huerta; muy buena predisposición.', 'Pidió ropa de abrigo; se entregó campera y frazada.', 'Conversación larga con la escucha; cuenta que quiere retomar contacto con su hija.', 'Llegó tarde y con aliento etílico; se le ofreció ducha y almuerzo.'],
};
const LIFE = [
  { beforeCentroBarrial: 'Creció en barrio Müller con sus abuelos. Trabajó en la construcción desde los 15 años. Después de separarse quedó en situación de calle.', inCentroBarrial: 'Llega al Centro Barrial por el comedor. Con el tiempo empieza a participar del taller de huerta y de los grupos.', afterCentroBarrial: 'Quiere conseguir un trabajo estable y alquilar una pieza cerca de su hija.' },
  { beforeCentroBarrial: 'Nació en Santiago del Estero y vino a Córdoba a trabajar en la cosecha. Perdió el contacto con su familia hace años.', inCentroBarrial: 'Se acerca por la ropería en invierno. Empieza el tratamiento por consumo de alcohol en el CPA.', afterCentroBarrial: null },
  { beforeCentroBarrial: 'Vivía con su pareja en barrio Los Cortaderos; atravesó una situación de violencia de género.', inCentroBarrial: 'La derivan desde el Polo de la Mujer. Participa de las charlas de los jueves y del taller de costura.', afterCentroBarrial: 'Consiguió trabajo en una casa de familia y alquila con su hermana.' },
];
const SURNAMES_CONTACT = ['Acosta', 'Rivero', 'Paz', 'Gómez', 'López', 'Moreno'];

const people = [];
for (const [first, last, gender, age, housing, occupation, reason, locality, docStatus, kind] of PEOPLE) {
  const dob = `${new Date().getFullYear() - age}-${String(1 + Math.floor(rnd() * 12)).padStart(2, '0')}-${String(1 + Math.floor(rnd() * 27)).padStart(2, '0')}`;
  const hasDni = docStatus === 'Con DNI';
  const entryDays = kind === 'res' ? 120 + Math.floor(rnd() * 200) : 60 + Math.floor(rnd() * 300);
  const body = {
    firstName: first, lastName: last,
    dni: hasDni ? String(20000000 + Math.floor(rnd() * 25000000)) : null,
    dateOfBirth: dob, phone: chance(0.6) ? `351${Math.floor(4000000 + rnd() * 5999999)}` : null,
    personType: 0,
    reasonForEntry: reason ? REASONS[reason] : null,
    entryDate: daysAgo(entryDays), housingSituation: housing, overnightLocation: pick(OVERNIGHT[housing]),
    occupation, hasDocumentation: hasDni,
    generalNotes: chance(0.5) ? pick(['Celíaco: avisar en la cocina.', 'Prefiere que lo llamen por el apodo.', 'Tiene un perro que lo acompaña.', 'Hipertenso, toma enalapril.']) : null,
    contact: chance(0.5) ? { firstName: pick(['Marta', 'Carlos', 'Ana', 'Jorge', 'Lidia']), lastName: pick(SURNAMES_CONTACT), phone: `351${Math.floor(4000000 + rnd() * 5999999)}`, address: pick(['Barrio Alberdi', 'Barrio San Vicente', 'Barrio Güemes']) } : null,
  };
  const created = ok(await call('POST', '/api/social-records', { token: T.ref, body }), `ficha ${first}`);
  const p = { first, last, gender, reason, locality, docStatus, kind, entryDays, personId: created.personId, recordId: created.id };
  people.push(p);
  // alta de la ficha con la fecha real de ingreso
  sql(`UPDATE social_records SET CreatedAt = DATEADD(day, -${entryDays}, SYSUTCDATETIME()) WHERE Id = '${p.recordId}'`);
}
console.log(`${people.length} fichas nuevas`);

// campos del informe de Cáritas (algunos quedan pendientes a propósito)
for (const p of people) {
  const body = {
    genderId: catByName(0, p.gender === 'F' ? 'Femenino' : 'Masculino'),
    housingSituationId: catByName(1, PEOPLE.find((x) => x[0] === p.first)[4]),
    originLocalityId: p.locality ? (catByName(2, p.locality) ?? catByName(2, p.locality === 'Otra provincia' ? 'Otra provincia' : 'Misma ciudad')) : null,
    documentationStatusId: p.docStatus ? catByName(3, p.docStatus) : null,
    attendanceReasonId: p.reason ? catByName(4, p.reason) : null,
  };
  ok(await call('PUT', `/api/social-records/${p.recordId}/report-fields`, { token: T.ref, body }), `Cáritas ${p.first}`);
}

// observaciones de la Referente y la Escucha, con fechas repartidas desde el ingreso
let obsCount = 0;
for (const p of people) {
  const n = 2 + Math.floor(rnd() * 5);
  for (let i = 0; i < n; i++) {
    const cat = pick(Object.keys(OBS));
    const r = ok(await call('POST', `/api/persons/${p.personId}/observations`, { token: chance(0.6) ? T.ref : T.esc, body: { categoryId: obsCat(cat), content: pick(OBS[cat]) } }), 'observación');
    const back = Math.floor(rnd() * Math.min(p.entryDays, 150));
    if (r?.id) sql(`UPDATE observations SET created_at = DATEADD(minute, -${back * 1440 + Math.floor(rnd() * 600)}, SYSUTCDATETIME()) WHERE Id = '${r.id}'`);
    obsCount++;
  }
}
console.log(`${obsCount} observaciones`);

// historia de vida
for (const [i, p] of people.entries()) {
  if (i % 3 !== 0) continue;
  ok(await call('PUT', `/api/persons/${p.personId}/life-story`, { token: T.ref, body: LIFE[i % LIFE.length] }), 'historia de vida');
}

// ----------------------------------------------------------------- Sprint 5: evaluación psiquiátrica y Casa de Convivencia
const PROS = ['Dra. Silvina López', 'Dr. Fernando Castro', 'Lic. Mariana Sosa'];
const DIAG = ['Apto para convivencia asistida', 'Trastorno por consumo de alcohol, en tratamiento', 'Apto, con seguimiento psicológico semanal', 'Episodio depresivo leve; apto para convivencia'];
const residents = people.filter((p) => p.kind === 'res');
for (const [i, p] of residents.entries()) {
  // historial: una evaluación vieja y la vigente
  ok(await call('POST', `/api/persons/${p.personId}/psychiatric-evaluations`, { token: T.ref, body: { date: monthsAgo(8 + i % 3), professional: pick(PROS), diagnosis: pick(DIAG) } }), 'evaluación vieja');
  // la 2ª vence en menos de 30 días (alerta), el resto está al día
  const current = i === 1 ? monthsAgo(5, 15) : daysAgo(10 + i * 9);
  ok(await call('POST', `/api/persons/${p.personId}/psychiatric-evaluations`, { token: T.ref, body: { date: current, professional: pick(PROS), diagnosis: pick(DIAG) } }), 'evaluación vigente');
  ok(await call('PUT', `/api/persons/${p.personId}/type`, { token: T.ref, body: { personType: 1 } }), `ingreso a Casa de Convivencia ${p.first}`);
}
// la estadía empieza con el ingreso de la ficha (unos meses atrás)
for (const p of residents) sql(`UPDATE casa_convivencia_stays SET EntryDate = DATEADD(day, -${Math.min(p.entryDays - 5, 90)}, SYSUTCDATETIME()) WHERE PersonId = '${p.personId}' AND ExitDate IS NULL`);
console.log(`${residents.length} residentes con evaluación vigente`);

// ----------------------------------------------------------------- Sprint 4: stock y esquemas de medicación
const MEDS = [['Clonazepam 0,5 mg', 120], ['Quetiapina 25 mg', 90], ['Sertralina 50 mg', 60], ['Omeprazol 20 mg', 80], ['Enalapril 10 mg', 45], ['Levomepromazina 25 mg', 8], ['Complejo B', 100], ['Ibuprofeno 400 mg', 4]];
for (const [nombre, cantidad] of MEDS) {
  ok(await call('POST', '/api/stock-medicamentos', { token: T.ref, body: { nombre, cantidad, fechaVencimiento: `${new Date().getFullYear() + 1}-0${1 + Math.floor(rnd() * 9)}-15T00:00:00`, proveedor: pick(['Farmacia Sindical', 'Donación Cáritas', 'Hospital Rawson']) } }), `stock ${nombre}`);
}
const medCatalog = ok(await call('GET', '/api/medication-schemes/catalog', { token: T.ref }), 'catálogo de medicación');
const medId = (name) => medCatalog.find((m) => m.name === name)?.id;
const PLANS = [[['Clonazepam 0,5 mg', '21:00'], ['Omeprazol 20 mg', '08:00']], [['Quetiapina 25 mg', '22:00'], ['Complejo B', '09:00']], [['Sertralina 50 mg', '08:30'], ['Enalapril 10 mg', '08:30']], [['Levomepromazina 25 mg', '21:30'], ['Omeprazol 20 mg', '08:00'], ['Complejo B', '13:00']]];
for (const [i, p] of residents.entries()) {
  for (const [name, time] of PLANS[i % PLANS.length]) {
    const r = await call('POST', `/api/persons/${p.personId}/medication-schemes`, { token: T.ref, body: { medicationId: medId(name), dose: '1 comprimido', time } });
    if (r.status >= 300) console.warn(`esquema ${p.first} ${name}: ${r.status} ${r.text.slice(0, 120)}`);
  }
}
// agenda de hoy: las tomas de la mañana ya se dieron, una quedó sin dar
const agenda = ok(await call('GET', `/api/medicamentos/agenda?fecha=${today}`, { token: T.dir }), 'agenda');
const intakes = (agenda?.groups ?? []).flatMap((g) => (g.intakes ?? []).map((it) => ({ ...it, time: g.time })));
for (const [i, it] of intakes.entries()) {
  if (!/^0[89]/.test(it.time)) continue;
  await call('PATCH', `/api/medicamentos/agenda/${it.id}`, { token: T.dir, body: i % 5 === 4 ? { status: 3, observation: 'Se negó a tomarla; se avisó a la directora' } : { status: 2, observation: null } });
}
// una residente con la evaluación vencida (más de 6 meses): bloquea medicación nueva
const expired = residents[residents.length - 1];
sql(`UPDATE psychiatric_evaluations SET Date = DATEADD(month, -7, SYSUTCDATETIME()) WHERE PersonId = '${expired.personId}' AND IsValid = 1`);
console.log(`${intakes.length} tomas en la agenda de hoy; evaluación vencida: ${expired.first} ${expired.last}`);

// ----------------------------------------------------------------- Sprint 4: asistencia diaria (últimos 4 meses)
let attCount = 0;
const registerDay = async (p, date, activity) => {
  const r = await call('POST', '/api/attendances', { token: T.ref, body: { personId: p.personId, activity, date, anonymousCount: 0 } });
  if (r.status < 300) attCount++;
};
for (let d = 120; d >= 0; d--) {
  const date = daysAgo(d);
  const dow = ar(-d).getUTCDay();
  for (const p of people) {
    if (d > p.entryDays) continue;
    if (p.kind === 'res') { await registerDay(p, date, 1); continue; }
    if (dow === 0) continue; // domingo cerrado
    if (STOPPED.has(p.first) && d < 35 + (p.first.length % 10)) continue;
    const freq = 0.25 + ((p.first.charCodeAt(0) * 7) % 60) / 100;
    if (chance(freq)) await registerDay(p, date, 0);
  }
  // asistentes anónimos del comedor
  if (dow !== 0) await call('POST', '/api/attendances', { token: T.ref, body: { personId: people[0].personId, activity: 0, date, anonymousCount: 3 + Math.floor(rnd() * 12) } });
}
console.log(`${attCount} asistencias`);

// ----------------------------------------------------------------- Sprint 4: actividades
for (const name of ['Comedor', 'Taller de huerta', 'Grupo de autoayuda', 'Taller de carpintería', 'Ropería', 'Apoyo escolar', 'Fútbol de los viernes']) {
  await call('POST', '/api/actividades', { token: T.ref, body: { name } });
}

// ----------------------------------------------------------------- Sprint 3: calendario general y personal
const EV = [
  ['Comedor del mediodía', '12:00', '14:00', 'Almuerzo para las personas del Centro Barrial.', 1 | 2 | 4 | 8 | 16, false],
  ['Merendero', '17:00', '18:30', 'Merienda con los chicos del barrio.', 2, false],
  ['Taller de huerta', '10:00', '12:00', 'En el patio de atrás; traer guantes.', 8, false],
  ['Grupo de autoayuda', '18:00', '19:30', 'Coordina la Lic. Mariana Sosa.', 1 | 4, false],
  ['Reunión de equipo', '09:00', '10:30', 'Revisión de casos y organización de la semana.', 1, false],
  ['Misa en la capilla', '19:00', '20:00', 'Misa mensual con las familias.', 0, true],
];
for (const [title, s, e, description, days, monthly] of EV) {
  ok(await call('POST', '/api/general-calendar-events', { token: T.ref, body: { title, date: daysAgo(30), startTime: `${s}:00`, endTime: `${e}:00`, description, recurrenceDays: days, repeatsMonthly: monthly } }), `evento ${title}`);
}
for (const [title, offset, s, description] of [['Jornada de vacunación', 3, '09:00', 'Con el centro de salud N° 42.'], ['Visita del Arzobispado', 8, '11:00', 'Recorrida por la Casa de Convivencia.'], ['Feria de ropa', 12, '10:00', 'Venta solidaria para la colecta de invierno.'], ['Charla de prevención de adicciones', 17, '18:00', 'En la escuela del barrio.']]) {
  ok(await call('POST', '/api/general-calendar-events', { token: T.ref, body: { title, date: daysAgo(-offset), startTime: `${s}:00`, endTime: null, description } }), `evento ${title}`);
}
for (const [tok, title, offset] of [[T.esc, 'Acompañar a Luis al Registro Civil', 2], [T.esc, 'Llamar a la hermana de Rosa', 1], [T.dir, 'Turno psiquiatría de Héctor', 4], [T.ref, 'Presentar informe a Cáritas', 6]]) {
  await call('POST', '/api/personal-calendar-events', { token: tok, body: { title, date: daysAgo(-offset), startTime: '10:00:00' } });
}

// ----------------------------------------------------------------- Sprint 3: colaboradores
const COLLAB = [['Ana', 'Bertolotti', 0, 'Comedor'], ['Tomás', 'Cuello', 0, 'Taller de huerta'], ['Inés', 'Del Valle', 1, 'Trabajo social'], ['Gabriel', 'Espinosa', 0, 'Apoyo escolar'], ['Lucía', 'Frías', 1, 'Psicología'], ['Ricardo', 'Gallo', 0, 'Mantenimiento'], ['Sofía', 'Heredia', 0, 'Ropería'], ['Matías', 'Iriarte', 1, 'Administración'], ['Carolina', 'Juncos', 0, 'Comedor'], ['Esteban', 'Krause', 0, 'Fútbol']];
for (const [first, last, type, area] of COLLAB) {
  await call('POST', '/api/collaborators', { token: T.ref, body: { firstName: first, lastName: last, dni: String(25000000 + Math.floor(rnd() * 20000000)), phone: `351${Math.floor(4000000 + rnd() * 5999999)}`, email: `${first.toLowerCase()}.${last.toLowerCase().replace(/\s/g, '')}@mail.com`, type, workArea: area } });
}

// ----------------------------------------------------------------- Sprint 3: visitas a la Casa de Convivencia
const VISITORS = ['Marta (hermana)', 'Jorge (hijo)', 'Ana (madre)', 'Pbro. Daniel', 'Lidia (amiga)', 'Carlos (primo)'];
const visits = [];
for (let i = 0; i < 14; i++) {
  const p = residents[i % residents.length];
  const r = await call('POST', '/api/casona-visits', { token: T.dir, body: { personId: p.personId, visitorName: pick(VISITORS), date: daysAgo(-(1 + (i % 7))), startTime: `${String(10 + (i % 7)).padStart(2, '0')}:00:00`, estimatedDurationMinutes: 60, allowOverlap: true } });
  if (r.status < 300 && r.json?.id) visits.push(r.json.id);
}
// la mitad ya pasó: realizadas o canceladas
for (const [i, id] of visits.entries()) {
  if (i % 2) continue;
  sql(`UPDATE casona_visits SET date = DATEADD(day, -${3 + i}, CAST(SYSUTCDATETIME() AS date)) WHERE id = '${id}'`);
  await call('PATCH', `/api/casona-visits/${id}/status`, { token: T.dir, body: i % 4 ? { status: 1, cancellationReason: null } : { status: 2, cancellationReason: 'La familia avisó que no podía venir' } });
}
console.log(`${COLLAB.length} colaboradores, ${visits.length} visitas`);

// ----------------------------------------------------------------- Sprint 5: adjuntos (imágenes y PDF generados con PyMuPDF)
const tmp = execFileSync('python', ['-c', `
import fitz, tempfile, os
d = tempfile.mkdtemp()
def doc(name, lines, png=False):
    pdf = fitz.open(); page = pdf.new_page(width=420, height=260 if png else 595)
    y = 40
    for l in lines:
        page.insert_text((30, y), l, fontsize=14 if y == 40 else 11); y += 22
    if png:
        page.draw_rect(fitz.Rect(10, 10, 410, 250), color=(0.2, 0.3, 0.6), width=2)
        page.get_pixmap(dpi=110).save(os.path.join(d, name))
    else:
        pdf.save(os.path.join(d, name))
doc('dni-frente.png', ['REPUBLICA ARGENTINA', 'Documento Nacional de Identidad', 'Copia escaneada para la ficha'], True)
doc('receta.png', ['Receta médica', 'Centro de salud N° 42', 'Indicación: control en 30 días'], True)
doc('certificado-cud.pdf', ['Certificado Único de Discapacidad', 'Trámite iniciado', 'Ministerio de Salud de Córdoba'])
doc('constancia-cuil.pdf', ['Constancia de CUIL', 'ANSES', 'Generada para el trámite de pensión'])
print(d)
`], { encoding: 'utf8' }).trim();
const fs = await import('fs');
const FILES = [['dni-frente.png', 'image/png'], ['receta.png', 'image/png'], ['certificado-cud.pdf', 'application/pdf'], ['constancia-cuil.pdf', 'application/pdf']];
let attachCount = 0;
for (const [i, p] of people.entries()) {
  if (i % 3 === 2) continue;
  const form = new FormData();
  for (const [name, type] of FILES.filter((_, j) => (i + j) % 2 === 0 || j === 0)) {
    form.append('files', new Blob([fs.readFileSync(`${tmp}/${name}`)], { type }), name);
  }
  const r = await call('POST', `/api/persons/${p.personId}/attachments`, { token: T.ref, form });
  if (r.status < 300) attachCount++; else console.warn(`adjuntos ${p.first}: ${r.status} ${r.text.slice(0, 120)}`);
}
console.log(`adjuntos en ${attachCount} fichas`);

// ----------------------------------------------------------------- Sprint 5: cambios de estado manuales con motivo
const manual = people.filter((p) => p.kind === 'amb' && !STOPPED.has(p.first)).slice(0, 2);
ok(await call('PATCH', `/api/persons/${manual[0].personId}/status`, { token: T.ref, body: { status: 1, reason: 'Viajó a Santiago del Estero con su familia' } }), 'estado manual');
ok(await call('PATCH', `/api/persons/${manual[1].personId}/status`, { token: T.ref, body: { status: 1, reason: 'Internación prolongada en el Hospital Misericordia' } }), 'estado manual');
ok(await call('PATCH', `/api/persons/${manual[1].personId}/status`, { token: T.ref, body: { status: 0, reason: 'Recibió el alta y volvió al Centro Barrial' } }), 'estado manual');

// ----------------------------------------------------------------- Sprint 5: historial de respaldos diarios (30 días)
const keys = [];
for (let d = 29; d >= 1; d--) {
  const failed = d === 12;
  const key = failed ? '' : `respaldo-demo-${daysAgo(d)}.bak`;
  sql(`INSERT INTO backups (Id, Status, CreatedAt, SizeBytes, ExternalKey, ErrorMessage) VALUES (NEWID(), ${failed ? 3 : 2}, DATEADD(minute, 3 * 60 + ${Math.floor(rnd() * 4)}, DATEADD(day, -${d}, CAST(CAST(SYSUTCDATETIME() AS date) AS datetime2))), ${failed ? 0 : 11800000 + Math.floor(rnd() * 900000)}, '${key}', ${failed ? "N'No se pudo conectar con el almacenamiento externo (tiempo de espera agotado).'" : 'NULL'})`);
  if (key) keys.push(key);
}
execFileSync('docker', ['exec', 'vicaria-api', 'sh', '-c', `mkdir -p /app/uploads/backups && cd /app/uploads/backups && for k in ${keys.join(' ')}; do printf 'Respaldo de demostración de la base VicariaDb' > $k; done`], { env: { ...process.env, MSYS_NO_PATHCONV: '1' } });
console.log(`${keys.length} respaldos exitosos y 1 fallido en el historial`);

console.log('\nListo. Reiniciá la API (docker restart vicaria-api) para que el proceso diario pase a Inactivo a quienes dejaron de venir.');
