# Proyecto Integrador - Vicaria - Guía para Agentes
---

Contiene decisiones técnicas ya tomadas y convenciones observadas en el código existente. No las reinventes ni las cambies sin confirmación explícita del equipo.
Ver `PROJECT.md` para el contexto de negocio, historias de usuario y estado del proyecto.

--- 
## Convenciones de nomenclatura

**Regla no-negociable: todo identificador de código (clases, métodos, variables, propiedades, parámetros, DTOs, enums, constantes) se escribe en inglés.** Sin excepción. El español queda reservado para: comentarios de código (opcional), mensajes de validación/error mostrados al usuario, y valores de datos que reflejan términos reales del negocio en español (ver más abajo).


### Clases

- PascalCase, sustantivos en inglés: `User`, `Role`, `AuditLog`.
- Sufijos según el tipo: `...Dto` para DTOs (`RegisterDto`), `...Service` para servicios (`AuthService`), `...Validator` para validadores FluentValidation (`RegisterDtoValidator`), `...Result` para el Result Pattern (`RegisterResult`), `...Configuration` para configuraciones de EF (`UserConfiguration`), `...Controller` para controllers (`AuthController`).
- Interfaces con prefijo `I`: `IAuthService`.

### Métodos

- PascalCase, verbos en inglés que describen la acción: `RegisterAsync`, `ApproveUserAsync`, `GetPendingUsersAsync`.
- Sufijo `Async` obligatorio en todo método asíncrono.
- Factory methods estáticos del Result Pattern en inglés: `.Ok(...)`, `.DuplicateEmail()`, `.UserNotFound()`.

### Variables y propiedades

- Propiedades públicas: PascalCase, inglés (`FirstName`, `LastName`, `Status`, `RoleId`).
- Variables locales y parámetros: camelCase, inglés (`userId`, `cancellationToken`).
- Constantes: PascalCase, inglés, agrupadas en clases estáticas (`RoleNames.Referent`, no `RolNombres.Referente`).
- Enums: nombre del tipo y sus valores en inglés (`UserStatus { Pending, Active, Inactive, Rejected }`).

### Lo que SÍ va en español

- **Mensajes de validación y de error mostrados al usuario** (ya es el patrón actual con FluentValidation) — el usuario final del sistema (Referente, Directora de Casona, Escucha) trabaja en español.
- **Valores de datos que son términos reales del negocio en español**, no identificadores de código. Ejemplo: el *valor* de un rol puede seguir siendo el string `"Referente"` (así lo usa el cliente), pero la *constante que lo contiene* se llama `RoleNames.Referent`, no `RolNombres.Referente`.
- **Mensajes de commit, documentación (`AGENTS.md`, `PROJECT.md`, PRs, Jira)**
- Comentarios de código: permitido en español, no es obligatorio traducirlos.

---
## Arquitectura

Clean Architecture en capas, organizadas como proyectos .NET separados dentro de `backend/src/`:

```

Vicaria.Api            → Controllers, Program.cs, configuración de la app (capa de presentación)

Vicaria.Application    → DTOs, interfaces de servicios, validadores, lógica de casos de uso

Vicaria.Domain         → Entidades de dominio puras, enums, constantes (sin dependencias externas)

Vicaria.Infrastructure → Implementación de servicios, DbContext, configuraciones EF, migraciones

```

  
Tests en `backend/tests/`:

```

Vicaria.UnitTests         → Tests aislados (servicios con InMemory DB, lógica de autorización)

Vicaria.IntegrationTests  → Tests end-to-end contra la API completa (WebApplicationFactory)

```

**Regla de dependencia:** `Api` → `Application` + `Infrastructure` → `Application` → `Domain`. El dominio no depende de nada.

  

---

  

## Patrones de código (por capa)

### Entidades (Domain)

- Nombres en inglés, PascalCase: `User`, `Role`, `AuditLog`
- Enums en inglés, tipo y valores: `UserStatus { Pending, Active, Inactive, Rejected }`
- Constantes de roles centralizadas en una clase estática en inglés: `RoleNames.Referent`, `RoleNames.???`, `RoleNames.???` — usar siempre esta clase en `[Authorize(Roles = ...)]`, nunca strings sueltos. No asumir un nombre en inglés sin validarlo con el equipo (candidatos a discutir: `CasonaDirector`/`HouseDirector`, `Listener`/`Attendant`).

