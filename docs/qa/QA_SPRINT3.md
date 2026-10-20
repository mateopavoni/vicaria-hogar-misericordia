# QA Sprint 3 — Calendario, Colaboradores y Visitas (EP-04 / EP-05)

Fecha: 2026-10-08 (actualizado 2026-10-09 con el backend de Emir integrado). Base: `dev-backend` @ `f2d2875` + ajustes propios en `dev`. Alcance: **API** (TC-41 a TC-83, TC-100 a TC-129) y **UI** en navegador real (TC-84 a TC-99 y TC-130 a TC-136).
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

### Edición y eliminación de eventos generales (SCRUM-190)
| TC | Caso | Pasos | Esperado | Respaldo |
|---|---|---|---|---|
| TC-100 | Editar evento (Referente) | `PUT /api/general-calendar-events/{id}` con nuevos datos | 204; el detalle refleja el cambio; `AuditLog` "Evento general modificado" | `Put_ByReferent_Returns204AndChangesDetail`, `UpdateAsync_WhenAuthorUpdates_SucceedsAndLogsAudit` |
| TC-101 | Referente edita evento ajeno | PUT de un Referente sobre evento de otro autor | 204 | `UpdateAsync_WhenReferentUpdatesOtherUsersEvent_SucceedsAndLogsAudit` |
| TC-102 | Sin permiso sobre evento ajeno | PUT / DELETE de un no-autor y no-Referente | 403 | `Put_ByNonAuthorNonReferent_Returns403`, `Delete_ByNonAuthorNonReferent_Returns403` |
| TC-103 | Título vacío al editar | PUT con `title` vacío | 400 | `Put_WithEmptyTitle_Returns400` |
| TC-104 | Evento inexistente | PUT / DELETE con id desconocido | 404 | `Put_UnknownEvent_Returns404`, `Delete_UnknownEvent_Returns404` |
| TC-105 | Eliminar evento | `DELETE /api/general-calendar-events/{id}` | 204; el detalle pasa a 404; `AuditLog` "Evento general eliminado" | `Delete_ByReferent_Returns204ThenDetailIs404`, `DeleteAsync_WhenAuthorDeletes_SucceedsRemovesEntityAndLogsAudit` |

## SCRUM-198 — QA calendario personal (historia SCRUM-17)
| TC | Caso | Pasos | Esperado | Respaldo |
|---|---|---|---|---|
| TC-52 | Solo veo mis eventos | `GET /api/personal-calendar-events` con dos usuarios distintos | Cada uno ve únicamente los suyos | `Get_ReturnsOnlyEventsOwnedByCaller` |
| TC-53 | Evento ajeno = inexistente | `GET /{id}` de evento de otro usuario | 404 (no revela existencia) | `GetById_OtherUsersEvent_Returns404` |
| TC-54 | Detalle propio | `GET /{id}` propio | 200 | `GetById_OwnEvent_Returns200` |
| TC-55 | Roles / sesión / rango | Rol autorizado, sin token, `from>to` | 200 / 401 / 400 | `Get_WithEachAuthorizedRole_Returns200`, `Get_WithoutToken_Returns401`, `Get_WithFromAfterTo_Returns400` |

### Recurrencia mensual y series recurrentes (SCRUM-189)
| TC | Caso | Pasos | Esperado | Respaldo |
|---|---|---|---|---|
| TC-137 | Evento mensual | `POST /api/general-calendar-events` con `repeatsMonthly: true` y consultar el mismo día de meses posteriores | Aparece el mismo día de cada mes; no aparece otros días ni antes de su fecha | `Post_MonthlyEvent_AppearsOnSameDayOfLaterMonths`, `Expand_MonthlyEvent_ReturnsSameDayEachMonth` |
| TC-138 | Mes más corto | Evento mensual del día 31 | Cae el último día de los meses de 30 días y de febrero (28 o 29) | `Expand_MonthlyEventOnDay31_FallsOnLastDayOfShorterMonths`, `Expand_MonthlyEventOnDay31_UsesLeapDayInLeapYear` |
| TC-139 | Serie iniciada antes del rango | Evento semanal creado en octubre, consultar una semana de diciembre | Se muestra (antes solo aparecía en su semana de inicio) | `Post_WeeklyEventStartedBeforeRange_AppearsInLaterWeeks`, `Expand_MonthlyEventStartingBeforeRange_ShowsOccurrenceInsideRange` |
| TC-140 | Mensual combinado con días | `repeatsMonthly: true` y `recurrenceDays` con valor | 400 | `Post_MonthlyCombinedWithWeekDays_Returns400` |
| TC-141 | Pasar un evento a mensual | `PUT` con `repeatsMonthly: true` | 204; el detalle informa `repeatsMonthly` | `Put_TurnsEventIntoMonthly_AndDetailReportsIt` |

