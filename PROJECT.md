# Proyecto final - Vicaria


Contexto de negocio y producto para que este repositorio. Este archivo es la base para cualquier agente de IA que necesite entender **que** se esta construyendo y **porque**, mas allá de las convenciones técnicas (ver `AGENTS.md` ).

## Nombre del proyecto

**Vicaria** - Sistema de Gestion Nuestra Señora de la Misericordia.

Nombre interno de trabajo tomado del cliente (Vicaria de los pobres)
El nombre formal completo del dispositivo atendido por el sistema es "Parroquia Nuestra Señora de la Misericordia."

## Objetivo

**Objetivo general:** diseñar, desarrollar e implementar un sistema web de gestion de datos e historial integral para el dispositivo, que centralice y digitalice los procesos de admisión, reduzca el uso de papel y facilite la toma de decisiones estratégicas.

**Objetivos específicos:**
1. Digitalizar el registro de personas (fichas flexibles, sin barreras de registro de ingreso).
2. Historial longitudinal de seguimiento (bitácora con fecha/hora/autor automático).
3. Historia de vida en 3 etapas (Antes / En el hogar / Después).
4. Gestion de acceso con roles diferenciados y aprobación por referentes.
5. Agenda compartida del hogar + calendario personal por referente.
6. Registro y gestion de colaboradores (voluntarios/empleados).
7. Inventario de ropa e insumos con alertas de stock bajo.
8. Adjuntar archivos a fichas y exportar a PDF.
9. Registro de asistencia diaria + agenda automatica de medicamentos.
10. Gestion de convenios institucionales y turnos colectivos.
11. Sitio web publico del hogar (landing page). (Queda fuera del MVP).
12. Arquitectura escalable a futuro. (Queda fuera del MVP).

--- 

## Stack

**Definitivo:**

- **Frontend:** Angular 16+ (TypeScript, RxJS, Bootstrap/Tailwind CSS)
- **Backend:** .NET 9.0 + C#
- **Base de datos:** SQL Server + Entity Framework
- **Autenticación:** JWT + BCrypt (salt: 10 rounds) + RBAC (3 roles)
- **DevOps:** Docker, docker-compose, GitHub Actions, Oracle Cloud VM
- **Testing:** xUnit (backend), Jest + Cypress (frontend, si aplica)

**Historial de decisión (contexto, no ambigüedad):** el diseño técnico original (diagramas de clases, arquitectura de monolito modular, esquema de BD) fue elaborado sobre un stack anterior (NestJS + PostgreSQL + Prisma), luego reemplazado por decisión del equipo. La **estructura conceptual** de esos diagramas (18 entidades, relaciones, módulos por dominio) sigue siendo válida como diseño lógico; el mapeo tecnológico específico de esos documentos quedó desactualizado.


**Estado del código (rama `dev-backend`):** El desarrollo de la base de datos queda definitivo con SQL Server. Lo ya creado con PostgreSQL queda deprecado y como version vieja desactualizada.

--- 

## Alcance

**Dentro del alcance (MVP definido con el cliente):**

- Gestión de fichas de personas atendidas, sin campos obligatorios salvo el nombre.
- Bitácora de observaciones con autoría y timestamp automático.
- Historia de vida en 3 etapas.
- Autenticación, roles y aprobación de cuentas.
- Agenda compartida del hogar y calendario personal.
- Registro de colaboradores (voluntarios/empleados).
- Inventario de ropa e insumos con alertas.
- Adjuntos a fichas y exportación a PDF.
- Asistencia diaria y agenda automática de medicación.


---

## Usuarios

El sistema tiene 4 roles con permisos diferenciados (RBAC):
| Rol - Permisos |
|----|
| **Referente** - Acceso completo (full) |
| **DirectoraDeCasona** - Fichas y medicación |
| **Escucha** - Lectura y carga de observaciones únicamente |
| **CoordinadorDeCasaConvivencia** - Gestión de residentes de la casa convivencial (Casona) |

Los 4 roles están definidos como constantes en `RoleNames` (Domain), y los permisos por rol en `Permission`/`RolePermission` con códigos en `PermissionNames`.

**Usuarios finales indirectos (no usan el sistema, pero son el motivo de su existencia):** personas en situación de calle y con problemáticas de consumo que asisten al Hogar de Día, muchas veces sin documentación ni domicilio fijo.

---

## Funcionalidades

Organizadas por épica/sprint según la planificación del proyecto:
| Sprint | Épica | Funcionalidad |
|---|---|---|
| **Sprint 1** | EP-03 | Registro, login, roles y permisos |
| Sprint 2 | EP-01 + EP-02 | Fichas de personas + observaciones/historia de vida |
| Sprint 3 | EP-04 + EP-05 | Calendario compartido/personal + gestión de colaboradores |
| Sprint 4 | EP-10 + EP-11 | Asistencia diaria + agenda de medicamentos |
| Sprint 5 | EP-12 + EP-07 | Evaluación psiquiátrica/estado + documentación y adjuntos (PDF) |
| Sprint 6 | EP-13 + QA | Informes institucionales + regresión y cierre |


