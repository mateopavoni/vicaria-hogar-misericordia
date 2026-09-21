# Arquitectura

## Capas del backend

Clean Architecture en 4 proyectos .NET (`Api` → `Application` + `Infrastructure` → `Application` → `Domain`). El detalle de convenciones por capa (DTOs, Result Pattern, controllers, EF configurations) ya está documentado en **[`/AGENTS.md`](../../AGENTS.md)** — no se duplica acá.

## Frontend

Angular (standalone components, sin NgModules) organizado por `core/` (auth, guards, notificaciones), `features/` (auth, social-records, users) y `shared/` (layout, componentes comunes). Ver [CURRENT_STATE.md](./CURRENT_STATE.md) para qué pantallas existen realmente hoy.

## Flujo de branches (verificado en el historial real, no asumido)

Este repo **no** usa un único `dev` como en un flujo estándar. Tiene 4 ramas de larga vida con roles distintos:

```
feature/SCRUM-<n>-<desc>  ──PR──▶  dev-backend  ──┐
fix/<desc>                ──PR──▶  dev-frontend ──┼──merge──▶  dev  ──merge──▶  main
                                                    ┘        (manual,        (manual,
                                                              agrupa         por hito/
                                                              backend+       cierre de
                                                              frontend)      sprint/deploy)
```

Evidencia (commits reales, no inferido):
- `feature/*` y `fix/*` se mergean a **`dev-backend`** o **`dev-frontend`** vía PR de GitHub (`gh pr merge --merge`, siempre merge commit, nunca squash — ver PRs #1 a #29).
- `dev-backend` y `dev-frontend` se integran a **`dev`** de forma manual, no automática: hay commits explícitos tipo `chore: integrar dev-backend en dev`, `chore: integrar dev-front en dev`, y merges `Merge remote-tracking branch 'origin/dev-backend' into dev`. Esto pasa en lotes (ej. antes de una demo o cierre de sprint), no en cada PR.
- **`dev`** se mergea a **`main`** también manualmente, con mensajes explícitos (`Merge dev: cierre Sprint 1`, `Merge dev: agrega Dockerfile de deploy`, `Merge dev: pagina el listado de usuarios`) — típicamente atado a un hito (cierre de sprint, deploy).
- **Al 2026-09-18:** el trabajo de fichas (SCRUM-5/6/7), el clúster de casona (SCRUM-134/140/141/146) y los fixes de QA recién mergeados a `dev-backend`/`dev-frontend` en esta sesión **todavía no están en `dev` ni en `main`**. `main` sigue teniendo solo EP-03 (auth/roles). Ver [CURRENT_STATE.md](./CURRENT_STATE.md).

**Implicancia práctica:** si buscás "qué hay en producción/en el código real de referencia", mirar `main` te da una foto vieja. Para ver el trabajo backend/frontend más reciente hay que mirar `dev-backend`/`dev-frontend` directamente, no asumir que `main` o incluso `dev` están al día.

## Deploy

- Frontend: Dockerfile + nginx, pensado para Dokku (commit `chore(deploy): agrega Dockerfile y nginx para deploy del frontend en Dokku`).
- No hay GitHub Actions ni ningún otro CI configurado todavía — los PRs se mergean sin check automático (`statusCheckRollup` viene vacío en los 4 PRs recientes). Ver [KNOWN_ISSUES.md](./KNOWN_ISSUES.md).