### DTOs y validadores (Application)

- DTOs como `record` inmutables, propiedades en inglés: `public record RegisterDto(string FirstName, string LastName, string Email, string Password);`
- Un validador FluentValidation por DTO, sufijo `Validator`: `RegisterDtoValidator`, `ApproveUserDtoValidator`
- Mensajes de validación siempre en español (son para el usuario final, ver sección Convenciones)

### Result Pattern (Application)

No se usan excepciones para flujo de control esperado. Cada operación de servicio devuelve una clase `Result` con:
- `Success: bool`
- `ErrorMessage: string?`
- Factory methods estáticos: `.Ok(...)`, `.DuplicateEmail()`, `.UserNotFound()`, etc.
- Si hay múltiples tipos de error posibles, un enum dedicado (`ApproveUserError { UserNotFound, InvalidState, InvalidRole }`) mapeado en el controller a códigos HTTP con `switch`

Ejemplo de mapeo en el controller:

```csharp

return result.Error switch

{

    null => NoContent(),

    ApproveUserError.UserNotFound => NotFound(new { message = result.ErrorMessage }),

    ApproveUserError.InvalidRole => BadRequest(new { message = result.ErrorMessage }),

    _ => Conflict(new { message = result.ErrorMessage })

};

```

  

### Controllers (Api)

- Ruta base: `[Route("api/{recurso-en-minuscula-ingles}")]`, ej. `api/auth`
- Un solo controller por dominio funcional (no dividir en Ep-XX
- Actor autenticado obtenido siempre igual: `Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)`
- Errores de validación: `ModelState.AddModelError` + `return ValidationProblem(ModelState)`

  

### Base de datos (Infrastructure)

- `DbSet` en plural, inglés: `Users`, `Roles`, `AuditLogs`
- Una clase `{Entity}Configuration.cs` por entidad en `Persistence/Configurations/`, aplicadas con `modelBuilder.ApplyConfigurationsFromAssembly(...)`
- Migraciones con nombre descriptivo en PascalCase e inglés, prefijo de timestamp automático de EF: `AddLastNameUserStatus`, `AddRoleAndRoleIdUser`, `AddAuditLog`

  

### Auditoría

Toda operación sensible (aprobar/rechazar usuario, etc.) registra un `AuditLog` con: `UserId` (del actor, no del afectado), `Action` ( El contenido del texto puede ir en español si es para lectura humana, el nombre del campo va en inglés), `AffectedEntity` (formato `"Entity:Id"`), `Date` (UTC).

  

---

  

## Cómo correr el proyecto


```bash

# Restaurar y compilar

cd backend

dotnet restore

dotnet build

  

# Correr tests

dotnet test

  

# Levantar la API (desde Vicaria.Api)

dotnet run --project src/Vicaria.Api

```

  

Swagger disponible en `/swagger` en ambiente de desarrollo.

Secretos locales (connection string real, etc.) van en `appsettings.{Environment}.local.json`, que **no se versiona** (ver `.gitignore`). 
(CRITICAL) No commitear credenciales reales.

---

  

## Reglas para agentes de IA al trabajar en este repo

1. El destino final de la capa de persistencia es SQL Server, no PostgreSQL. Si el trabajo implica tocar migraciones o el DbContext, tenerlo en cuenta (no es necesario volver a confirmarlo). (NO BORRAR HASTA QUE LA MIGRACION DE LA BBDD SE HAYA COMPLETADO).
2. Seguir el Result Pattern existente, no introducir excepciones para flujo de control esperado.
3. Todo nuevo endpoint sensible a permisos debe usar `RoleNames`, nunca strings de rol hardcodeados.
4. Toda operación que modifique estado de una entidad relevante (usuarios, fichas, etc.) debe registrar `AuditLog`.
5. Nuevas features en Application/Infrastructure deben tener tests unitarios (InMemory DB) y, si tocan un endpoint, tests de integración siguiendo el patrón de `AuthControllerTests`.
6. Todo identificador de código nuevo (clases, métodos, variables, propiedades) va en inglés — ver sección Convenciones. Si el código que estás tocando todavía tiene nombres en español (pendiente de refactor), no mezclar inglés y español dentro del mismo archivo sin que el refactor esté explícitamente en curso.
7. No asumir ni inventar decisiones de producto — para dudas sobre alcance o prioridad, consultar `PROJECT.md` primero; si no está ahí, preguntar antes de implementar.