### Alta, edición, baja y conversión de eventos personales (SCRUM-195 y SCRUM-17)
| TC | Caso | Pasos | Esperado | Respaldo |
|---|---|---|---|---|
| TC-106 | Alta de evento personal | `POST /api/personal-calendar-events` con Ref | 201; solo el dueño lo ve | `Post_WithReferent_Returns201AndOnlyOwnerSeesIt` |
| TC-107 | Alta solo Referente | POST con Dir / Esc / Coo; sin token; sin título | 403 / 401 / 400 | `Post_WithNonReferentRoles_Returns403`, `Post_WithoutToken_Returns401`, `Post_WithMissingTitle_Returns400` |
| TC-108 | Editar evento propio | `PUT /api/personal-calendar-events/{id}` | 204; `AuditLog` "Evento personal modificado" | `Put_ByOwnerReferent_Returns204AndChangesTitle`, `UpdateAsync_WhenAuthor_UpdatesFieldsAndLogsAudit` |
| TC-109 | Editar o borrar evento ajeno | PUT / DELETE de otro usuario | 404 (no revela existencia) | `Put_OnOtherUsersEvent_Returns404`, `Delete_OnOtherUsersEvent_Returns404`, `UpdateAsync_WhenNotAuthor_ReturnsNotFoundAndKeepsEvent` |
| TC-110 | Validación al editar | PUT con título vacío | 400 | `Put_WithEmptyTitle_Returns400` |
| TC-111 | Eliminar evento propio | `DELETE /api/personal-calendar-events/{id}` | 204; detalle 404; `AuditLog` "Evento personal eliminado" | `Delete_ByOwnerReferent_Returns204ThenDetailIs404`, `DeleteAsync_WhenAuthor_RemovesEventAndLogsAudit` |
| TC-112 | Borrar con rol no permitido | DELETE con Esc / Dir | 403 | `Delete_WithNonReferentRoles_Returns403` |
| TC-113 | Convertir personal a general | `PUT /api/personal-calendar-events/{id}/publish` | 200 con `id` del evento general; el personal desaparece; `AuditLog` | `Publish_ByOwner_Returns200MovesEventToGeneral`, `PublishAsync_WhenAuthorPublishes_MovesEventToGeneralAndLogsAudit` |
| TC-114 | Convertir evento ajeno o inexistente | PUT /publish de otro usuario / id desconocido | 404 | `Publish_OnOtherUsersEvent_Returns404`, `PublishAsync_WhenEventBelongsToAnotherUser_ReturnsNotFoundAndDoesNotPublish` |

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

### Edición y baja lógica de colaboradores (SCRUM-200)
| TC | Caso | Pasos | Esperado | Respaldo |
|---|---|---|---|---|
| TC-115 | Editar colaborador | `PUT /api/collaborators/{id}` con Ref | 204; el detalle refleja los cambios; `AuditLog` "Colaborador modificado" | `Put_ByReferent_Returns204AndDetailReflectsChanges`, `UpdateAsync_WithValidDto_UpdatesFieldsAndTrimsValues` |
| TC-116 | Baja lógica | PUT con `isActive: false` | 204; el listado lo marca inactivo; `AuditLog` "Colaborador dado de baja" | `Put_WithIsActiveFalse_DeactivatesAndListShowsIt`, `UpdateAsync_WhenDeactivating_WritesDeactivatedAuditLog` |
| TC-117 | Reactivación | PUT con `isActive: true` sobre uno inactivo | 204; `AuditLog` "Colaborador reactivado" | `UpdateAsync_WhenReactivating_WritesReactivatedAuditLog` |
| TC-118 | DNI duplicado al editar | PUT con DNI de otro colaborador | 409; conservar el propio DNI es válido | `Put_WithDuplicateDni_Returns409`, `UpdateAsync_WhenKeepingSameDni_Succeeds` |
| TC-119 | Validaciones al editar | `firstName` vacío; id inexistente | 400 / 404 | `Put_WithEmptyFirstName_Returns400`, `Put_UnknownCollaborator_Returns404` |
| TC-120 | Edición solo Referente | PUT con Dir / Esc / Coo | 403 | `Put_WithUnauthorizedRole_Returns403` |

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

