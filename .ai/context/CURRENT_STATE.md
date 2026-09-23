# Estado actual — foto verificada al 2026-09-18

Reemplaza la sección "Estado actual" desactualizada que tenía `/PROJECT.md` (fechada 2026-08-26, sobre una sola rama `dev`). Esta foto compara `main`, `dev-backend` y `dev-frontend` por separado, porque **no están alineadas entre sí** (ver [ARCHITECTURE.md](./ARCHITECTURE.md)).

## `main` (lo más cercano a "producción/referencia estable")

Solo tiene **EP-03** (auth/roles) completo:
- Entidades: `User`, `Role`, `Permission`, `RolePermission`, `AuditLog`, `Notification` únicamente.
- Registro, login/JWT/refresh, aprobación/rechazo de cuentas, bloqueo por 5 intentos, notificaciones internas, listado de usuarios paginado.
- **No tiene** `Person`, `SocialRecord`, `PsychiatricEvaluation` ni `CasonaStay` — es decir, todo lo de fichas y casona (abajo) todavía no llegó a `main`.

## `dev-backend` (rama más adelantada de backend)

Todo lo de `main`, más:
- **EP-01 parcial:** `Person`, `Contact`, `SocialRecord` (ficha, separada de persona) — crear, buscar, editar ficha (SCRUM-5/6/7), actualización de tipo de persona.
- **Clúster de casona (EP-12):** `PsychiatricEvaluation` con flag `IsValid`, `CasonaStay` con flujo de egreso (SCRUM-134/140/146), registro automático de ingreso (SCRUM-141).
- Recién integrado en esta sesión (PRs #25, #28, #29): fixes de QA sobre fichas/usuarios, endpoint de egreso, registro automático de ingreso.
- **No tiene:** Observación/HistoriaVida (EP-02), Asistencia/AgendaMedicamentos (EP-10/11), InformeCaritas/adjuntos-PDF (EP-07), Colaboradores (EP-05), Calendario (EP-04), EP-13.

## `dev-frontend` (rama más adelantada de frontend)

- Auth completo: login, registro, pending-approval.
- Gestión de usuarios: listado paginado, aprobar/rechazar/cambiar rol.
- Fichas: **solo alta** (`new-social-record`) — no hay pantalla de búsqueda, edición ni visualización de ficha, aunque el backend ya expone esos endpoints (SCRUM-6/7). Ver [KNOWN_ISSUES.md](./KNOWN_ISSUES.md), gap de paridad front/back.
- **No tiene ninguna pantalla de casona** (ni estadías ni egreso), aunque el backend ya lo expone.
- Recién integrado en esta sesión (PR #26): fixes de QA de fichas/gestión de usuarios.

## Qué se hizo en esta sesión (2026-09-18)

4 PRs abiertas se mergearon (merge commit, sin borrar las ramas fuente, sin tocar `dev`/`main`, por decisión explícita del usuario):

| PR | Rama fuente | Base |
|---|---|---|
| #29 | `feature/SCRUM-146-endopoint-egreso` | `dev-backend` |
| #28 | `SCRUM-141-registro-automatico-de-ingreso` | `dev-backend` |
| #25 | `fix/qa-social-records-usuarios-backend` | `dev-backend` |
| #26 | `fix/qa-social-records-usuarios` | `dev-frontend` |

Quedaron 0 PRs abiertas. No se tocaron las ~24 ramas `feature/*` restantes sin PR — inventario en [OPEN_QUESTIONS.md](./OPEN_QUESTIONS.md).

## Épicas: estado por sprint (cruza con la tabla de `/PROJECT.md`)

| Épica | Estado real |
|---|---|
| EP-03 (Sprint 1) | ✅ Completo, en `main` |
| EP-01 (fichas, Sprint 2) | 🟡 Parcial, solo en `dev-backend`/`dev-frontend`, sin llegar a `dev`/`main`; front solo tiene alta |
| EP-02 (observaciones/historia de vida, Sprint 2) | ❌ Sin empezar — ninguna entidad |
| EP-04/EP-05 (calendario/colaboradores, Sprint 3) | ❌ Sin empezar |
| EP-10/EP-11 (asistencia/medicación, Sprint 4) | ❌ Sin empezar |
| EP-12 (evaluación psiquiátrica/estado, Sprint 5) | 🟡 Clúster de casona implementado en `dev-backend`, sin frontend, sin llegar a `dev`/`main` |
| EP-07 (documentación/adjuntos PDF, Sprint 5) | ❌ Sin empezar |
| EP-13 (informes institucionales, Sprint 6) | ❌ Sin empezar |
