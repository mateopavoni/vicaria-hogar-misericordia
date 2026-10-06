# Plan Sprint 3 (EP-04 Calendario + EP-05 Colaboradores)

Borrador del 2026-10-06, armado **solo desde el repo** (`PROJECT.md`, `AGENTS.md`). No se pudo leer Jira (extensión de Chrome desconectada), así que:

- **Pendiente de cruzar con Jira:** historias reales, IDs SCRUM, criterios de aceptación y qué tareas tiene asignadas Mateo Pavoni. Nada de esto está inventado acá.
- Las entidades y épicas sí vienen de `PROJECT.md` (tabla de entidades, filas 16-18).

## Punto de partida

Sprint 1 y 2 están en `main` (CI verde). Entidades pendientes según `PROJECT.md`:

| # | Entidad | Épica |
|---|---|---|
| 16 | `PersonalCalendarEvent` | EP-04 |
| 17 | `GeneralCalendarEvent` | EP-04 |
| 18 | `Collaborator` | EP-05 |

Estas dos épicas no dependen una de otra, así que pueden avanzar en paralelo.

## EP-04 — Calendario compartido y personal

Orden sugerido (cada paso entrega algo testeable):

1. **Domain/Infra:** `GeneralCalendarEvent` y `PersonalCalendarEvent` + `*Configuration` + migración (`AddCalendarEvents`). Recordar que el destino final es SQL Server.
2. **Application:** DTOs `record`, validadores FluentValidation (mensajes en español), `Result` con enum de errores, `ICalendarService`.
3. **Api:** `CalendarController` (`api/calendar`). Permisos con `RoleNames`/`PermissionNames`, nunca strings sueltos. `AuditLog` en crear/editar/borrar eventos generales.
4. **Tests:** unitarios (InMemory) + integración siguiendo `AuthControllerTests`.
5. **Frontend:** vista de calendario (mes/semana), alta/edición, distinción visual evento general vs personal, `permissionGuard` en la ruta.

Regla a respetar: el evento personal es privado de su dueño (el servicio filtra por el `userId` del token; ningún otro rol lo ve).

## EP-05 — Gestión de colaboradores

1. **Domain/Infra:** `Collaborator` (voluntario/empleado) + configuración + migración (`AddCollaborator`).
2. **Application/Api:** CRUD con baja lógica, `CollaboratorsController` (`api/collaborators`), validadores, `AuditLog`.
3. **Tests** unitarios + integración.
4. **Frontend:** listado con búsqueda/filtro, formulario, baja/reactivación.

## Preguntas de producto (no resolver por cuenta propia; ver `AGENTS.md` regla 7)

- ¿Qué roles pueden crear/editar eventos **generales** del calendario? ¿Todos los roles pueden verlos?
- ¿Los eventos admiten recurrencia, recordatorios o notificaciones internas?
- ¿Qué datos exactos lleva un `Collaborator` (documento, contacto, tipo, horarios)? Ojo con datos sensibles, ver `CONSTRAINTS.md`.
- ¿Quién gestiona colaboradores: solo Referente o también Directora/Coordinador?
- ¿Se vincula un colaborador con un `User` del sistema o son registros independientes?

## Qué se puede adelantar ya (sin esperar respuestas)

- Esqueleto de entidades + migraciones + tests de configuración (pasos 1 de ambas épicas).
- Estructura de módulos y rutas del frontend (feature `calendar` y `collaborators`) con guards.

## Para completar mañana

1. Abrir el backlog de Jira, anotar las historias del Sprint 3 y cuáles están asignadas a Mateo.
2. Contrastar con este borrador y ajustar IDs SCRUM y criterios de aceptación.
3. Resolver las preguntas de arriba con el equipo antes de implementar permisos.
