# Open questions

Para que el equipo (Amanda, Emir, Belén, Santiago) resuelva — no se resuelven acá ni se asume una respuesta.

## (a) Versión de Angular

`/PROJECT.md` dice "Angular 16+", `dev-frontend/frontend/package.json` usa `@angular/core ^22.1.0`. ¿Cuál es la versión real objetivo? Actualizar `/PROJECT.md` una vez decidido.

## (b) Expiración de sesión por inactividad (SCRUM-96)

Hoy la sesión expira por TTL fijo (JWT 60 min + refresh 7 días), no por inactividad real. La rama `feature/SCRUM-96-inactivity-timeout` existe pero no está mergeada a ninguna parte. Dado que el sistema maneja datos de personas en situación de vulnerabilidad y puede quedar abierto en equipos compartidos del hogar: **¿es un requisito de seguridad necesario para priorizar ahora, o puede esperar a un sprint posterior?**

## (c) Framework E2E: Playwright vs Cypress

`/PROJECT.md` menciona "Jest + Cypress, si aplica" como condicional — hoy no hay ninguno de los dos implementado. ¿Se define ahora o se pospone?

## (d) Inventario de ramas `feature/*`/`fix/*` sin PR abierta (~24, no tocadas)

No resuelto, solo inventariado. Algunas parecen ya integradas (aparecen como "merged" en `git branch --merged` contra `dev-backend`/`dev-frontend`/`main`, probablemente por integración manual sin PR):

`feature/SCRUM-105-notificaciones-y-auth`, `feature/SCRUM-79-endpoint-registro`, `feature/SCRUM-97-login`, `feature/login-ui-mejoras`, `feature/SCRUM-5-formulario-ficha`.

Estas **no** muestran ancestry limpio en ninguna rama de larga vida (podrían necesitar revisión manual, no asumir que están perdidas ni que están integradas):

`dev-frontend-belen`, `docs/agents-project-md-dev-frontend`, `docs/documentacion-base-agentes-ia`, `feature/SCRUM-111-busqueda-personas`, `feature/SCRUM-112-normalizacion-acentos`, `feature/SCRUM-128-conteo-previo-personas`, `feature/SCRUM-136-auditoria-cambio-estado`, `feature/SCRUM-137-restriccion-visibilidad-rol`, `feature/SCRUM-152-backend-endpoint-cambio-estado-perfil`, `feature/SCRUM-153-backend-extension-proceso-automatico-30-dias`, `feature/SCRUM-5-crear-ficha`, `feature/SCRUM-6-buscar-ficha`, `feature/SCRUM-7-editar-ficha`, `feature/SCRUM-80-aprobacion-usuarios`, `feature/SCRUM-83-JWT-login-y-refresh-token`, `feature/SCRUM-84-account-lockout`, `feature/SCRUM-86-roles-y-guard`, `feature/SCRUM-88-rol-directora-permisos`, `feature/SCRUM-89-user-deactivate-reactivate`, `feature/SCRUM-90-Notificacion-cuenta-pendiente`, `feature/SCRUM-94-auth-login`, `feature/SCRUM-96-inactivity-timeout`, `fix/SCRUM-94-login-validator-di`, `refactor/db-migration-postgres-a-sqlserver`.

Varias de estas (ej. `SCRUM-5-crear-ficha`, `SCRUM-80`, `SCRUM-86`, `SCRUM-94`, el refactor de postgres→sqlserver) **sí tienen su PR marcada como MERGED** en GitHub — el `git branch --merged` no las detecta probablemente porque la rama siguió recibiendo commits después del merge, o el merge se resolvió distinto a un fast-forward simple. Es decir: el estado "mergeada" de GitHub y el ancestry de git no siempre coinciden acá — antes de borrar cualquiera de estas, confirmar contra el historial real, no solo contra uno de los dos indicadores.

## (e) Tabla de roles de `/PROJECT.md` incompleta

Lista 3 roles (Referente, DirectoraDeCasona, Escucha); el código (`RoleNames`) tiene un 4to, `CoordinadorDeCasaConvivencia`, más un segundo nivel de permisos granulares (`PermissionNames`) no descrito en ningún documento de negocio. ¿Se actualiza la tabla, o `CoordinadorDeCasaConvivencia` es un rol interno/técnico que no debería estar en la tabla de negocio?

## (f) ¿El clúster de casona es EP-12 o merece épica propia?

En esta pasada se documentó como parte de EP-12 por instrucción directa de Mateo al pedir este trabajo. Si el equipo prefiere una épica separada (tiene bastante entidad propia: estadías + evaluación psiquiátrica + egreso), es una corrección simple a `/PROJECT.md` y a [CURRENT_STATE.md](./CURRENT_STATE.md).

## (g) Dos jobs de inactividad automática pisándose (encontrado 2026-09-23)

`PersonInactivityService` y `AttendanceInactivityService` corren en paralelo sobre el mismo `SocialRecord.Status`, con criterios distintos (staleness de `UpdatedAt` vs. falta de `Attendance`). Ninguno distingue `PersonType` — un Residente de la Casa de Convivencia (presente físicamente todos los días) puede pasar a Inactive solo por no tener un registro de asistencia explícito. Preguntas para el equipo: **¿se unifican en un solo job?**, **¿un Residente con estadía abierta debería quedar exento del chequeo de asistencia?** Ver [KNOWN_ISSUES.md](./KNOWN_ISSUES.md).

## (h) Rename `Hogar` → `Centro Barrial` en `LifeStory`: ¿cuándo?

Confirmado por el equipo que corresponde (mismo criterio que "Casona" → "Casa de Convivencia", ya aplicado en el resto del código). Queda pendiente específicamente en el dominio `LifeStory` (entidad, DTOs, rutas, columnas de DB) porque es un rename que toca migración, no solo texto — no se hizo el 2026-09-23 para no arriesgar un demo el mismo día. ¿Se hace antes de pasar `dev` a `main` (como el resto de la revisión de convenciones), o se puede posponer a un sprint dedicado?