### Sprint 1 — Historias de usuario (Módulo de acceso y roles, Ep-03)


| ID    | Historia                        | Descripción                                                                                                           | Jira               |
| ----- | ------------------------------- | --------------------------------------------------------------------------------------------------------------------- | ------------------ |
| RF-01 | Registro y aprobación de cuenta | Registro abierto. La cuenta queda "Pendiente" hasta que un Referente la apruebe y asigne un rol.                      | SCRUM-12           |
| RF-02 | Autenticación segura            | Login de usuarios con bloqueo tras 5 intentos fallidos y expiración de sesión por inactividad.                        | SCRUM-33           |
| RF-03 | Roles diferenciados             | Permisos para Referente (full), Directora de Casona (fichas/medicación) y Escucha (lectura y carga de observaciones). | SCRUM-13, SCRUM-64 |
| RF-04 | Desactivación de cuentas        | Baja lógica de usuarios para inhabilitar el acceso sin alterar la autoría de sus bitácoras históricas.                | SCRUM-20           |
| RF-19 | Notificaciones internas         | Alertas del sistema ante nuevas cuentas pendientes o fichas sin observaciones por más de 30 días.                     | SCRUM-22           |

*(Ir completando con las historias a medida que arranque cada sprint)*

---
## Estado actual

**Último relevamiento del código real (rama `dev`, 2026-08-26):**

✅ **Completo, backend y frontend conectados de punta a punta (no mockeado):**
- RF-01 (registro y aprobación de cuenta): registro, listado de pendientes, aprobar, rechazar, con auditoría y notificación a referentes.
- RF-02 (login/JWT): `LoginAsync`, `POST /api/auth/login`, `refresh`, `logout`. Bloqueo de cuenta tras 5 intentos fallidos con notificación a referentes. Expiración de sesión resuelta con refresh token configurable (`Jwt:RefreshTokenExpirationDays`).
- RF-03 (roles diferenciados): 4 roles (Referente, DirectoraDeCasona, Escucha, CoordinadorDeCasaConvivencia), tabla de permisos (`Permission`/`RolePermission`), reasignación de rol desde el panel.
- RF-04 (desactivación de cuentas): activar/desactivar con auditoría, listado real de usuarios activos/inactivos en el panel.
- RF-19 (notificaciones internas): cuenta pendiente y cuenta bloqueada, panel de notificaciones en el front con marcado individual y masivo como leídas.
- **Fichas de personas (Social Records, EP-01/EP-02 parcial)**: creación, búsqueda con paginación (`PagedResult<T>`) y actualización de fichas flexibles (solo el nombre es obligatorio), con contacto asociado y auditoría. Backend completo en `api/social-records`.
- CORS configurado (`Cors:AllowedOrigins` por ambiente).
- Migración de motor de BD: Postgres → SQL Server, completa.
- Frontend: login, registro, pending-approval y gestión de usuarios (pendientes/activos/suspendidos) conectados al backend real, con interceptor de auth y persistencia de sesión.

❌ **Pendiente:**
- Filtrado de fichas por tipo de persona (SCRUM-88) — backend de fichas (`api/social-records`) ya implementado (crear/buscar/editar), pendiente conectar el panel de fichas del front con estos endpoints.
- Bitácora de observaciones e historia de vida en 3 etapas (EP-02): entidades `Observación`/`HistoriaVida` todavía no existen.
- Listado general de usuarios con paginación y filtro por fecha (hoy el listado de pendientes/activos/inactivos no pagina).
- Tests automatizados de frontend.

*(Actualizar a medida que avanze el proyecto y los sprints)*

---

## Contexto del proyecto

### El cliente

**Organización:** Vicaria de los Pobres, a través del dispositivo "Parroquia Nuestra Señora de la Misericordia".

**Qué hace:** Opera un Hogar de Día en Córdoba capital, de lunes a viernes de 9:30 a 14:30, enfocado en atención, contención y acompañamiento de personas con problemáticas de consumo de sustancias y en situación de calle. También funciona como merendero los martes de 16:00 a 18:00. Tiene una casa convivencial (Casona) en Unquillo. Los jueves de 18:00 a 20:00 hay reuniones con familiares de personas con adicciones.

**Equipo de referentes (personas reales, no ficticias):**
- Trabajador Social
- Cura y Coordinador del Dispositivo
- Contención diaria (Madraza / Directora de Casona)

### El problema