### Detalle y listado de colaboradores (SCRUM-205)
| TC | Caso | Pasos | Esperado | Respaldo |
|---|---|---|---|---|
| TC-121 | Detalle de colaborador | `GET /api/collaborators/{id}` con cualquier rol autenticado | 200 con datos completos, estado y quién lo registró | `GetById_WithAnyAuthenticatedRole_Returns200WithRegistrar`, `GetByIdAsync_WhenCollaboratorExists_ReturnsAllDetailsAndRegistrarName` |
| TC-122 | Detalle inexistente / sin sesión | id desconocido; sin token | 404 / 401 | `GetById_Unknown_Returns404`, `GetById_WithoutToken_Returns401` |
| TC-123 | Listado completo | `GET /api/collaborators` | 200 ordenado por nombre, con `isActive` y quién lo registró | `List_ReturnsCollaboratorsWithRegisteredByName`, `List_WithoutToken_Returns401` |

## SCRUM-215 — QA calendario de visitas (historia SCRUM-74)
| TC | Caso | Pasos | Esperado | Respaldo |
|---|---|---|---|---|
| TC-73 | Alta de visita (Ref/Dir/Coo) | `POST /api/casona-visits` `{personId, visitorName, date, startTime, estimatedDurationMinutes}` | 201; queda Pendiente | `Post_WithEachAuthorizedRole_Returns201WithId` |
| TC-74 | Escucha no puede escribir | POST / PUT con Esc | 403 | `Post_WithListenerRole_Returns403`, `Put_WithListenerRole_Returns403` |
| TC-75 | Validaciones de alta | Campos faltantes, fecha pasada, duración 0 | 400 con mensajes en español | `Post_WithMissingFields_Returns400`, `Post_WithPastDate_Returns400`, `Post_WithZeroDuration_Returns400` |
| TC-76 | Persona inválida | Persona inexistente / ambulatoria / sin estadía abierta | 400 | `Post_WithUnknownPerson_Returns400`, `Post_WithAmbulatoryPerson_Returns400`, `Post_WithoutOpenStay_Returns400` |
| TC-77 | Solapamiento de horario (advertencia) | Dos visitas pisadas, sin confirmar | 409 con el aviso | `Post_OverlappingVisit_Returns409` |
| TC-78 | Editar visita Pendiente | `PUT /api/casona-visits/{id}` | 204 | `Put_OnPendingVisit_Returns204` |
| TC-79 | Editar visita cerrada | PUT sobre Realizada / Cancelada | 409 | `Put_OnCompletedVisit_Returns409` |
| TC-80 | Cancelar sin motivo | PUT estado Cancelada sin motivo | 204 (el motivo es opcional, decisión del 2026-10-09) | `Put_CancelledWithoutReason_Returns204` |
| TC-81 | PUT inválidos | Estado inválido, mover al pasado, id inexistente | 400 / 400 / 404 | `Put_WithInvalidStatusValue_Returns400`, `Put_MovingVisitToPast_Returns400`, `Put_UnknownVisit_Returns404` |
| TC-82 | Consulta por rango | `GET /api/casona-visits?from&to` con los 4 roles | 200 paginado, solo visitas del rango | `Get_ReturnsOnlyVisitsInRangeWithPagedShape`, `Get_WithEachAuthorizedRole_Returns200` |
| TC-83 | Sin sesión / rango inválido | Sin token; `from>to` | 401 / 400 | `Get_WithoutToken_Returns401`, `Get_WithFromAfterTo_Returns400` |

