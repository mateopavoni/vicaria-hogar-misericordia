# Índice de contexto — Vicaria

Set de contexto para agentes de IA (y para el equipo: Amanda, Emir, Belén, Santiago) que trabajan sobre este repo. Generado por una pasada de Context Engineering el 2026-09-18, con una actualización el 2026-09-23 (cierre de Sprint 2, merge de `dev-backend`+`dev-frontend`→`dev`, QA end-to-end), contra el código real de `main`, `dev`, `dev-backend` y `dev-frontend` (no contra otro documento).

**No duplica lo que ya existe.** `AGENTS.md` y `/PROJECT.md` (raíz del repo) siguen siendo la fuente de verdad de convenciones de código y contexto de negocio respectivamente — este set los referencia en vez de repetirlos, y agrega lo que faltaba: estado real verificado, decisiones registradas, huecos conocidos y preguntas abiertas para el equipo.

## Cómo usar este set

| Archivo | Para qué sirve | Léelo si... |
|---|---|---|
| [PROJECT.md](./PROJECT.md) | Puntero al PROJECT.md de raíz + qué cambió en esta pasada | querés contexto de negocio/alcance |
| [ARCHITECTURE.md](./ARCHITECTURE.md) | Capas del backend (resumen, ver AGENTS.md para detalle) + flujo real de branches y deploy | vas a mergear, ramear o deployar algo |
| [DOMAIN.md](./DOMAIN.md) | Las 18 entidades conceptuales vs. cuáles existen hoy en código, roles/permisos reales, reglas de negocio verificadas | vas a tocar el dominio (entidades, reglas) |
| [CONVENTIONS.md](./CONVENTIONS.md) | Puntero a AGENTS.md + convención de git/PR que no estaba documentada | vas a nombrar algo o abrir una PR |
| [DECISIONS.md](./DECISIONS.md) | Decisiones técnicas ya tomadas, con evidencia y fecha | tenés dudas de "¿por qué está hecho así?" |
| [CURRENT_STATE.md](./CURRENT_STATE.md) | Foto verificada al 2026-09-18: qué hay en cada rama | necesitás saber qué está implementado *ahora*, no en el plan |
| [KNOWN_ISSUES.md](./KNOWN_ISSUES.md) | Problemas reales detectados, con severidad | vas a prioritizar o hacer QA |
| [OPEN_QUESTIONS.md](./OPEN_QUESTIONS.md) | Preguntas para que el equipo resuelva — no las resuelvas vos solo | vas a tomar una decisión de producto o arquitectura no cerrada |
| [CONSTRAINTS.md](./CONSTRAINTS.md) | Restricciones de dominio no negociables, con su nivel real de enforcement | vas a agregar un campo o validación |

## Regla de mantenimiento

Estos archivos quedan desactualizados si no se tocan. Cuando una PR cierre algo de `OPEN_QUESTIONS.md` o `KNOWN_ISSUES.md`, o agregue una entidad nueva de `DOMAIN.md`, esa PR debería actualizar el archivo correspondiente — no dejar que se desalinee del código, como ya le pasó una vez a `/PROJECT.md` con los diagramas técnicos originales.
