# Levantar Vicaria en local

Guía paso a paso para clonar el repo y tener todo el stack (SQL Server + API + Frontend) corriendo en la propia máquina, con datos de prueba ya cargados. Pensada para QA manual, demos, y para arrancar a desarrollar sin depender de un backend/frontend remoto.

Hay dos formas de levantarlo:

- **Opción A — Docker Compose (recomendada)**: un solo comando, levanta SQL Server + API + Frontend ya buildeados, con el frontend sirviendo el `dist/` de producción vía nginx. Es lo más parecido a "clonar y listo". Ideal para demos.
- **Opción B — Servidores de desarrollo**: `dotnet run` + `ng serve`, con hot-reload. Ideal para desarrollar.

En ambos casos el resultado incluye datos de prueba precargados: 4 usuarios (uno por rol) y 6 personas de ejemplo (ambulatorios y residentes, con observaciones y estadías en la Casa de Convivencia) — ver [Datos de prueba](#datos-de-prueba-ya-cargados) más abajo.

---

## Requisitos

- **Docker Desktop** (opción A) — con al menos ~4GB de RAM asignados; SQL Server + API + build de Angular consumen bastante durante el `build`.
- **.NET SDK 9.0** (opción B, backend) — [descargar](https://dotnet.microsoft.com/download/dotnet/9.0).
- **Node.js** `^22.22.2` o superior (opción B, frontend) — la CLI de Angular exige esta versión mínima; Node más viejo falla al arrancar `ng`. Verificar con `node --version`.
- Windows: si no se puede actualizar el Node del sistema (falta de permisos de administrador), se puede usar una versión portátil descomprimida en cualquier carpeta y anteponerla al `PATH` de la sesión — no hace falta instalarla.

---

## Opción A — Docker Compose (recomendada para demos)

Todo el compose vive en `backend/docker-compose.yml`.

```bash
cd backend
cp .env.example .env
# .env está en .gitignore — no se commitea. Los valores por defecto de .env.example
# alcanzan para levantar en local; MSSQL_SA_PASSWORD y JWT_KEY solo importan si se
# va a exponer esto fuera de la propia máquina (no es el caso acá).

docker compose up -d --build
```

Esto levanta 3 contenedores:

| Contenedor | Puerto local | Qué es |
|---|---|---|
| `vicaria-sqlserver` | `1434` → 1433 | SQL Server 2022 Express |
| `vicaria-api` | `5000` | Backend (.NET), aplica migraciones y siembra datos de prueba solas al arrancar |
| `vicaria-web` | `4200` | Frontend (build de producción servido por nginx) |

Abrir **http://localhost:4200** y loguearse con cualquiera de los [usuarios de prueba](#datos-de-prueba-ya-cargados).

**Notas sobre el compose:**
- El servicio `web` usa `frontend/Dockerfile.local` + `frontend/nginx.local.conf` (no los `Dockerfile`/`nginx.conf` de raíz, que son los de producción y apuntan la API a `vicaria-api.mateopavoni.com.ar`). El `.local` proxea `/api/` al contenedor `api` de la propia red de compose — así el frontend containerizado habla con el backend local, no con producción.
- `FRONTEND_PATH` en `.env` asume que `frontend/` es carpeta hermana de `backend/` en el mismo checkout (`../frontend`) — es el layout normal de este repo, no hace falta tocarlo.
- Para parar todo: `docker compose down` (agregar `-v` si además se quiere borrar la base y volver a sembrar datos desde cero en el próximo `up`).
- Para reconstruir después de bajar una rama nueva: `docker compose up -d --build` de nuevo (rebuildea solo lo que cambió).

**Si Docker Desktop da errores raros** (containers que no arrancan, `500 Internal Server Error` de la API de Docker, timeouts): reiniciar Docker Desktop por completo (cerrar el ícono de la bandeja y volver a abrirlo, o `taskkill` de sus procesos + reabrir) suele resolverlo — es una inestabilidad conocida de Docker Desktop en Windows bajo uso prolongado, no un problema del proyecto.

---

## Opción B — Servidores de desarrollo (hot-reload)

### 1. Base de datos

La forma más simple: levantar solo el servicio `sql` del compose de arriba (`docker compose up -d sql`, con el `.env` ya creado) y apuntar el backend a `localhost:1434`. También sirve cualquier SQL Server 2022 local ya instalado.

### 2. Backend

```bash
cd backend
dotnet restore
dotnet build
```

Configurar los secretos locales en `src/Vicaria.Api/appsettings.Development.local.json` (no versionado, ver `.gitignore`):

```json
{
  "ConnectionStrings": {
    "VicariaDb": "Server=localhost,1434;Database=VicariaDb;User Id=sa;Password=<la de tu .env>;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "clave-de-desarrollo-solo-local-no-usar-en-produccion"
  }
}
```

```bash
dotnet run --project src/Vicaria.Api
```

Aplica las migraciones y siembra los datos de prueba solas al arrancar (solo en `Development`). Queda escuchando en `http://localhost:5187` (perfil `http` de `launchSettings.json`). Swagger en `http://localhost:5187/swagger`.

### 3. Frontend

```bash
cd frontend
npm install
npm start   # = ng serve
```

Queda escuchando en `http://localhost:4200`, con el proxy a la API (`proxy.conf.json` → `http://localhost:5187`) ya configurado en `angular.json` — no hace falta pasar `--proxy-config` a mano.

---

## Correr los tests

```bash
cd backend

# Unitarios (InMemory DB, rápidos, no necesitan Docker)
dotnet test tests/Vicaria.UnitTests

# Integración (Testcontainers, necesitan Docker corriendo y libre — no correr
# al mismo tiempo que el docker-compose de arriba, compiten por el daemon)
export ConnectionStrings__VicariaDb="Server=localhost;Database=Vicaria;Trusted_Connection=True;"  # dummy, solo para el design-time model
docker compose stop   # si el compose de la Opción A está arriba
dotnet test tests/Vicaria.IntegrationTests
```

```bash
cd frontend
npm test    # unitarios (Vitest)
npm run build   # build de producción, para chequear que compila limpio
```

---

## Datos de prueba ya cargados

Sembrados automáticamente al arrancar la API en `Development` (`Program.cs`, `SeedTestUsers` + `SeedDemoData`) — idempotente, no duplica si ya existen.

### Usuarios (contraseña `Test1234!` para los 4)

| Email | Rol |
|---|---|
| `referente@test.com` | Referente |
| `directora@test.com` | Directora de Casona *(nombre del rol pendiente de decisión de equipo, ver `.ai/context/OPEN_QUESTIONS.md`)* |
| `escucha@test.com` | Escucha |
| `coordinador@test.com` | Coordinador de Casa de Convivencia |

### Personas de ejemplo

| Nombre | Tipo | Estado | Notas |
|---|---|---|---|
| Juan Pérez | Ambulatorio | Activo | Con contacto de referencia y 2 observaciones |
| María Gómez | Ambulatorio | Activo | Con contacto de referencia y 1 observación |
| Carlos Rodríguez | Residente | Activo | Estadía abierta en la Casa de Convivencia, evaluación psiquiátrica vigente, 1 observación |
| Lucía Fernández | Residente | Activo | Estadía abierta en la Casa de Convivencia, evaluación psiquiátrica vigente, 1 observación |
| Roberto Sánchez | Residente | Inactivo | Estadía cerrada (alta del equipo) |
| Ana Torres | Ambulatorio | Inactivo | Dejó de asistir |

**Importante si se reinicia la app y las personas "activas" aparecen como inactivas:** hay un job en background (`AttendanceInactivityBackgroundService`, SCRUM-135) que pasa a Inactivo cualquier ficha Activa sin un registro de `Attendance` en los últimos 30 días — el seed ya crea asistencias recientes para Juan/María/Carlos/Lucía para evitar esto, pero si se edita el seed o pasa mucho tiempo sin reiniciar, puede volver a dispararse.

---

## Problemas conocidos (no bloquean, pero conviene saberlos)

- **Dos jobs de inactividad con lógica solapada** (`PersonInactivityService` y `AttendanceInactivityService`) — ver `.ai/context/KNOWN_ISSUES.md`.
- **CI no configurado** — los merges no corren build/tests automáticos todavía.
- Ver `.ai/context/KNOWN_ISSUES.md` y `.ai/context/OPEN_QUESTIONS.md` para el resto del estado real verificado.
