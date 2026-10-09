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
- **Al 2026-09-23:** `dev-backend` y `dev-frontend` ya se integraron a `dev` (merge manual vía rama `dev-integration`, sin borrar `dev-backend`/`dev-frontend`). `dev` es ahora la foto más completa del proyecto. `main` sigue teniendo solo EP-03 (auth/roles) — el pase de `dev` a `main` está pendiente, condicionado al rename `Hogar`→`Centro Barrial` en `LifeStory` (ver [KNOWN_ISSUES.md](./KNOWN_ISSUES.md)).

**Implicancia práctica:** para ver el estado más completo y real del proyecto, mirar `dev` (no `dev-backend`/`dev-frontend` por separado, y no `main`, que sigue atrasado).

## Deploy

- Frontend: Dockerfile + nginx, pensado para Dokku (commit `chore(deploy): agrega Dockerfile y nginx para deploy del frontend en Dokku`).
- No hay GitHub Actions ni ningún otro CI configurado todavía — los PRs se mergean sin check automático (`statusCheckRollup` viene vacío en los 4 PRs recientes). Ver [KNOWN_ISSUES.md](./KNOWN_ISSUES.md).
