# QA Sprint 3 — Calendario, Colaboradores y Visitas (EP-04 / EP-05)

Fecha: 2026-10-08. Base: `dev-backend` @ `58fee98`. Alcance: **API** (el frontend de Belén aún apunta a rutas asumidas, ver `Pendientes`).
Roles: **Ref** = Referente, **Dir** = Directora de Casa de Convivencia, **Esc** = Escucha, **Coo** = Coordinador de Casa de Convivencia.
Cada caso cita el test automatizado de integración que lo respalda (`tests/Vicaria.IntegrationTests/...`).

## SCRUM-188 — QA calendario general (historia SCRUM-15)
| TC | Caso | Pasos | Esperado | Respaldo automatizado |
|---|---|---|---|---|
| TC-41 | Listar eventos por rango | `GET /api/general-calendar-events?from=2026-10-12&to=2026-10-18` con cada rol | 200; ocurrencias recurrentes expandidas, paginado | `Get_WithRange_ExpandsSeedTemplatesAndPaginates`, `Get_WithEachAuthorizedRole_Returns200` |
| TC-42 | Sin rango usa la semana actual | `GET` sin `from`/`to` | 200 con forma paginada | `Get_WithoutRange_Returns200WithPagedShape` |
| TC-43 | Rango inválido | `from` posterior a `to` | 400 "El parámetro 'from' no puede ser posterior a 'to'." | `Get_WithFromAfterTo_Returns400` |
| TC-44 | Detalle de evento | `GET /api/general-calendar-events/{id}` existente / inexistente | 200 / 404 "El evento especificado no existe." | `GetById_KnownSeedTemplate_Returns200`, `GetById_Unknown_Returns404` |
| TC-45 | Sin sesión | `GET` sin token | 401 | `Get_WithoutToken_Returns401` |

## SCRUM-193 — QA creación de eventos (historia SCRUM-16)
| TC | Caso | Pasos | Esperado | Respaldo |
|---|---|---|---|---|
| TC-46 | Crear evento (Referente) | `POST /api/general-calendar-events` `{title, date, startTime, endTime}` | 201 con `id` | `Post_WithReferentRole_Returns201WithId` |
| TC-47 | Crear con rol no permitido | Mismo POST con Dir / Esc / Coo | 403 | `Post_WithNonReferentRoles_Returns403` |
| TC-48 | Sin sesión | POST sin token | 401 | `Post_WithoutToken_Returns401` |
| TC-49 | Título obligatorio | `title` vacío | 400 "El título del evento es obligatorio." | `Post_WithMissingTitle_Returns400` |
| TC-50 | Hora fin ≤ inicio | `endTime <= startTime` | 400 "La hora de fin debe ser posterior a la hora de inicio." | `Post_WithEndTimeNotAfterStartTime_Returns400` |
| TC-51 | Evento recurrente | POST con `recurrenceDays` (flags de día) y listar el rango | Aparecen ocurrencias en cada día marcado | `CalendarEventOccurrenceExpanderTests` (unit) |

Bloqueado (backend pendiente): edición y eliminación con auditoría → SCRUM-190.

## SCRUM-198 — QA calendario personal (historia SCRUM-17)
| TC | Caso | Pasos | Esperado | Respaldo |
|---|---|---|---|---|
| TC-52 | Solo veo mis eventos | `GET /api/personal-calendar-events` con dos usuarios distintos | Cada uno ve únicamente los suyos | `Get_ReturnsOnlyEventsOwnedByCaller` |
| TC-53 | Evento ajeno = inexistente | `GET /{id}` de evento de otro usuario | 404 (no revela existencia) | `GetById_OtherUsersEvent_Returns404` |
| TC-54 | Detalle propio | `GET /{id}` propio | 200 | `GetById_OwnEvent_Returns200` |
| TC-55 | Roles / sesión / rango | Rol autorizado, sin token, `from>to` | 200 / 401 / 400 | `Get_WithEachAuthorizedRole_Returns200`, `Get_WithoutToken_Returns401`, `Get_WithFromAfterTo_Returns400` |

Bloqueado: **alta** de evento personal (no existe `POST` en `PersonalCalendarEventsController` ni subtarea en Jira), conversión personal→general (SCRUM-195).

## SCRUM-203 — QA alta de colaborador (historia SCRUM-18)
| TC | Caso | Pasos | Esperado | Respaldo |
|---|---|---|---|---|
| TC-56 | Alta exitosa | `POST /api/collaborators` con Ref | 201 con `id` | `Post_WithReferente_Returns201WithId` |
| TC-57 | Alta con solo nombre | Campos opcionales nulos | 201 | `Post_WithNullOptionalFields_Returns201` |
| TC-58 | Rol no permitido | POST con Dir / Esc / Coo | 403 | `Post_WithUnauthorizedRole_Returns403` |
| TC-59 | Sin sesión | POST sin token | 401 | `Post_WithoutToken_Returns401` |
| TC-60 | Nombre obligatorio | `firstName` vacío | 400 "El nombre es obligatorio." | `Post_WithMissingFirstName_Returns400` |
| TC-61 | Nombre muy largo | >100 caracteres | 400 | `Post_WithTooLongFirstName_Returns400` |
| TC-62 | Email inválido | `email: "abc"` | 400 "El email no tiene un formato válido." | `Post_WithInvalidEmail_Returns400` |
| TC-63 | DNI duplicado | Dos altas con igual DNI | 409 | `Post_WithDuplicateDni_Returns409` |
| TC-64 | Auditoría y autor | Alta y revisar BD | Se guarda el actor y se escribe `AuditLog` | `Post_PersistsCollaboratorWithActorAndWritesAuditLog` |