### Cambio de estado, solapamiento y permisos (SCRUM-211 y SCRUM-212)
| TC | Caso | Pasos | Esperado | Respaldo |
|---|---|---|---|---|
| TC-124 | Marcar visita realizada | `PATCH /api/casona-visits/{id}/status` `{status: 1}` | 204; queda Completed; `AuditLog` "Visita marcada como realizada" | `Patch_ToCompleted_Returns204AndPersistsStatus`, `ChangeStatusAsync_WhenChangingToCompleted_UpdatesStatusAndLogsAudit` |
| TC-125 | Cancelar con motivo | PATCH `{status: 2, cancellationReason}` | 204; guarda el motivo; `AuditLog` "Visita cancelada" | `Patch_ToCancelled_StoresReason`, `ChangeStatusAsync_WhenChangingToCancelled_SavesReasonAndLogsAudit` |
| TC-126 | Estado inválido / visita inexistente | PATCH `{status: 9}`; id desconocido | 400 / 404 | `Patch_WithInvalidStatusValue_Returns400`, `Patch_UnknownVisit_Returns404`, `ChangeStatusAsync_WhenVisitNotFound_ReturnsNotFound` |
| TC-127 | Horario liberado por una cancelada | Alta en el horario de una visita Cancelada | 201 (no hay conflicto) | `CreateAsync_WhenSlotWasOccupiedByCancelledVisit_DoesNotConflict` |
| TC-128 | Escucha sin acceso a visitas | GET / PATCH con Esc | 403 | `Get_WithListenerRole_Returns403`, `Patch_WithListenerRole_Returns403`, `Controller_HasAuthorizeAttribute_RestrictedToExpectedRoles`, `GetByRange_DoesNotAllowListenerRole` |
| TC-129 | Roles con acceso | GET con Ref / Dir / Coo | 200 | `Get_WithEachAuthorizedRole_Returns200` |

### Solapamiento como advertencia y motivo opcional (decisión del 2026-10-09)
| TC | Caso | Pasos | Esperado | Respaldo |
|---|---|---|---|---|
| TC-142 | Guardar igual en el alta | `POST /api/casona-visits` solapada con `allowOverlap: true` | 201 | `Post_OverlappingVisitWithAllowOverlap_Returns201` |
| TC-143 | Guardar igual en la edición | `PUT` solapado: sin confirmar 409, con `allowOverlap: true` 204 | 409 y luego 204 | `Put_OverlappingVisitWithAllowOverlap_Returns204` |
| TC-144 | Cancelar sin motivo | `PUT` con estado Cancelada y sin motivo | 204 | `Put_CancelledWithoutReason_Returns204` |

## QA de UI — frontend integrado (2026-10-08)
El frontend de Belén se integró en `dev` apuntando a las rutas reales del backend. Se probó en un navegador real
(Playwright/Chromium) contra el stack local (`docker compose`), con los usuarios de prueba. Script reproducible:
`docs/qa/e2e/sprint3-ui-flows.mjs`.

| TC | Caso | Resultado |
|---|---|---|
| TC-84 | Calendario general: ver semana con actividades precargadas (desayuno, almuerzo, merendero) | OK |
| TC-85 | Crear evento general desde la UI (Referente) | OK |
| TC-86 | Crear evento semanal recurrente | OK |
| TC-87 | Crear evento personal en "Mi calendario" | OK |
| TC-88 | El evento personal no aparece en "General"; "Combinado" muestra ambos | OK |
| TC-89 | Detalle de evento con autor y descripción | OK |
| TC-90 | Crear colaborador desde la UI | OK |
| TC-91 | DNI duplicado: la UI informa "Ya existe un colaborador con ese DNI." | OK |
| TC-92 | Búsqueda de colaboradores por área | OK |
| TC-93 | Dar de baja un colaborador | OK (re-ejecutado 2026-10-09; el 2026-10-08 fallaba con 404 por falta de SCRUM-200) |
| TC-94 | Crear visita desde la UI (selector de residentes reales) | OK |
| TC-95 | Visita solapada: la UI avisa y el backend responde 409 | OK (ver nota 2 abajo) |
| TC-96 | Marcar visita como realizada | OK |
| TC-97 | Cancelar visita con motivo | OK |
| TC-98 | Escucha: ve el calendario, sin "Nuevo evento", sin pestaña Casa de Convivencia, sin Colaboradores, `/colaboradores` bloqueado | OK |
| TC-99 | Directora: ve el calendario sin crear eventos; puede crear visitas | OK |

