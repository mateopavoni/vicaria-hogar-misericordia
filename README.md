# Vicaría Hogar Misericordia

Sistema web de gestión de datos e historial integral para el Hogar de Día "Nuestra Señora de la Misericordia", dispositivo de la Vicaría de los Pobres (Pastoral de Adicciones) en Córdoba. Centraliza el registro de las personas atendidas, su seguimiento, la agenda del hogar y la gestión del equipo, reemplazando los registros en papel.

Trabajo Final Integrador de Prácticas Profesionalizantes II, Tecnicatura en Desarrollo de Software, Instituto Superior Cura Gabriel Brochero.

## Contenido

- [Contexto](#contexto)
- [Funcionalidades](#funcionalidades)
- [Roles y permisos](#roles-y-permisos)
- [Arquitectura](#arquitectura)
- [Tecnologías](#tecnologías)
- [Puesta en marcha](#puesta-en-marcha)
- [Pruebas y calidad](#pruebas-y-calidad)
- [Flujo de trabajo](#flujo-de-trabajo)
- [Cómo contribuir](#cómo-contribuir)
- [Ejemplos de uso](#ejemplos-de-uso)
- [Documentación](#documentación)
- [Equipo](#equipo)

## Contexto

El Hogar de Día funciona de lunes a viernes de 9:30 a 14:30 y acompaña a personas en situación de calle y con consumo problemático de sustancias. Además sirve como merendero los martes por la tarde, tiene una casa convivencial en Unquillo y organiza reuniones semanales con familiares.

El equipo registraba a las personas, sus observaciones y la agenda en papel, sin un historial longitudinal ni una vista compartida. El sistema digitaliza esos procesos con un principio de diseño central: ninguna ficha exige datos más allá del nombre, para no crear barreras de registro a quien llega sin documentación.

## Funcionalidades

| Épica | Funcionalidad | Estado |
|---|---|---|
| EP-03 | Registro de cuentas, aprobación por un Referente, inicio de sesión con JWT y renovación de token, bloqueo tras 5 intentos fallidos, desactivación de cuentas y notificaciones internas | Completa |
| EP-01 | Fichas de personas: alta, búsqueda con filtros combinables, edición, perfil completo y tipo de persona (Ambulatorio o Residente) | Completa |
| EP-02 | Observaciones con autoría y fecha automáticas, línea de tiempo con filtros, exportación a CSV, categorías de observación e historia de vida por etapas | Completa |
| EP-12 | Casa de Convivencia: evaluación psiquiátrica, estadías con ingreso automático y egreso con motivo y auditoría | Completa |
| EP-04 | Calendario general del Centro Barrial con actividades recurrentes precargadas, eventos con repetición diaria, semanal o mensual, calendario personal privado y conversión de un evento personal en general | Completa |
| EP-05 | Colaboradores (voluntarios y empleados): alta, edición, baja y reactivación lógicas, detalle y búsqueda por nombre o área | Completa |
| EP-05 | Calendario semanal de visitas a residentes de la Casa de Convivencia, con estados, motivo de cancelación opcional y aviso de solapamiento de horarios | Completa |
| EP-10 | Asistencia diaria con actividades configurables, modificación justificada de días cerrados, estadísticas, detección de inactivos, resumen mensual en PDF e inactivación automática a los 30 días | Completa |
| EP-11 | Medicación: esquemas por persona con revisión mensual, agenda diaria de tomas, historial exportable a PDF e inventario de medicamentos con alerta de stock bajo | Completa |
| EP-07 | Documentos adjuntos de la ficha (imágenes y PDF) y exportación de la ficha a PDF | Completa |
| EP-13 | Informe estadístico para Cáritas con catálogo de valores configurable y exportación a PDF; respaldos diarios de la base con retención y descarga | Completa |

Quedan en el backlog, sin planificar: stock de ropa (EP-06), convenios (EP-08) y la página pública (EP-09).

Toda operación que modifica el estado de una entidad relevante queda registrada en un registro de auditoría con el actor, la acción, la entidad afectada y la fecha en UTC.

## Roles y permisos

El acceso se controla por roles. Las cuentas nuevas quedan en estado pendiente hasta que un Referente las aprueba y les asigna un rol.

| Rol | Alcance |
|---|---|
| Referente | Acceso completo: usuarios, fichas, calendario, colaboradores y visitas |
| Directora de Casa de Convivencia | Fichas y medicación, solo de personas Residentes; calendario en lectura; gestión de visitas |
| Coordinador de Casa de Convivencia | Igual que la Directora; gestión de los residentes de la casa convivencial |
| Escucha | Lectura de fichas y calendario, y carga de observaciones; sin acceso a visitas ni a colaboradores |

## Arquitectura

Monolito con Clean Architecture en el backend y una aplicación de una sola página en el frontend.

```
.
├── backend/
│   ├── src/
│   │   ├── Vicaria.Api              Controllers, autenticación y configuración de la aplicación
│   │   ├── Vicaria.Application      DTOs, validadores, interfaces de servicios y resultados
│   │   ├── Vicaria.Domain           Entidades, enumeraciones y constantes, sin dependencias externas
│   │   └── Vicaria.Infrastructure   Servicios, DbContext, configuraciones de EF y migraciones
│   ├── tests/
│   │   ├── Vicaria.UnitTests        Servicios con base en memoria y lógica de autorización
│   │   └── Vicaria.IntegrationTests API completa con WebApplicationFactory y SQL Server real
│   └── docker-compose.yml           SQL Server, API y frontend para ejecución local
├── frontend/                        Aplicación Angular
├── docs/                            Manual técnico, datos de demostración y material de QA
├── .github/workflows/               Integración continua (ci.yml) y publicación de imágenes (cd.yml)
└── .ai/context/                     Estado verificado, decisiones y preguntas abiertas
```

La regla de dependencia es `Api` hacia `Application` e `Infrastructure`, `Infrastructure` hacia `Application`, y `Application` hacia `Domain`. El dominio no depende de nada.

Decisiones de diseño que se mantienen en todo el código:

- Las operaciones de los servicios devuelven un objeto de resultado con factory methods (`Ok`, `NotFound`, `DuplicateDni`, entre otros) en lugar de usar excepciones para el flujo esperado. El controller traduce el resultado a códigos HTTP.
- Un validador de FluentValidation por DTO, con mensajes en español para el usuario final.
- Los permisos se expresan con las constantes de `RoleNames`; no se escriben nombres de rol sueltos en los atributos de autorización.
- Todos los identificadores de código están en inglés. El español se reserva para mensajes al usuario, documentación y valores de datos del negocio.

## Tecnologías

| Capa | Tecnología |
|---|---|
| Backend | .NET 9, C#, ASP.NET Core, Entity Framework Core, FluentValidation |
| Base de datos | SQL Server 2022 |
| Autenticación | JWT con renovación de token, BCrypt, control de acceso por roles |
| Frontend | Angular, TypeScript, RxJS, Tailwind CSS |
| Pruebas | xUnit, Testcontainers (backend); Vitest (frontend); Playwright (flujos de interfaz) |
| Entrega continua | GitHub Actions, Docker y Docker Compose |

## Puesta en marcha

Requisitos: Docker Desktop con al menos 4 GB de memoria asignados.

```bash
cd backend
cp .env.example .env
docker compose up -d --build
```

El comando levanta tres contenedores. La API aplica las migraciones y carga datos de prueba al arrancar.

| Servicio | Dirección |
|---|---|
| Frontend | http://localhost:4200 |
| API | http://localhost:5000 |
| Documentación de la API (Swagger) | http://localhost:5000/swagger |
| SQL Server | localhost:1434 |

Usuarios de prueba, todos con la contraseña `Test1234!`:

| Correo | Rol |
|---|---|
| `referente@test.com` | Referente |
| `directora@test.com` | Directora de Casa de Convivencia |
| `coordinador@test.com` | Coordinador de Casa de Convivencia |
| `escucha@test.com` | Escucha |

Para detener el entorno, `docker compose down`; con `-v` también se elimina la base de datos.

La guía completa, incluida la ejecución con servidores de desarrollo (`dotnet run` y `ng serve`) y la lista de datos sembrados, está en [`deploy-local.md`](./deploy-local.md).

Los secretos locales, como la cadena de conexión real, van en archivos `appsettings.{Environment}.local.json` o en `.env`, ambos fuera del control de versiones. No se deben commitear credenciales.

## Pruebas y calidad

```bash
# Backend
cd backend
dotnet test tests/Vicaria.UnitTests
dotnet test tests/Vicaria.IntegrationTests -- xUnit.ParallelizeTestCollections=false

# Frontend
cd frontend
npm ci
npx ng test --watch=false
npx ng build --configuration production
```

Las pruebas de integración levantan un SQL Server real con Testcontainers y se ejecutan de forma secuencial para no saturar Docker.

Estado de la última ejecución completa (3 de noviembre de 2026, rama `main`):

| Suite | Resultado |
|---|---|
| Backend, pruebas unitarias (`backend/tests/Vicaria.UnitTests`) | 364 de 364 |
| Backend, pruebas de integración (`backend/tests/Vicaria.IntegrationTests`) | 270 de 270 |
| Frontend, pruebas unitarias y compilación de producción | 181 de 181 y compilación sin advertencias |

La integración continua (`.github/workflows/ci.yml`) compila y ejecuta las pruebas de backend y frontend en cada push y pull request hacia `main` y `dev`. Cuando el CI de `main` termina bien, el despliegue continuo (`.github/workflows/cd.yml`) publica las imágenes Docker de la API y del frontend en GitHub Container Registry (`ghcr.io`).

En [`docs/qa/`](./docs/qa/) están los scripts que generan el plan de pruebas, la planilla de ciclos y el informe de pruebas, los flujos de interfaz reproducibles con Playwright (`docs/qa/e2e/`) y las pruebas de seguridad y carga de la API (`docs/qa/security/`). Las planillas y documentos generados (`.xlsx` y `.docx`) no se versionan y se entregan aparte.

## Flujo de trabajo

| Rama | Uso |
|---|---|
| `main` | Versión estable; recibe `dev` mediante merge al cerrar un hito |
| `dev` | Integración de backend y frontend; refleja el estado más completo |
| `dev-backend` | Rama de integración del equipo de backend |
| `dev-frontend` | Rama de integración del equipo de frontend |
| `feature/*`, `fix/*` | Trabajo por historia de usuario, que se integra mediante pull request con merge commit |

Los mensajes de commit y la documentación se escriben en español. Las migraciones de Entity Framework tienen nombres descriptivos en inglés.

## Cómo contribuir

1. Crear una rama desde `dev` con el número de la historia de Jira: `feature/SCRUM-123-descripcion-corta` (o `fix/...` para una corrección).
2. Respetar las convenciones de [`AGENTS.md`](./AGENTS.md): identificadores en inglés, mensajes al usuario en español, Result Pattern en los servicios, constantes de `RoleNames` en la autorización y un registro de auditoría en toda operación que cambie el estado de una entidad.
3. Agregar pruebas unitarias para la lógica nueva y pruebas de integración si se toca un endpoint.
4. Correr las pruebas de backend y frontend en local (ver [Pruebas y calidad](#pruebas-y-calidad)).
5. Escribir commits cortos y descriptivos, en español, con el formato `tipo(alcance): descripción` (`feat`, `fix`, `docs`, `test`, `chore`).
6. Abrir un pull request hacia `dev`. El CI tiene que pasar antes del merge.

## Ejemplos de uso

Con el entorno levantado (ver [Puesta en marcha](#puesta-en-marcha)), la forma más simple de usar el sistema es entrar a http://localhost:4200 con alguno de los usuarios de prueba. Algunos recorridos habituales:

- **Registrar a una persona que llega al hogar:** Fichas → Nueva ficha. Solo el nombre es obligatorio; el resto se completa después.
- **Cargar el seguimiento:** desde el perfil de la persona, Observaciones → Nueva observación, con categoría. La línea de tiempo se filtra por fecha, autor o categoría y se exporta a CSV.
- **Tomar asistencia del día:** Asistencia → elegir la actividad y marcar a las personas presentes.
- **Ver la agenda de medicamentos:** Medicación muestra las tomas del día; cada una se marca como administrada o no administrada.
- **Generar el informe para Cáritas:** Informe Cáritas, elegir el período y exportar a PDF.

La API también se puede usar directamente. Por ejemplo, iniciar sesión y crear una ficha:

```bash
# inicia sesión y guarda el token
TOKEN=$(curl -s http://localhost:5000/api/auth/login   -H "Content-Type: application/json"   -d '{"email":"referente@test.com","password":"Test1234!"}' | jq -r .token)

# crea una ficha con el único dato obligatorio
curl -s http://localhost:5000/api/social-records   -H "Authorization: Bearer $TOKEN"   -H "Content-Type: application/json"   -d '{"firstName":"Juan","hasDocumentation":false}'

# busca fichas por nombre o DNI
curl -s "http://localhost:5000/api/social-records?q=juan" -H "Authorization: Bearer $TOKEN"
```

Todos los endpoints, con sus parámetros y respuestas, se pueden probar desde Swagger en http://localhost:5000/swagger.

## Documentación

| Documento | Contenido |
|---|---|
| [`PROJECT.md`](./PROJECT.md) | Contexto de negocio, objetivos, alcance y roles |
| [`AGENTS.md`](./AGENTS.md) | Convenciones de código, patrones por capa y reglas de trabajo |
| [`docs/MANUAL_TECNICO.md`](./docs/MANUAL_TECNICO.md) | Manual técnico: instalación desde cero y configuración por entorno, integraciones y seguridad |
| [`docs/GUIA_USUARIO.md`](./docs/GUIA_USUARIO.md) | Guía de usuario final: acceso, navegación, funciones principales con capturas y ejemplos de uso por rol |
| [`deploy-local.md`](./deploy-local.md) | Guía para levantar el sistema en local |
| [`.ai/context/`](./.ai/context/00_INDEX.md) | Estado verificado contra el código, decisiones y deuda conocida |
| [`docs/demo/`](./docs/demo/) | Carga de datos de demostración |
| [`docs/qa/`](./docs/qa/) | Scripts del plan de pruebas, los ciclos y el informe; flujos de interfaz y pruebas de seguridad |

## Equipo

- Belén Muñoz
- Santiago Nietto
- Mateo Pavoni
- Emir Rojano
- Amanda Torres

Docente: Karina Salto.
