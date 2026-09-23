# Known issues

Formato: severidad explícita, no párrafos narrativos. Severidad = impacto real dado que el sistema maneja datos de personas en situación de vulnerabilidad, no gravedad técnica en abstracto.

Actualizado al 2026-09-23 — ver [CURRENT_STATE.md](./CURRENT_STATE.md) para el detalle de qué se cerró desde la versión anterior de este archivo.

## CRÍTICA

- **Información especialmente sensible (abusos) no tiene ningún control técnico.** El acuerdo de manejarla solo verbalmente (nunca cargarla al sistema ni en papel) es puramente humano — no hay validación, campo restringido, ni política de contenido en código que lo haga cumplir. Si alguien carga esa información en un campo de texto libre (ej. `GeneralNotes` de la ficha, `Content` de una observación), el sistema la acepta sin aviso. Ver [CONSTRAINTS.md](./CONSTRAINTS.md).
- **No hay CI configurado.** Los PRs se mergean sin ningún check automático (build, tests) ni revisión obligatoria. Con datos sensibles de por medio, un merge roto o con una regresión de seguridad puede llegar a `dev`/`main` sin que nada lo frene. Sigue sin resolverse.

## ALTA

- **El rename `Hogar` → `Centro Barrial` está incompleto.** Todo el dominio `LifeStory` (entidad, DTOs, rutas `before-hogar`/`in-hogar`/`after-hogar`, columnas de tabla) sigue usando `Hogar` como identificador de código. Es un rename que toca migración de DB — deliberadamente no se hizo el 2026-09-23 para no arriesgar un demo el mismo día. **Bloqueante para pasar `dev` a `main`**, no bloqueante para usar `dev` tal cual está. Ver [CURRENT_STATE.md](./CURRENT_STATE.md).
- **Dos jobs de inactividad automática con lógica solapada y criterios distintos**, encontrado en QA manual del 2026-09-23: `PersonInactivityService` (pasa a Inactive por `UpdatedAt`/`CreatedAt` viejo) y `AttendanceInactivityService` (pasa a Inactive por falta de `Attendance` en 30 días, SCRUM-135) — ambos corren como hosted services independientes sobre el mismo campo `SocialRecord.Status`, sin coordinarse. Un residente de la Casa de Convivencia (vive ahí todos los días) puede pasar a Inactive solo por no tener un registro de `Attendance` explícito, aunque su estadía siga abierta — la lógica no distingue `PersonType`. No es un bug de código (ambos hacen lo que su propio ticket pedía, SCRUM aparte) sino un gap de diseño de producto: dos mecanismos automáticos pisándose. Ver [OPEN_QUESTIONS.md](./OPEN_QUESTIONS.md).

## MEDIA

- **Expiración de sesión no es por inactividad real**, sino por TTL fijo de JWT (60 min) + refresh token (7 días). Existe `feature/SCRUM-96-inactivity-timeout` pero no está mergeada a ninguna rama. Sigue sin resolverse — ver [OPEN_QUESTIONS.md](./OPEN_QUESTIONS.md).
- **Contradicción de versión de Angular** entre `/PROJECT.md` (16+) y el código real (`^22.1.0`) — sigue sin resolverse.
- **RBAC documentado vs. real no coincide:** `/PROJECT.md` lista 3 roles; el código tiene un 4to (`CoordinadorDeCasaConvivencia`) y además un segundo nivel de permisos granulares (`Permission`/`RolePermission`) no descrito en ningún doc de negocio.
- **`RoleNames.DirectoraDeCasona` sin renombrar**, a diferencia del resto de las referencias a "Casona" ya renombradas a "Casa de Convivencia". Es un valor persistido (rol en DB, claims de JWT, decenas de `[Authorize(Roles=...)]`), no un rename cosmético — requiere decisión de equipo antes de tocarlo. Ver [OPEN_QUESTIONS.md](./OPEN_QUESTIONS.md).

## BAJA

- **~24 branches `feature/*`/`fix/*` en remoto sin PR abierta**, con estado de merge ambiguo. Sin proceso de limpieza. Inventario en [OPEN_QUESTIONS.md](./OPEN_QUESTIONS.md), sin tocar por ahora (decisión explícita del usuario).
- **`docker-compose.yml` local tenía dos bugs que impedían levantar el stack tal cual** (arreglados el 2026-09-23, ver [CURRENT_STATE.md](./CURRENT_STATE.md)): referenciaba un `frontend/Dockerfile.local` que no existía, y el único `nginx.conf` real apuntaba `/api/` a producción en vez del contenedor local. Se agregaron variantes `.local` dedicadas — si se edita el proxy o el Dockerfile de producción, revisar si el `.local` necesita el mismo cambio (no se sincronizan solos).

## Resueltos en esta pasada (2026-09-23), dejados de referencia

- **Bug real: `ObservationService.ApplyFilters` sin `.Include(Category)`/`.Include(AuthorUser)`** — el timeline y el CSV de observaciones mostraban `categoryName`/`authorName` siempre vacíos, aunque los IDs sí estaban bien guardados. Encontrado en QA manual end-to-end (no por lectura de código), no cubierto por ningún test existente. Arreglado + test de regresión agregado.
- **`main` estaba muy por detrás de `dev-backend`/`dev-frontend`** — ya no aplica, `dev` tiene todo integrado (ver [CURRENT_STATE.md](./CURRENT_STATE.md)). `main` en sí sigue atrasado, pero ahora es una decisión pendiente de "cuándo promover", no trabajo perdido en ramas separadas.
- **Gap de paridad frontend/backend** (fichas, Casa de Convivencia) — cerrado, ambos lados tienen las mismas funcionalidades.
- Tests de integración con tokens de `Guid.NewGuid()` — resuelto (`SembrarActorAsync`).
