# Decisiones

Registro de decisiones técnicas cerradas, con evidencia. No re-plantear estas como dudas — si algo acá parece raro, es una decisión tomada, no un error a corregir por cuenta propia.

## Stack (cerrado — ver también `/PROJECT.md`, sección "Convenciones de fuente de verdad")

| Decisión | Evidencia |
|---|---|
| Backend: .NET 9 / ASP.NET Core, no NestJS | `backend/Vicaria.sln`, `net9.0` en todos los `.csproj`. Cero `@nestjs/*` en el repo. |
| Base de datos: SQL Server, no PostgreSQL | `Testcontainers.MsSql` + `.UseSqlServer(...)`. PR #10 "refactor: db migrada de postgres a sqlserver" (2026-08-26). |
| ORM: EF Core, no Prisma | `VicariaDbContext` + `DbContextOptions`. Sin `schema.prisma` en el repo. |
| Motor de tests de integración: SQL Server real vía Testcontainers, no Oracle | `Testcontainers.MsSql` v4.1.0, imagen `mcr.microsoft.com/mssql/server:2022-latest`. |
| Identificadores de código en inglés | PR #18 "refactor: identificadores de código a inglés" (2026-08-26) — refactor deliberado, ver `/AGENTS.md`. |
| Merge de PR: merge commit, no squash | Consistente en las 29 PRs del repo. |

El diseño técnico original (NestJS + PostgreSQL + Prisma) fue reemplazado por decisión del equipo — la estructura conceptual de esos diagramas (18 entidades, relaciones) sigue siendo válida como diseño lógico, ver [DOMAIN.md](./DOMAIN.md).

## Decisiones de esta pasada de Context Engineering (2026-09-18)

- **El clúster de casona (evaluación psiquiátrica + estadías + egreso) se documenta como parte de EP-12**, no como épica propia. Instrucción directa del usuario al pedir esta pasada; si el equipo prefiere una épica separada, es reversible — ver [OPEN_QUESTIONS.md](./OPEN_QUESTIONS.md).
- **La contradicción de versión de Angular (doc: 16+, código: `^22.1.0`) no se resolvió** — se documenta como pregunta abierta en vez de asumir cuál es la correcta, por instrucción explícita.
- **La restricción sobre información sensible (abusos) se documenta como acuerdo humano sin enforcement técnico**, porque no se encontró ningún mecanismo en código que la haga cumplir (búsqueda de "abuso"/"información sensible" en todo el repo: sin resultados). Ver [CONSTRAINTS.md](./CONSTRAINTS.md).

## Decisiones del 2026-09-23 (cierre de Sprint 2)

- **`dev-backend` + `dev-frontend` se integran a `dev`**, sin borrar ninguna de las dos ramas fuente. `main` queda sin tocar hasta que se resuelva el rename pendiente de `Hogar`→`Centro Barrial` en `LifeStory` — ver [KNOWN_ISSUES.md](./KNOWN_ISSUES.md).
- **Rename "Casona"→"Casa de Convivencia" confirmado y aplicado** en todo el código, rutas y DB (migración `RenameTable`, no drop+create). **Rename "Hogar"→"Centro Barrial" confirmado pero aplicado solo parcialmente** — el dominio `LifeStory` queda pendiente por ser un rename que toca migración de DB, decisión explícita de no arriesgarlo el mismo día de un demo.
- **`RoleNames.DirectoraDeCasona` no se renombra** — es un valor persistido (rol en DB/JWT), requiere decisión de equipo aparte, no se asume unilateralmente. Ver [OPEN_QUESTIONS.md](./OPEN_QUESTIONS.md).
- **Seed de datos de demo** (`Program.cs`, `SeedDemoData`) se agrega como código de aplicación (no como script SQL aparte), mismo patrón que `SeedTestUsers` ya existente — corre solo en `Development`, es idempotente.

## Gate de RAG / subagente dedicado (Fase 3)

**Verdict: no aplica todavía, con evidencia (no se asume por defecto).**

- Tamaño real del código: 108 archivos `.cs` en `dev-backend`, 50 archivos `.ts` en `dev-frontend`. Entra completo en el contexto de un agente sin indexado especial.
- Volumen de documentación de contexto: 15 archivos `.md` en total contando raíz + `.ai/context/` recién creado — un índice (`00_INDEX.md`) alcanza para navegarlo, no hace falta retrieval.
- Equipo de 4 personas, alcance académico definido, sin necesidad de servir contexto a múltiples equipos o repos en paralelo.

**Cuándo reconsiderar:** si el código supera aprox. 500 archivos fuente, si la documentación de contexto crece a punto de no entrar cómoda en una sesión, o si aparece la necesidad real de que un agente responda sobre múltiples repos del cliente a la vez — no antes. Volver a evaluar con evidencia fresca en ese momento, no reabrir esto en abstracto.
