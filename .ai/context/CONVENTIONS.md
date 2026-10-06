# Convenciones — puntero

Las convenciones de nomenclatura, arquitectura por capa, Result Pattern, DTOs/validadores y auditoría ya están completamente documentadas en **[`/AGENTS.md`](../../AGENTS.md)**. No se duplican acá — leelo ahí.

## Lo que faltaba: convención de git/PR

No estaba documentado en ningún lado. Verificado en el historial real (ver [ARCHITECTURE.md](./ARCHITECTURE.md) para el flujo completo de ramas):

- Branch de trabajo: `feature/SCRUM-<n>-<descripción-corta-en-español>` o `fix/<descripción>` (a veces con `SCRUM-<n>` en el medio). No hay un patrón único de mayúsculas/guiones estrictamente uniforme — inconsistencia menor, no bloqueante.
- Se abre PR contra `dev-backend` o `dev-frontend` (nunca directo a `dev` o `main`, salvo excepciones históricas puntuales).
- El merge de PR es siempre **merge commit** (`gh pr merge --merge`), nunca squash ni rebase — preserva el historial de commits de cada feature.
- No hay checks automáticos ni requisito de review configurado en GitHub (`reviewDecision` y `statusCheckRollup` vienen vacíos en todas las PRs revisadas) — el merge queda a criterio de quien lo hace. Ver [KNOWN_ISSUES.md](./KNOWN_ISSUES.md).
