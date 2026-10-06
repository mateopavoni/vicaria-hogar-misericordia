# Proyecto — puntero

La fuente de verdad de contexto de negocio, objetivo, alcance y stack es **[`/PROJECT.md`](../../PROJECT.md)** (raíz del repo). Este archivo no la duplica.

## Qué cambió en esta pasada de Context Engineering (2026-09-18)

- Se corrigió la tabla de sprints/épicas de `/PROJECT.md`: el clúster de casona (evaluación psiquiátrica con flag `IsValid`, estadías, egreso — PRs #24, #27, #29) estaba implementado en `dev-backend` pero no aparecía en ninguna épica de la tabla. Quedó integrado a **EP-12** (ver `/PROJECT.md`).
- Se refrescó la sección "Estado actual" de `/PROJECT.md` con una foto verificada contra el código real (antes estaba fechada 2026-08-26, sobre `dev`; ahora es 2026-09-18, comparando `main` vs `dev-backend` vs `dev-frontend` por separado — ver también [CURRENT_STATE.md](./CURRENT_STATE.md)).
- Se detectaron dos contradicciones nuevas entre `/PROJECT.md` y el código que **no se resolvieron acá** — quedan en [OPEN_QUESTIONS.md](./OPEN_QUESTIONS.md) para el equipo: versión de Angular (doc dice 16+, código usa `^22.1.0`) y la tabla de roles (doc lista 3, el código tiene un 4to rol `CoordinadorDeCasaConvivencia`).

## Verificación previa (7 puntos, ya cerrada — no re-investigar)

Ver [DECISIONS.md](./DECISIONS.md) para el detalle con evidencia de: framework backend (.NET 9), base de datos (SQL Server), ORM (EF Core), motor de tests de integración (Testcontainers.MsSql), separación Ficha/Persona, flag "vigente", lockout de 5 intentos, y expiración de sesión por JWT fijo (no por inactividad).
