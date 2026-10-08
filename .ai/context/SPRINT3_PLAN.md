# Sprint 3 — Calendario (EP-04) y Colaboradores (EP-05)

Estado al 2026-10-08, cruzado con Jira (SCRUM Sprint 3: 28/09 – 07/10). Reemplaza el borrador del 06/10.
Historias: SCRUM-15, 16, 17 (calendario general/personal), 18, 19 (colaboradores), 74 (visitas Casa de Convivencia).

## Backend (rutas reales en `dev-backend`)
| Controller | Ruta | Hecho |
|---|---|---|
| `GeneralCalendarEventsController` | `api/general-calendar-events` | POST (Referente), GET por rango, GET detalle |
| `PersonalCalendarEventsController` | `api/personal-calendar-events` | GET por rango y detalle (privados del dueño) |
| `CollaboratorsController` | `api/collaborators` | POST (Referente), GET `search` |
| `CasonaVisitsController` | `api/casona-visits` | POST, PUT (visita Pendiente), GET por rango |

| Subtarea | Estado | Responsable |
|---|---|---|
| 183, 184, 185, 189, 194, 199, 209, 210 | Finalizado | Santiago / Emir |
| 204 búsqueda de colaboradores | PR #80 mergeado (Jira dice "Por hacer") | Santiago |
| 190 edición/eliminación de evento + auditoría | Pendiente | Emir |
| 195 conversión personal → general | Pendiente | Emir |
| 200 edición/baja lógica de colaborador | En curso | Emir |
| 205 detalle de colaborador | Pendiente | Emir |
| 211 cambio de estado de visita + solapamiento | Pendiente (el POST/PUT ya devuelven 409 por solapamiento) | Emir |
| 212 permisos por rol en visitas | Pendiente (hoy: Ref/Dir/Coo escriben, Esc lee) | Emir |

Alta de evento personal: agregada el 2026-10-08 (`POST api/personal-calendar-events`, solo Referente), sin subtarea en Jira.

## Frontend (Belén) — integrado en `dev` el 2026-10-08
Las pantallas de `dev-frontend-belen` (calendario, colaboradores, visitas) se trajeron a `dev` sobre la estructura actual
(su rama era previa a Sprint 1/2 y tenía 25 conflictos) y los servicios se adaptaron al backend real
(`api/general-calendar-events`, `api/personal-calendar-events`, `api/casona-visits`, `api/collaborators`).
Probado en navegador: ver `docs/qa/QA_SPRINT3.md` (TC-84 a TC-99). Falta el backend de edición/baja (ver tabla de arriba).
En el backend se agregó `POST api/personal-calendar-events`, `GET api/collaborators` y autor/`isPreloaded` en las ocurrencias.

## QA (Mateo)
SCRUM-188, 193, 198, 203, 208, 215 → casos en `docs/qa/QA_SPRINT3.md` (nivel API).
Sprint 4: SCRUM-219, 223 (asistencia), sin código todavía.

## Decisiones/preguntas abiertas
- Solapamiento de visitas: ¿advertencia o bloqueo? (frontend permite "guardar igual", backend responde 409).
- Motivo de cancelación de visita: ¿obligatorio (backend) u opcional (frontend)?
- Recurrencia mensual: no existe en el backend.
