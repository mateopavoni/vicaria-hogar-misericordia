# Dominio

## Las 18 entidades conceptuales — cuáles existen hoy en código

El diseño lógico original (`/PROJECT.md`, sección "Modelo de datos conceptual") sigue vigente como mapa de alcance. Esta tabla cruza esas 18 entidades contra `Vicaria.Domain.Entities` real en `dev-backend` (rama más adelantada; `main` tiene muchas menos, ver [CURRENT_STATE.md](./CURRENT_STATE.md)).

| # | Entidad conceptual | Clase real | Estado |
|---|---|---|---|
| 1 | Usuario | `User` | ✅ implementada |
| 2 | Persona | `Person` | ✅ implementada |
| 3 | Contacto | `Contact` | ✅ implementada |
| 4 | Ficha | `SocialRecord` | ✅ implementada, **tabla separada de `Person`** (confirma la separación ficha/persona) |
| 5 | Observación | — | ❌ no existe |
| 6 | CategoriaObservacion | — | ❌ no existe |
| 7 | HistoriaVida | — | ❌ no existe |
| 8 | Asistencia | — | ❌ no existe |
| 9 | EsquemaMedicacion | — | ❌ no existe |
| 10 | AgendaMedicamentos | — | ❌ no existe |
| 11 | MedicamentoCatalogo | — | ❌ no existe |
| 12 | EstadiasCasona | `CasonaStay` | ✅ implementada (incluye flujo de egreso, `CasonaStayExitDto`) |
| 13 | VisitasCasona | — | ❌ no existe |
| 14 | EvaluacionesPsiquiatricas | `PsychiatricEvaluation` | ✅ implementada, con flag `IsValid` ("vigente") |
| 15 | InformeCaritas | — | ❌ no existe |
| 16 | EventoCalendarioPersonal | — | ❌ no existe |
| 17 | EventoCalendarioGeneral | — | ❌ no existe |
| 18 | Colaborador | — | ❌ no existe |

Entidades adicionales que existen en código y no están en las 18 originales: `AuditLog`, `Notification`, `Role`, `RolePermission`, `Permission`, `PersonType`, `SocialRecordStatus`, `UserStatus` — soporte de auth/RBAC/auditoría, no del dominio de negocio "atención a personas" en sí.

## Roles y permisos (verificado en código, no en el doc)

`RoleNames` (`Vicaria.Domain.Entities`) define **4 roles**, no 3:

- `Referente`
- `DirectoraDeCasona`
- `Escucha`
- `CoordinadorDeCasaConvivencia` ← no está en la tabla de roles de `/PROJECT.md` (sección "Usuarios"). Ver [OPEN_QUESTIONS.md](./OPEN_QUESTIONS.md).

Además hay un segundo nivel de permisos granulares, `PermissionNames`, todos ligados a la Casa de Convivencia:
- `VerFichasResidentesCasaConvivencia`
- `CargarObservacionesResidentes`
- `VerAgendaMedicamentos`

Es decir: el control de acceso real no es solo RBAC por rol como describe `/PROJECT.md` — hay una tabla `Permission`/`RolePermission` con permisos finos asignables, al menos para todo lo relacionado a la Casona.

## Reglas de negocio verificadas en código

- **Sin barreras de ingreso:** `CreateSocialRecordDtoValidator` solo exige `FirstName` (`NotEmpty`); todo lo demás (apellido, DNI, fecha de nacimiento, teléfono, motivo de ingreso, situación habitacional, ocupación, notas) es opcional, con límites de longitud nomás. Esto **sí está enforced en código**, no es solo un principio de diseño en el papel.
- **Evaluación psiquiátrica vigente:** `PsychiatricEvaluation.IsValid` — el comentario del código dice explícitamente que una persona necesita una evaluación vigente para poder ser `Residente` (SCRUM-134/EP-12).
- **Bloqueo de cuenta:** 5 intentos fallidos de login → `LockoutEnd = now + 30 minutos` (`AuthService.cs`, línea ~238). Notifica a los referentes.
- **Expiración de sesión:** JWT de acceso expira a los 60 min (`Jwt:ExpirationMinutes`), refresh token a los 7 días (`Jwt:RefreshTokenExpirationDays`), ambos configurables. **No** es expiración por inactividad real — ver [OPEN_QUESTIONS.md](./OPEN_QUESTIONS.md).
