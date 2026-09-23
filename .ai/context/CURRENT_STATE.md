# Estado actual — foto verificada al 2026-09-23

Reemplaza la versión anterior (fechada 2026-09-18). Entre esa fecha y hoy se cerró el Sprint 2 completo (backend y frontend), se mergeó `dev-backend` + `dev-frontend` → `dev`, y se hizo QA manual end-to-end con datos sembrados. Ver [DECISIONS.md](./DECISIONS.md) para el detalle de qué se mergeó en cada tanda.

## `dev` (rama de integración — la que se demuestra/deploya)

Desde hoy tiene **todo** lo de `dev-backend` y `dev-frontend` fusionado (merge manual vía rama `dev-integration`, sin conflictos de negocio sin resolver). Es la rama que refleja el estado real más avanzado del proyecto.

- **EP-03 (auth/roles):** completo. Registro, login/JWT/refresh, aprobación/rechazo, bloqueo por 5 intentos, notificaciones internas, listado de usuarios paginado.
- **EP-01 (fichas):** completo, back y front. Alta, búsqueda con filtros combinables (`GET /api/social-records/list`), edición, perfil completo (`GET /api/social-records/{id}`), selector/historial de tipo de persona (Ambulatorio/Residente).
- **Clúster Casa de Convivencia (EP-12):** completo, back y front. Evaluación psiquiátrica (`IsValid`), estadías con ingreso automático y flujo de egreso (motivo + auditoría), timeline de estadías en el perfil.
- **EP-02 (observaciones/historia de vida):** completo, back y front. Alta de observación, timeline con filtros, exportación a CSV, categorías de observación (CRUD, 6 categorías predefinidas sembradas), historia de vida por etapas (antes/durante/después).
- **Control manual de estado activo/inactivo** (SCRUM-156): UI + endpoint, además de los jobs automáticos (ver [KNOWN_ISSUES.md](./KNOWN_ISSUES.md) sobre la duplicación de esos jobs).
- **Timeline unificado de perfil** (SCRUM-159): combina observaciones + hitos de estadía, un solo endpoint (`GET /api/persons/{id}/timeline`).

**No tiene todavía:** Asistencia/AgendaMedicamentos más allá del job de 30 días (EP-10/11 parcial — existe `Attendance` y el job, no la agenda de medicamentos), InformeCaritas/adjuntos-PDF (EP-07), Colaboradores (EP-05), Calendario (EP-04), EP-13.

## `main`

Sin tocar en esta sesión — sigue solo con EP-03. El pase de `dev` → `main` es un paso posterior, condicionado a que el renombre pendiente (ver abajo) y la revisión de convenciones estén cerrados primero.

## Renombre de terminología (en curso, no completo)

Decisión de equipo (confirmada): "Casona" → "Casa de Convivencia", "Hogar" → "Centro Barrial", personas que asisten (no residen) → "Ambulatorio".

- ✅ **Completo:** entidades, DTOs, rutas, tablas y comentarios de "Casona" → "Casa de Convivencia" (`CasaConvivenciaStay`, `api/casa-convivencia-stays`, migración `RenameCasonaToCasaConvivencia`). `PersonType.Ambulatory` ya nace con el nombre correcto en inglés (no hubo que renombrarlo). UI del topbar ("Sede actual: Centro Barrial").
- ❌ **Pendiente:** el dominio `LifeStory` completo usa `Hogar` como identificador de las 3 etapas (`BeforeHogar`/`InHogar`/`AfterHogar`, DTOs, rutas `before-hogar`/`in-hogar`/`after-hogar`, columnas de tabla). Es un rename profundo (toca migración de DB, no solo código) — deliberadamente **no** se tocó en esta sesión para no arriesgar el demo del día; queda como tarea explícita antes de pasar `dev` a `main`.
- ❌ **Sin decidir:** `RoleNames.DirectoraDeCasona` — es un valor persistido (rol en DB, claims de JWT, decenas de `[Authorize(Roles=...)]`). Ver [OPEN_QUESTIONS.md](./OPEN_QUESTIONS.md), no renombrar sin decisión de equipo.

## QA manual end-to-end (2026-09-23)

Verificado levantando el stack completo con `docker compose` (ver `/deploy-local.md`) contra datos sembrados (`Program.cs`, `SeedTestUsers` + `SeedDemoData`, 6 personas de ejemplo con observaciones/estadías/evaluaciones):

- Login con los 4 roles, listado y detalle de fichas, observaciones con nombre de categoría/autor, estadías activas/cerradas — todo verificado con requests reales contra la API containerizada, no solo por lectura de código.
- **Bug encontrado y arreglado en esta pasada:** `ObservationService.ApplyFilters` no traía `Include(Category)`/`Include(AuthorUser)` — el timeline y el CSV de observaciones mostraban categoría y autor siempre vacíos. Arreglado + test de regresión agregado (`ObservationServiceTests`).
- **Bug encontrado y arreglado:** el `docker-compose.yml` local apuntaba el frontend a `frontend/Dockerfile.local` (no existía) y el único `nginx.conf` real proxeaba `/api/` a producción — se hubiera usado el backend de producción en vez del local sin ningún error visible. Se crearon `frontend/Dockerfile.local` + `frontend/nginx.local.conf` dedicados a compose.
- **Bug encontrado y arreglado:** `ng serve` no aplicaba `proxy.conf.json` (faltaba `options.proxyConfig` en `angular.json`) — cualquiera que corriera `npm start` sin saber del flag manual se encontraba con llamadas a la API fallando en dev sin compose.

Backend: `dotnet build` limpio, 113/113 tests unitarios en verde. Frontend: `ng build` de producción limpio.

## Merge `dev-backend` + `dev-frontend` → `dev`: nota técnica importante

El merge de `origin/dev-backend` a la rama de integración **borró silenciosamente los ~101 archivos de `frontend/`** que ya existían en `origin/dev` (falso positivo de detección de renombre de git al mergear una rama backend-only que diverge de `dev` desde antes de que `frontend/` existiera) — sin marcarlo como conflicto. Se detectó porque `npm install` fallaba con `package.json` no encontrado, y se confirmó comparando árboles de archivos completos (`git ls-tree -r`) entre ramas, no solo el archivo que falló. Restaurado completo desde `origin/dev-frontend`. **Lección para el equipo:** después de cualquier merge de ramas con superficies de archivos muy distintas (backend-only + frontend-only), comparar conteos de archivos por carpeta antes de confiar en que "no hubo conflictos" significa "no se perdió nada".