Toda la gestión de información se hace hoy de forma manual y en papel. Esto genera:

  
- **Pérdida de información:** fichas extraviadas o deterioradas.
- **Fragmentación del seguimiento:** sin línea de tiempo estandarizada de observaciones por persona.
- **Dificultad de actualización:** actualizar datos implica reescribir sobre la ficha física, sin registrar qué cambió ni quién.
- **Inaccesibilidad remota:** la información está centralizada en un lugar físico.
- **Falta de registro de colaboradores:** no hay listado formal de voluntarios/empleados.
- **Comunicación ineficiente:** coordinación por WhatsApp o verbal, sin agenda compartida.
- **Sin trazabilidad de accesos:** el personal de escucha no tiene forma formal de registrar sus observaciones.

  

### Principio de diseño clave (no negociable)

El sistema **no debe encasillar ni limitar el ingreso de una persona**. Ningún campo de la ficha es estrictamente obligatorio salvo el nombre, ya que muchos asistentes no tienen documentación ni domicilio fijo. Cualquier funcionalidad nueva que agregue una barrera de datos obligatorios para el ingreso debe ser señalada, no implementada por defecto.

### Modelo de datos conceptual (18 entidades)

Según el diagrama de clases original del proyecto (diseño lógico vigente, mapeo tecnológico desactualizado — ver Stack):


| # | Entidad | Descripción | Estado en código |
|---|---|---|---|
| 1 | User (Usuario) | Cuentas de usuario con autenticación | ✅ Implementada |
| 2 | Person (Persona) | Datos básicos de cada persona atendida | ✅ Implementada |
| 3 | Contact (Contacto) | Información de contacto relacionada | ✅ Implementada |
| 4 | SocialRecord (Ficha) | Información extendida de cada persona | ✅ Implementada (la ficha actual es `SocialRecord` + `Person`, con campos flexibles) |
| 5 | Observation (Observación) | Bitácora de notas y observaciones | ❌ Pendiente (EP-02) |
| 6 | ObservationCategory (CategoriaObservacion) | Categorías para clasificar observaciones | ❌ Pendiente (EP-02) |
| 7 | LifeHistory (HistoriaVida) | Registro de historia de vida en 3 etapas | ❌ Pendiente (EP-02) |
| 8 | Attendance (Asistencia) | Registro diario de asistencia | ❌ Pendiente (EP-10) |
| 9 | MedicationSchedule (EsquemaMedicacion) | Esquema de medicamentos por persona | ❌ Pendiente (EP-11) |
| 10 | MedicationAgenda (AgendaMedicamentos) | Agenda diaria generada automáticamente | ❌ Pendiente (EP-11) |
| 11 | MedicationCatalog (MedicamentoCatalogo) | Catálogo de medicamentos disponibles | ❌ Pendiente (EP-11) |
| 12 | CasonaStay (EstadiasCasona) | Registro de estadías en la casona | ❌ Pendiente |
| 13 | CasonaVisit (VisitasCasona) | Registro de visitas de residentes | ❌ Pendiente |
| 14 | PsychiatricEvaluation (EvaluacionesPsiquiatricas) | Evaluaciones psicológicas documentadas | ❌ Pendiente (EP-12) |
| 15 | CaritasReport (InformeCaritas) | Informes Cáritas (datos en JSONB en el diseño original) | ❌ Pendiente (EP-13) |
| 16 | PersonalCalendarEvent (EventoCalendarioPersonal) | Eventos personales del usuario | ❌ Pendiente (EP-04) |
| 17 | GeneralCalendarEvent (EventoCalendarioGeneral) | Eventos compartidos institucionales | ❌ Pendiente (EP-04) |
| 18 | Collaborator (Colaborador) | Voluntarios y empleados | ❌ Pendiente (EP-05) |

**Nota sobre nombres en código vs. diseño conceptual:** el diagrama original usaba nombres en español (`Usuario`, `Ficha`, `Observación`, etc.). El código real usa identificadores en inglés (`User`, `SocialRecord`, ...), ya que **todo identificador de código va en inglés** (regla de `AGENTS.md`). La tabla expresa el mapeo conceptual → código. Además, en código existen entidades que el diseño original no listaba individualmente: `Role`, `Permission`, `RolePermission`, `AuditLog`, `Notification` (todas ✅ implementadas).

### Marco académico

Este sistema es el Trabajo Final Integrador de la materia Prácticas Profesionalizante II, en el Instituto Superior Cura Gabriel Brochero (TSDS), con Vicaria de los Pobres como cliente real (no un caso simulado). Esto implica que las decisiones de alcance y requerimientos están sujetas a validación real del cliente, no solo a criterios académicos — cualquier cambio de alcance debe confirmarse con los referentes del dispositivo, no asumirse.

---

## Convenciones de fuente de verdad

- El stack tecnológico (SQL Server + EF, .NET 9 + C#) es una decisión cerrada — no volver a plantearla como duda.
- Ante cualquier **otra** ambigüedad entre este documento, el código y la documentación original del Google Drive del proyecto, **señalar la contradicción explícitamente** — no asumir ni resolver por cuenta propia.
- Este archivo debe actualizarse a medida que avanza el desarrollo real, para que no quede desalineado con el código (como pasó con los diagramas técnicos originales).

 