Bloqueado: edición, baja lógica y auditoría de cambios → SCRUM-200.

## SCRUM-208 — QA búsqueda de colaboradores (historia SCRUM-19)
| TC | Caso | Pasos | Esperado | Respaldo |
|---|---|---|---|---|
| TC-65 | Buscar por nombre | `GET /api/collaborators/search?q=ana` | 200 con coincidencias | `Search_WithToken_Returns200WithMatch` |
| TC-66 | Ignora tildes y mayúsculas | `q=JOSE` vs "José" | Encuentra | `Search_IgnoresAccentsAndCase`, `Search_ByLastNameWithoutAccents_FindsCollaborator` |
| TC-67 | Buscar por área | `q=cocina` | Encuentra por `workArea` | `Search_ByWorkArea_FindsCollaborator` |
| TC-68 | Filtro por tipo | `type=Volunteer` / `Employee` | Solo ese tipo | `Search_WithTypeFilter_ReturnsOnlyThatType` |
| TC-69 | Sin resultados / vacío | `q` vacío o nulo | 200 lista vacía | `Search_WithEmptyOrNullQuery_Returns200WithEmptyList` |
| TC-70 | Entrada inválida | `q` >100 chars; `type` inválido | 400 con mensaje en español | `Search_WithTooLongQuery_Returns400WithSpanishMessage`, `Search_WithInvalidTypeValue_Returns400` |
| TC-71 | Comodín `%` literal | `q=%` | Solo coincide con `%` literal | `Search_WithPercentWildcard_MatchesOnlyLiteralPercent` |
| TC-72 | Sin sesión | GET sin token | 401 | `Search_WithoutToken_Returns401` |

Bloqueado: detalle de colaborador (SCRUM-205); listado completo (el frontend usa `GET /api/collaborators`, que no existe).

## SCRUM-215 — QA calendario de visitas (historia SCRUM-74)
| TC | Caso | Pasos | Esperado | Respaldo |
|---|---|---|---|---|
| TC-73 | Alta de visita (Ref/Dir/Coo) | `POST /api/casona-visits` `{personId, visitorName, date, startTime, estimatedDurationMinutes}` | 201; queda Pendiente | `Post_WithEachAuthorizedRole_Returns201WithId` |
| TC-74 | Escucha no puede escribir | POST / PUT con Esc | 403 | `Post_WithListenerRole_Returns403`, `Put_WithListenerRole_Returns403` |
| TC-75 | Validaciones de alta | Campos faltantes, fecha pasada, duración 0 | 400 con mensajes en español | `Post_WithMissingFields_Returns400`, `Post_WithPastDate_Returns400`, `Post_WithZeroDuration_Returns400` |
| TC-76 | Persona inválida | Persona inexistente / ambulatoria / sin estadía abierta | 400 | `Post_WithUnknownPerson_Returns400`, `Post_WithAmbulatoryPerson_Returns400`, `Post_WithoutOpenStay_Returns400` |
| TC-77 | Solapamiento de horario | Dos visitas pisadas | 409 | `Post_OverlappingVisit_Returns409` |
| TC-78 | Editar visita Pendiente | `PUT /api/casona-visits/{id}` | 204 | `Put_OnPendingVisit_Returns204` |
| TC-79 | Editar visita cerrada | PUT sobre Realizada / Cancelada | 409 | `Put_OnCompletedVisit_Returns409` |
| TC-80 | Cancelar sin motivo | PUT estado Cancelada sin motivo | 400 | `Put_CancelledWithoutReason_Returns400` |
| TC-81 | PUT inválidos | Estado inválido, mover al pasado, id inexistente | 400 / 400 / 404 | `Put_WithInvalidStatusValue_Returns400`, `Put_MovingVisitToPast_Returns400`, `Put_UnknownVisit_Returns404` |
| TC-82 | Consulta por rango | `GET /api/casona-visits?from&to` con los 4 roles | 200 paginado, solo visitas del rango | `Get_ReturnsOnlyVisitsInRangeWithPagedShape`, `Get_WithEachAuthorizedRole_Returns200` |
| TC-83 | Sin sesión / rango inválido | Sin token; `from>to` | 401 / 400 | `Get_WithoutToken_Returns401`, `Get_WithFromAfterTo_Returns400` |

Bloqueado: cambio de estado dedicado y alerta de solapamiento (SCRUM-211), permisos por rol (SCRUM-212), `GET /residentes` que usa el frontend.

## Pendientes y desajustes a comunicar
1. **Frontend ↔ backend:** `dev-frontend-belen` llama a `/api/eventos`, `/api/casa-convivencia-visitas` y `GET /api/collaborators`; el backend real expone `/api/general-calendar-events`, `/api/personal-calendar-events`, `/api/casona-visits` y `/api/collaborators/search`. La rama además tiene 25 conflictos con `dev-frontend` actual.
2. **Alta de evento personal:** no hay endpoint ni subtarea en Jira (el frontend lo usa en SCRUM-197).
3. **Endpoints que faltan:** edición/eliminación de evento (SCRUM-190), conversión (SCRUM-195), edición/baja de colaborador (SCRUM-200), detalle (SCRUM-205), estado de visita + solapamiento (SCRUM-211), permisos (SCRUM-212).
4. **Jira SCRUM-204** figura "Por hacer" pero su PR #80 ya está mergeado.
5. Estos casos son de nivel API. El QA de UI queda para cuando el frontend apunte a las rutas reales.
