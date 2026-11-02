# Manual técnico — Sistema de gestión de la Vicaría de los Pobres

Sistema de gestión del Hogar de Día y la Casa de Convivencia de la Vicaría de los Pobres, Pastoral de Adicciones. Este manual explica cómo instalar el sistema desde cero y cómo configurarlo para cada entorno.

| Dato | Detalle |
|---|---|
| Versión | rama `dev`, Sprint 6 (noviembre de 2026) |
| Componentes | API REST en .NET 9, aplicación web en Angular 22 y base de datos SQL Server 2022 |
| Repositorio | https://github.com/mateopavoni/vicaria-hogar-misericordia |

## Contenido

1. [Manual de instalación](#1-manual-de-instalación)
   - [Requisitos del sistema](#11-requisitos-del-sistema)
   - [Pasos de instalación](#12-pasos-de-instalación)
   - [Verificación post-instalación](#13-verificación-post-instalación)
   - [Configuración inicial](#14-configuración-inicial)
2. [Manual de configuración](#2-manual-de-configuración)
   - [Archivos de configuración](#21-archivos-de-configuración)
   - [Configuración por entorno](#22-configuración-por-entorno)
   - [Integraciones externas](#23-integraciones-externas)
   - [Seguridad](#24-seguridad)

---

## 1. Manual de instalación

### 1.1 Requisitos del sistema

#### Hardware

| Recurso | Mínimo | Recomendado |
|---|---|---|
| Procesador | 2 núcleos x86-64 | 4 núcleos x86-64 |
| Memoria RAM | 4 GB | 8 GB |
| Disco | 20 GB libres | 50 GB en SSD |
| Red | Conexión a internet para descargar las imágenes | IP pública fija o dominio propio para producción |

SQL Server necesita al menos 2 GB de RAM para él solo. El disco crece con los adjuntos de las fichas (hasta 10 MB por archivo) y con los respaldos: se guardan los últimos 30, de unos 13 MB cada uno con los datos actuales.

La imagen de SQL Server existe solo para x86-64 (amd64). En un servidor ARM, como las instancias Ampere de Oracle Cloud o los Mac con Apple Silicon, no corre de forma nativa.

#### Sistema operativo

| Uso | Sistemas compatibles |
|---|---|
| Servidor de producción | Linux x86-64 con Docker Engine 24 o posterior. Probado con Ubuntu Server 22.04 y 24.04 LTS |
| Equipo de desarrollo o de pruebas | Windows 10/11 o macOS con Docker Desktop (con al menos 4 GB de memoria asignados), o Linux con Docker Engine |
| Navegador del usuario final | Google Chrome actualizado. Las pruebas se hicieron en Chrome y Chromium |

#### Dependencias

Con Docker, que es la forma recomendada, alcanza con tener **Docker** y **Git**: el resto viene dentro de las imágenes.

| Componente | Versión | Para qué se usa |
|---|---|---|
| Docker / Docker Compose | Docker 24 o posterior, Compose 2.24 o posterior | Levantar los tres contenedores |
| Git | cualquier versión actual | Descargar el código |
| SQL Server | 2022, edición Express (imagen `mcr.microsoft.com/mssql/server:2022-latest`) | Base de datos |
| .NET SDK | 9.0 (solo para desarrollar sin Docker) | Compilar y correr la API |
| Node.js | 22.22.3 o posterior dentro de la 22, o 24.15 o posterior (solo para desarrollar sin Docker) | Compilar y servir el frontend con la CLI de Angular |
| nginx | imagen `nginx:alpine` | Servir el frontend y reenviar `/api` a la API |

Librerías principales:

| Capa | Librería | Versión |
|---|---|---|
| Backend | ASP.NET Core / Entity Framework Core (proveedor SQL Server) | 9.0 |
| Backend | FluentValidation (validaciones) | 12.1 |
| Backend | BCrypt.Net-Next (hash de contraseñas) | 4.2 |
| Backend | Microsoft.AspNetCore.Authentication.JwtBearer (tokens) | 9.0 |
| Backend | PDFsharp (informes en PDF) | 6.2.4 |
| Backend | Swashbuckle (documentación Swagger) | 7.2 |
| Frontend | Angular | 22.1 |
| Frontend | TypeScript | 6.0 |
| Frontend | Tailwind CSS | 4 |

### 1.2 Pasos de instalación

#### Paso 1: Descargar el software

```bash
git clone https://github.com/mateopavoni/vicaria-hogar-misericordia.git
cd vicaria-hogar-misericordia
git checkout main
```

La rama `main` tiene la última versión liberada; `dev` tiene la versión en curso.

#### Paso 2: Crear el archivo de variables de entorno

```bash
cd backend
cp .env.example .env
```

Editar `backend/.env`:

| Variable | Qué poner |
|---|---|
| `MSSQL_SA_PASSWORD` | Contraseña del usuario `sa` de SQL Server: al menos 8 caracteres, con mayúsculas, minúsculas, números y símbolos |
| `JWT_KEY` | Clave con la que se firman los tokens de sesión: una cadena aleatoria de al menos 32 caracteres |
| `FRONTEND_PATH` | Ruta a la carpeta `frontend/` del repositorio. Desde `backend/` es `../frontend` |

Para generar una clave aleatoria:

```bash
openssl rand -base64 48
```

`.env` no se versiona (está en `.gitignore`). Los valores de `.env.example` son solo para un entorno local.

#### Paso 3: Instalar las dependencias y levantar los contenedores

```bash
docker compose up -d --build
```

El comando:

1. Descarga las imágenes base (SQL Server, .NET 9, Node 22 y nginx).
2. Compila la API: hace `dotnet restore`, que instala los paquetes NuGet, y `dotnet publish`.
3. Compila el frontend: hace `npm ci`, que instala los paquetes de npm, y `ng build --configuration production`.
4. Levanta tres contenedores:

| Contenedor | Servicio | Puerto en el equipo |
|---|---|---|
| `vicaria-sqlserver` | SQL Server 2022 Express | 1434 |
| `vicaria-api` | API REST (.NET 9) | 5000 |
| `vicaria-web` | Frontend (nginx) | 4200 |

La primera vez tarda entre 5 y 10 minutos. Al arrancar, la API espera a que SQL Server esté sano y **aplica sola las migraciones de la base de datos**: no hace falta ejecutar ningún script SQL.

#### Instalación sin Docker (solo para desarrollar)

Primero hay que levantar únicamente la base:

```bash
cd backend
docker compose up -d sql
```

Después la API, desde la carpeta `backend`:

```bash
dotnet restore
dotnet run --project src/Vicaria.Api
```

Y el frontend, en otra terminal, desde la carpeta `frontend`:

```bash
npm ci
npx ng serve
```

Notas:
- La API toma la conexión de `appsettings.Development.json`, que apunta a `localhost,1434`.
- `ng serve` publica el frontend en http://localhost:4200 y reenvía `/api` a la API según `frontend/proxy.conf.json`. Si la API corre en otro puerto, hay que ajustar `target` en ese archivo.

La guía detallada para desarrollo está en [`deploy-local.md`](../deploy-local.md).

### 1.3 Verificación post-instalación

1. Los tres contenedores tienen que estar en estado `Up`, y `vicaria-sqlserver` en `healthy`:

   ```bash
   docker compose ps
   ```

2. La API no tiene que mostrar errores al arrancar y tiene que haber aplicado las migraciones:

   ```bash
   docker compose logs api --tail 50
   ```

3. Comprobar en el navegador:

   | Dirección | Resultado esperado |
   |---|---|
   | http://localhost:4200 | Pantalla de inicio de sesión |
   | http://localhost:5000/swagger | Documentación interactiva de la API (solo en el entorno de desarrollo) |

4. Iniciar sesión con un usuario de prueba (ver 1.4) y abrir Fichas, Asistencia y Calendario: tienen que cargar sin mensajes de error.

5. Opcional, para quien desarrolla: correr las pruebas automatizadas.

   ```bash
   cd backend && dotnet test        # 361 unitarias y 270 de integración (las de integración necesitan Docker)
   cd ../frontend && npx ng test --watch=false   # 191 pruebas
   ```

### 1.4 Configuración inicial

#### Creación de usuarios

**En desarrollo y pruebas** (`ASPNETCORE_ENVIRONMENT=Development`) la API crea al arrancar un usuario por rol, todos con la contraseña `Test1234!`:

| Correo | Rol |
|---|---|
| `referente@test.com` | Referente |
| `directora@test.com` | Directora de Casa de Convivencia |
| `coordinador@test.com` | Coordinador de Casa de Convivencia |
| `escucha@test.com` | Escucha |

**En producción** no se crea ningún usuario. El registro es abierto, pero cada cuenta nueva queda *Pendiente* hasta que un Referente la aprueba, así que el primer Referente se habilita una sola vez a mano:

1. Registrarse desde la pantalla de registro con el correo de quien va a ser Referente.
2. Activar esa cuenta y asignarle el rol en la base de datos:

   ```bash
   docker exec -it vicaria-sqlserver /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -d VicariaDb -Q "UPDATE users SET Status = 'Active', RoleId = (SELECT Id FROM roles WHERE Name = 'Referente') WHERE Email = 'correo@dominio.org';"
   ```

3. Iniciar sesión con esa cuenta. De ahí en adelante, las demás cuentas se aprueban desde **Usuarios > Pendientes**, donde se elige el rol de cada una.

#### Carga de datos base

Las migraciones cargan solas los datos que el sistema necesita para funcionar:

| Datos | Contenido |
|---|---|
| Roles | Referente, Directora de Casa de Convivencia, Coordinador de Casa de Convivencia y Escucha |
| Permisos | Los permisos de cada rol (ver 2.4) |
| Categorías de observaciones | Las categorías con que se clasifican las observaciones de las fichas |
| Catálogo del informe de Cáritas | Valores iniciales de género, situación habitacional, documentación, localidad de origen y motivo de asistencia |
| Eventos de calendario | Actividades recurrentes iniciales del Hogar de Día |

Lo demás se carga desde la aplicación, con un usuario Referente:
- las **actividades** (Actividades),
- los **valores de los campos del informe de Cáritas** (Configuración > Campos del informe),
- los **medicamentos del catálogo**, que se agregan al cargar el primer esquema de medicación.

En desarrollo también se cargan personas y fichas de ejemplo. Hay un juego de datos de demostración más grande, con 45 personas, en [`docs/demo`](./demo/README.md); sirve solo para demos y no se debe cargar en producción.

#### Activación de licencias

No aplica: todas las dependencias son libres o gratuitas.
- **SQL Server Express:** es gratuito, con un límite de 10 GB por base de datos. Si la base alguna vez supera ese tamaño, hay que pasar a la edición Standard, que sí requiere licencia, cambiando `MSSQL_PID`.
- **PDFsharp 6.2.4:** se distribuye con licencia MIT.

---

## 2. Manual de configuración

### 2.1 Archivos de configuración

| Archivo | Formato | Ubicación | Qué configura |
|---|---|---|---|
| `appsettings.json` | JSON | `backend/src/Vicaria.Api/` | Valores comunes a todos los entornos: tokens, CORS y respaldos |
| `appsettings.Development.json` | JSON | `backend/src/Vicaria.Api/` | Valores de desarrollo: conexión local y clave de prueba |
| `appsettings.Production.json` | JSON | `backend/src/Vicaria.Api/` | Valores de producción: nivel de logs; los secretos van por variables de entorno |
| `appsettings.{Entorno}.local.json` | JSON | `backend/src/Vicaria.Api/` | Secretos propios de cada equipo. **No se versiona** |
| `.env` | `CLAVE=valor` | `backend/` | Variables que lee Docker Compose (contraseña de SQL Server, clave JWT, ruta del frontend) |
| `docker-compose.yml` | YAML | `backend/` | Contenedores, puertos y variables de entorno de cada servicio |
| `nginx.local.conf` | Configuración de nginx | `frontend/` | Cómo se sirve el frontend, el reenvío de `/api` a la API, el tamaño máximo de los adjuntos y los encabezados de seguridad |
| `proxy.conf.json` | JSON | `frontend/` | Reenvío de `/api` cuando se usa `ng serve` |
| `angular.json` | JSON | `frontend/` | Configuraciones de compilación del frontend (`development` y `production`) |

#### Parámetros clave

| Parámetro (`appsettings`) | Variable de entorno equivalente | Valor por defecto | Descripción |
|---|---|---|---|
| `ConnectionStrings:VicariaDb` | `ConnectionStrings__VicariaDb` | — | Cadena de conexión a SQL Server |
| `Jwt:Key` | `Jwt__Key` | vacío (**obligatorio**) | Clave de firma de los tokens. Sin ella la API no arranca |
| `Jwt:Issuer` / `Jwt:Audience` | `Jwt__Issuer` / `Jwt__Audience` | `Vicaria.Api` / `Vicaria.Frontend` | Emisor y destinatario que se validan en cada token |
| `Jwt:ExpirationMinutes` | `Jwt__ExpirationMinutes` | 60 | Duración del token de acceso, en minutos |
| `Jwt:RefreshTokenExpirationDays` | `Jwt__RefreshTokenExpirationDays` | 7 | Duración del token de renovación, en días |
| `Cors:AllowedOrigins` | `Cors__AllowedOrigins__0`, `__1`… | `[]` | Orígenes que pueden llamar a la API desde otro dominio. Si el frontend y la API se publican en el mismo dominio, con nginx reenviando `/api`, se deja vacío |
| `ASPNETCORE_URLS` | — | `http://+:5000` | Dirección y puerto en que escucha la API |
| `ASPNETCORE_ENVIRONMENT` | — | `Production` | Entorno: `Development` o `Production` (ver 2.2) |
| `Backup:StorageProvider` | `Backup__StorageProvider` | `Local` | Dónde se guardan los respaldos: `Local` o `S3` |
| `Backup:RetentionCount` | `Backup__RetentionCount` | 30 | Cantidad de respaldos que se conservan; los más viejos se borran |
| `Backup:ScheduleHourUtc` | `Backup__ScheduleHourUtc` | 3 | Hora del respaldo diario, en UTC. Las 3 UTC son las 0 de Córdoba |

En .NET, el `:` de las claves de `appsettings` se escribe `__` (doble guion bajo) en las variables de entorno. Una variable de entorno siempre pisa el valor del archivo.

#### Rutas de datos

| Ruta (dentro del contenedor de la API) | Contenido |
|---|---|
| `/app/uploads/person_attachments` | Archivos adjuntos de las fichas |
| `/app/uploads/backups` | Respaldos `.bak` cuando `Backup:StorageProvider` es `Local` |
| `/var/opt/mssql` (contenedor de SQL Server) | Archivos de la base de datos |

En producción, estas tres rutas tienen que estar en **volúmenes de Docker** (ver 2.2) para que los datos no se pierdan al recrear un contenedor.

#### Puertos

| Puerto | Servicio | ¿Se expone a internet? |
|---|---|---|
| 80 / 443 | Frontend (nginx o el proxy con HTTPS) | Sí |
| 5000 | API | No: se accede por `/api` a través de nginx |
| 1433 / 1434 | SQL Server | **Nunca** |

### 2.2 Configuración por entorno

| | Desarrollo | Testing | Producción |
|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Development` | `Development` (stack de Docker), o el entorno de pruebas que levantan los tests de integración | `Production` |
| Base de datos | SQL Server en Docker, puerto 1434 | Un SQL Server temporal en Docker por clase de test (Testcontainers), que se borra al terminar | SQL Server en un volumen persistente |
| Usuarios y datos de prueba | Sí, se cargan al arrancar | Sí en el stack de Docker. Los tests de integración crean sus propios datos | No |
| Swagger (`/swagger`) | Habilitado | Habilitado | Deshabilitado |
| Nivel de logs | `Information` | `Information` | `Warning` |
| Secretos | `appsettings.Development.json` y `.env` con valores de prueba | Los genera cada test | Variables de entorno con valores propios; nunca en archivos versionados |
| Frontend | `ng serve` con recarga automática, o la imagen de nginx | Imagen de nginx (`ng build --configuration production`) | Imagen de nginx detrás de un proxy con HTTPS |

#### Desarrollo

Es la configuración que viene en el repositorio (`docker compose up -d --build` o `dotnet run` + `ng serve`). No requiere cambios.

#### Testing

- **Pruebas automatizadas:** `dotnet test` en `backend/` y `npx ng test --watch=false` en `frontend/`. Los tests de integración levantan su propio SQL Server con Docker, así que no tocan la base local.
- **Pruebas manuales y de QA:** se usa el stack de Docker en modo `Development`. Los scripts están en [`docs/qa/e2e`](./qa/e2e) y [`docs/qa/security`](./qa/security). Conviene correrlos sobre una base aparte de la de demostración, porque crean datos.

#### Producción

1. **Variables de entorno.** Crear `backend/.env` con valores propios, distintos de los de `.env.example`:

   ```bash
   MSSQL_SA_PASSWORD=<contraseña fuerte>
   JWT_KEY=<salida de: openssl rand -base64 48>
   FRONTEND_PATH=../frontend
   ```

2. **Archivo de Compose de producción.** Crear `backend/docker-compose.prod.yml`, que se combina con el principal y cambia el entorno, agrega volúmenes y saca los puertos internos de la red pública:

   ```yaml
   services:
     sql:
       ports: !reset []
       volumes:
         - vicaria-sql:/var/opt/mssql
       restart: unless-stopped
     api:
       environment:
         ASPNETCORE_ENVIRONMENT: Production
       ports: !reset []
       volumes:
         - vicaria-uploads:/app/uploads
       restart: unless-stopped
     web:
       ports: !override
         - "127.0.0.1:8080:80"
       restart: unless-stopped
   volumes:
     vicaria-sql:
     vicaria-uploads:
   ```

3. **Levantar el sistema:**

   ```bash
   docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
   ```

4. **HTTPS.** Delante del contenedor `web` va un proxy inverso con certificado. Puede ser Caddy, Traefik, la plataforma de despliegue (por ejemplo, Coolify) o nginx con Let's Encrypt. El proxy recibe el tráfico en los puertos 80 y 443 y lo reenvía a `127.0.0.1:8080`. Ejemplo con Caddy, que obtiene y renueva el certificado solo:

   ```
   vicaria.ejemplo.org {
       reverse_proxy 127.0.0.1:8080
   }
   ```

5. **Primer Referente.** Habilitarlo como se explica en 1.4.

6. **Respaldos y alertas.** Configurarlos como se explica en 2.3.

7. **Prueba de humo.** Verificar el funcionamiento como se explica en 1.3, desde la dirección pública.

**Actualizar a una versión nueva:**

```bash
git pull
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

Las migraciones nuevas se aplican solas cuando arranca la API. Antes de actualizar, conviene descargar el último respaldo desde **Respaldos**.

### 2.3 Integraciones externas

#### API REST propia

El frontend se comunica con la API por HTTP/JSON bajo la ruta `/api`. En desarrollo, la documentación de todos los endpoints está en `/swagger`.

#### Almacenamiento de respaldos (compatible con S3)

Todos los días, a la hora de `Backup:ScheduleHourUtc`, la API genera un respaldo completo de la base (`BACKUP DATABASE`) y conserva los últimos `Backup:RetentionCount`. Desde la pantalla **Respaldos**, un Referente puede ver el historial, descargar cada respaldo y generar uno en el momento.

Por defecto los respaldos quedan en el mismo servidor (`/app/uploads/backups`). Para guardarlos fuera del servidor, que es lo recomendado en producción, se usa cualquier servicio compatible con S3, como Backblaze B2, AWS S3 o MinIO:

| Variable | Ejemplo | Descripción |
|---|---|---|
| `Backup__StorageProvider` | `S3` | Activa el almacenamiento externo |
| `Backup__S3__ServiceUrl` | `https://s3.us-west-004.backblazeb2.com` | Dirección del servicio |
| `Backup__S3__Bucket` | `vicaria-respaldos` | Bucket donde se guardan los archivos |
| `Backup__S3__Region` | `us-west-004` | Región del bucket |
| `Backup__S3__AccessKey` | — | Identificador de la clave de acceso |
| `Backup__S3__SecretKey` | — | Clave secreta |
| `Backup__S3__UsePathStyle` | `true` | Formato de dirección `servicio/bucket`, que piden B2 y MinIO |

Estas variables se agregan en la sección `environment` del servicio `api`.

#### Correo saliente (SMTP)

Si un respaldo falla, la API envía un correo a los administradores. Se configura con estas variables:

| Variable | Descripción |
|---|---|
| `Backup__Smtp__Host` / `Backup__Smtp__Port` | Servidor SMTP y puerto (587 por defecto) |
| `Backup__Smtp__EnableSsl` | `true` para cifrar la conexión (STARTTLS) |
| `Backup__Smtp__User` / `Backup__Smtp__Password` | Credenciales de la cuenta que envía |
| `Backup__Smtp__From` | Remitente de los avisos |
| `Backup__Smtp__AdminEmails` | Destinatarios, separados por coma |

Si `Host` queda vacío no se envían correos. La falla igual queda registrada en el historial de respaldos y en las notificaciones internas de los Referentes.

#### Autenticación

La autenticación es propia del sistema: correo y contraseña, con tokens JWT. **No usa OAuth ni LDAP** ni proveedores externos de identidad.

#### Tareas automáticas

La API ejecuta en segundo plano, sin configuración adicional:

| Tarea | Qué hace |
|---|---|
| Inactivación por inasistencia | Todos los días pasa a *Inactivo* a quien no asiste hace 30 días, contados con la fecha de Córdoba, y avisa a los Referentes |
| Alerta de fichas sin actualizar | Avisa por notificación interna qué fichas llevan tiempo sin cambios |
| Alerta de revisión de medicación | Avisa qué esquemas de medicación necesitan revisión |
| Respaldo diario | Ver "Almacenamiento de respaldos" |

### 2.4 Seguridad

#### Roles y permisos

El acceso se controla por roles. Cada endpoint de la API valida el rol con `[Authorize(Roles = ...)]` y el frontend oculta lo que el rol no puede usar.

| Rol | Alcance |
|---|---|
| Referente | Acceso completo: usuarios y aprobación de cuentas, fichas, observaciones, calendario, colaboradores, visitas, asistencia, medicación, actividades, respaldos, informe de Cáritas y exportación de fichas |
| Directora de Casa de Convivencia | Fichas de personas *Residentes* (alta, edición, observaciones, adjuntos y evaluaciones psiquiátricas), medicación y su stock, gestión de visitas, y calendario y colaboradores en lectura |
| Coordinador de Casa de Convivencia | Igual que la Directora |
| Escucha | Lectura de fichas y del calendario, carga de observaciones y registro de la asistencia diaria. No accede a visitas, colaboradores, medicación ni informes |

Gestión de cuentas:
- Las cuentas nuevas quedan *Pendientes* hasta que un Referente las aprueba o las rechaza.
- Un Referente puede desactivar una cuenta y reactivarla, y cambiar su rol.
- Si se desactiva una cuenta o se le cambia el rol, sus sesiones abiertas se invalidan en el momento.

**Auditoría:** cada operación que modifica datos sensibles queda registrada con el usuario que la hizo, la acción, la entidad afectada y la fecha en UTC. Por ejemplo, aprobar o desactivar cuentas, crear o editar fichas, exportarlas, cargar medicación o cambiar el estado de una persona.

#### Contraseñas y sesiones

| Medida | Detalle |
|---|---|
| Almacenamiento de contraseñas | Hash con BCrypt; nunca se guarda la contraseña en texto plano |
| Bloqueo por intentos fallidos | Al quinto intento fallido la cuenta se bloquea durante 30 minutos |
| Token de acceso | JWT firmado con `Jwt:Key` (HMAC-SHA256), que dura 60 minutos |
| Renovación | Token de renovación de 7 días |
| Validación de cada pedido | Firma, emisor, destinatario, vencimiento, cuenta activa y versión del token |

#### Cifrado y certificados

- **En tránsito:**
  - En producción todo el tráfico va por HTTPS: el certificado lo gestiona el proxy inverso (ver 2.2) y la API redirige a HTTPS.
  - Dentro del servidor, la API y SQL Server se comunican por la red interna de Docker, sin exponerse.
  - La cadena de conexión usa `TrustServerCertificate=True` porque SQL Server corre en la misma red privada. Si la base se mueve a otro servidor, hay que instalarle un certificado válido y quitar esa opción.
- **Secretos:**
  - La contraseña de `sa`, la clave JWT y las credenciales de S3 y SMTP van solo en variables de entorno o en archivos que no se versionan (`.env`, `appsettings.*.local.json`).
  - La cadena de conexión de `appsettings.Production.json` es un valor de ejemplo y se tiene que pisar siempre con `ConnectionStrings__VicariaDb`.
- **Respaldos:**
  - Contienen datos personales y de salud.
  - En S3 hay que usar un bucket privado, con cifrado del lado del servidor activado.
  - Solo un Referente puede descargarlos.

#### Encabezados de seguridad del frontend

`nginx.local.conf` agrega estos encabezados a cada respuesta:

| Encabezado | Efecto |
|---|---|
| `Content-Security-Policy` | Solo permite cargar scripts, estilos e imágenes del propio sitio, más las fuentes de Google Fonts |
| `X-Frame-Options: SAMEORIGIN` | Impide que el sitio se muestre dentro de otro sitio (clickjacking) |
| `X-Content-Type-Options: nosniff` | Impide que el navegador interprete archivos con otro tipo |
| `Referrer-Policy` | No envía la dirección completa a otros sitios |
| `Permissions-Policy` | Desactiva cámara, micrófono y ubicación |

#### Firewall

Reglas mínimas en el servidor de producción, por ejemplo con `ufw` en Ubuntu:

```bash
sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw allow 22/tcp     # SSH, idealmente limitado a la IP de administración o a una VPN
sudo ufw allow 80/tcp     # HTTP (redirige a HTTPS)
sudo ufw allow 443/tcp    # HTTPS
sudo ufw enable
```

Los puertos 1433 y 1434 (SQL Server) y 5000 (API) **nunca** se abren a internet. Con el `docker-compose.prod.yml` de 2.2 ya no se publican.

Docker publica sus puertos por fuera de `ufw`, así que la protección real es no declararlos en `ports`, como hace ese archivo. Si el proveedor de nube tiene su propio firewall, como los grupos de seguridad o listas de seguridad, hay que aplicar las mismas reglas también ahí.
