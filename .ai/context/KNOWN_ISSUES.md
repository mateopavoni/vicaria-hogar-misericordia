# Known issues

Formato: severidad explícita, no párrafos narrativos. Severidad = impacto real dado que el sistema maneja datos de personas en situación de vulnerabilidad, no gravedad técnica en abstracto.

## CRÍTICA

- **Información especialmente sensible (abusos) no tiene ningún control técnico.** El acuerdo de manejarla solo verbalmente (nunca cargarla al sistema ni en papel) es puramente humano — no hay validación, campo restringido, ni política de contenido en código que lo haga cumplir. Si alguien carga esa información en un campo de texto libre (ej. `GeneralNotes` de la ficha, sin límite de contenido más allá de longitud), el sistema la acepta sin aviso. Ver [CONSTRAINTS.md](./CONSTRAINTS.md).
- **No hay CI configurado.** Los 4 PRs mergeados en esta sesión tenían `statusCheckRollup` vacío y `reviewDecision` vacío — se mergean sin ningún check automático (build, tests) ni revisión obligatoria. Con datos sensibles de por medio, un merge roto o con una regresión de seguridad (ej. en el flujo de auth) puede llegar a `dev-backend` sin que nada lo frene.

## ALTA

- **`main` está muy por detrás de `dev-backend`/`dev-frontend`.** Todo el trabajo de fichas (EP-01) y casona (EP-12) — meses de desarrollo — vive solo en las ramas `dev-*`, nunca llegó a `dev` ni a `main`. Si `main` es lo que se deploya, lo que está en producción es solo EP-03.
- **Gap de paridad frontend/backend.** El backend ya expone búsqueda y edición de ficha (SCRUM-6/7) y todo el flujo de casona (estadías, egreso), pero el frontend solo tiene la pantalla de alta de ficha — nada de búsqueda/edición/visualización de ficha, nada de casona. Funcionalidad "completa" en el backend es inutilizable sin UI.

## MEDIA

- **Expiración de sesión no es por inactividad real**, sino por TTL fijo de JWT (60 min) + refresh token (7 días). Existe `feature/SCRUM-96-inactivity-timeout` pero no está mergeada a ninguna rama. Dado que el sistema puede quedar abierto en una compu compartida del hogar, esto podría ser más relevante de lo que su prioridad actual sugiere — ver [OPEN_QUESTIONS.md](./OPEN_QUESTIONS.md).
- **Contradicción de versión de Angular** entre `/PROJECT.md` (16+) y el código real (`^22.1.0`) — puede llevar a alguien del equipo a instalar/asumir la versión incorrecta al onboardearse.
- **RBAC documentado vs. real no coincide:** `/PROJECT.md` lista 3 roles; el código tiene un 4to (`CoordinadorDeCasaConvivencia`) y además un segundo nivel de permisos granulares (`Permission`/`RolePermission`) no descrito en ningún doc de negocio.
- **(Resuelto 2026-09-22)** Los tests de integración con tokens de `Guid.NewGuid()` (`PersonsEndpointTests`, `CasonaStayEndpointTests`) ya se arreglaron aplicando el patrón `SembrarActorAsync` en todas las clases afectadas.

## BAJA

- **~24 branches `feature/*`/`fix/*` en remoto sin PR abierta**, con estado de merge ambiguo (algunas parecen ya integradas por otro camino que no deja rastro de ancestry git limpio). Sin proceso de limpieza. Inventario en [OPEN_QUESTIONS.md](./OPEN_QUESTIONS.md), sin tocar por ahora (decisión explícita del usuario).