Re-ejecución del 2026-10-09 con el backend de Emir integrado y las decisiones de producto aplicadas (33 verificaciones, 33 OK):

| TC | Caso | Resultado |
|---|---|---|
| TC-130 | Editar un evento general desde el detalle (PUT 204) | OK |
| TC-131 | Eliminar un evento general con confirmación (DELETE 204) | OK |
| TC-132 | Convertir un evento personal en general: sale de "Mi calendario" y aparece en "General" (PUT /publish) | OK |
| TC-133 | Editar un colaborador desde la UI (PUT 204) | OK |
| TC-134 | Dar de baja: pasa a la pestaña "Inactivos" y la baja persiste al recargar | OK |
| TC-135 | Reactivar un colaborador: vuelve a "Activos" | OK |
| TC-136 | Marcar visita realizada y cancelar con motivo vía `PATCH /status` | OK |
| TC-145 | Crear un evento mensual y verlo el mismo día del mes siguiente | OK |
| TC-146 | Solapamiento de visitas: la UI avisa (409) y al tocar Guardar de nuevo la guarda igual | OK |

## Pendientes y desajustes a comunicar
1. **Resuelto 2026-10-08:** el frontend usa `/api/general-calendar-events`, `/api/personal-calendar-events`, `/api/casona-visits` y `/api/collaborators`. Se agregó `POST api/personal-calendar-events`, `GET api/collaborators` (listado) y autor/`isPreloaded` en las ocurrencias.
2. **Resuelto 2026-10-09:** contratos reales de Emir integrados: `PUT/DELETE api/general-calendar-events/{id}`, `PUT api/personal-calendar-events/{id}/publish`, `PUT api/collaborators/{id}` (la baja es `isActive` dentro del PUT), `GET api/collaborators/{id}` y `PATCH api/casona-visits/{id}/status`. El listado de colaboradores ahora devuelve `isActive`. Se agregaron `PUT/DELETE api/personal-calendar-events/{id}` (edición y borrado de eventos propios), que no estaban en el backend.
3. **Defecto hallado en `dev-backend` (Emir):** `UpdateGeneralCalendarEventDtoValidator`, `UpdateCollaboratorDtoValidator` y `ChangeCasonaVisitStatusDtoValidator` no estaban registrados en `Program.cs`; los endpoints que los usan respondían 500. Sus tests eran todos unitarios y no lo detectaron. Corregido en `dev`; hay que llevarlo a `dev-backend`. Se agregaron tests HTTP para esos endpoints.
4. **Resuelto 2026-10-09 (decisión de producto):** el solapamiento de visitas es una advertencia. El backend responde 409 con el aviso y el alta y la edición aceptan `allowOverlap: true` para guardar igual; el frontend reenvía con ese valor cuando la persona vuelve a tocar Guardar.
5. **Resuelto 2026-10-09 (decisión de producto):** el motivo de cancelación de una visita es opcional; el `PUT` ya no lo exige y el frontend deja de mandar un texto por defecto.
6. **Resuelto 2026-10-09:** recurrencia mensual. Los eventos tienen `repeatsMonthly` (columnas `repeats_monthly`, migración `AddCalendarEventMonthlyRecurrence`): se repiten el mismo día de cada mes y, en meses más cortos, el último día. No se combina con repetición por días de la semana. Además se corrigió que las series con fecha de inicio anterior al rango consultado no se expandían. Limitación: al editar una ocurrencia de una serie, la fecha del formulario es la de esa ocurrencia, por lo que la serie pasa a empezar ese día.
7. **Escucha y visitas:** desde SCRUM-212 el rol Escucha no puede consultar visitas (403). Coincide con la UI, que le oculta la pestaña.
8. **Defecto preexistente (main):** los ítems Inicio, Asistencia, Medicación e Informes del menú apuntan a `/inicio`, una ruta sin pantalla. Prueba de humo por rol 2026-10-09: 11 de 15 pantallas OK, las 4 restantes son `/inicio`.
9. **Jira:** SCRUM-198 y SCRUM-215 (QA) siguen "En curso".
