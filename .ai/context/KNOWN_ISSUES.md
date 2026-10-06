# Known issues

Formato: severidad explícita, no párrafos narrativos. Severidad = impacto real dado que el sistema maneja datos de personas en situación de vulnerabilidad, no gravedad técnica en abstracto.

Actualizado al 2026-10-06 — ver [CURRENT_STATE.md](./CURRENT_STATE.md) para el detalle de qué se cerró desde la versión anterior de este archivo.

## CRÍTICA

- **Resuelto (2026-10-06): información sensible (abusos).** Ahora hay un control técnico: `SensitiveContentRules` (Application/Common) rechaza con 400 las frases inequívocas de abuso/violación en observaciones, notas y motivo de ingreso de la ficha, y en la historia de vida; los formularios muestran el recordatorio. Es una red de seguridad con lista conservadora de frases (no bloquea "abuso de sustancias"), no reemplaza el acuerdo verbal. Ver [CONSTRAINTS.md](./CONSTRAINTS.md).
- **Resuelto (2026-10-06): CI.** `.github/workflows/ci.yml` corre build + tests de backend (unit + integración) y tests + build de producción del frontend en cada push/PR a `main`/`dev`. **Pendiente de hacer en GitHub (no se puede desde el repo):** activar branch protection en `main`/`dev` exigiendo los checks `Backend` y `Frontend` y al menos una revisión.

## ALTA

- **Resuelto (2026-10-06): rename `Hogar` → `Centro Barrial` en LifeStory.** Entidad, DTOs, rutas (`before-centro-barrial`, `in-centro-barrial`, `after-centro-barrial`), columnas y frontend. Migración `RenameHogarToCentroBarrial` usa `RenameColumn`/`RenameIndex` (no pierde datos). Deja de bloquear el pase de `dev` a `main`.
- **Resuelto (2026-10-05): jobs de inactividad.** Se decidió dejar solo `AttendanceInactivityService` (por asistencia, SCRUM-135).

## MEDIA

- **Resuelto (2026-10-06): expiración de sesión por inactividad.** El frontend cierra la sesión tras 30 min sin actividad (`IdleTimeoutService`, compartido entre pestañas) y renueva el JWT con el refresh token ante un 401 (`authInterceptor`); si el refresh falla, cierra sesión y avisa en el login. La rama `feature/SCRUM-96-inactivity-timeout` quedó obsoleta.
- **Resuelto (2026-10-06): versión de Angular.** `/PROJECT.md` ahora dice Angular 22, igual que el código.
- **Resuelto (2026-10-06): RBAC documentado vs. real.** `/PROJECT.md` describe los 4 roles; Directora y Coordinador solo acceden a Residentes en backend (`IPersonAccessService` + `[DirectorResidentsOnly]`), y el mapa de permisos del frontend se alineó con el backend (solo el Referente gestiona usuarios). Los `permissionGuard` de rutas están activos.
- **Resuelto (2026-10-05): rol `DirectoraDeCasaConvivencia`.**

## BAJA

- **~24 branches `feature/*`/`fix/*` en remoto sin PR abierta.** Decisión explícita del usuario: no tocar. Borrarlas es destructivo y queda como tarea manual del equipo, ver [OPEN_QUESTIONS.md](./OPEN_QUESTIONS.md).
- **Tokens en `localStorage`.** Quedan ahí (riesgo si algún día entra XSS). Mitigado con CSP + `X-Frame-Options` en nginx, escape de Angular y rechazo de `<`/`>` en nombres. Pasarlos a cookie `HttpOnly` exige CSRF + cambios en backend y nginx; pendiente de decisión del equipo.
- **Recuperación de contraseña sin correo.** `/auth/forgot-password` ahora explica que se resuelve con un Referente; no existe flujo de reseteo ni envío de mails. Requiere definir un flujo (reset por Referente o SMTP).
- **Refresh token verificado contra todos los usuarios con BCrypt** (`AuthService.RefreshTokenAsync`): O(n) por refresh. Aceptable con pocos usuarios; si crece, identificar el refresh token con el id de usuario.
- **Evaluación psiquiátrica sin endpoint de carga** (EP-12, Sprint 5): hoy solo se inserta por seed/SQL; los TC que la requieren (TC-21) se prueban así.
- **`docker-compose.yml` local:** sus dos bugs de 2026-09-23 siguen resueltos; si se edita el proxy o el Dockerfile de producción, revisar si el `.local` necesita el mismo cambio. El `nginx.conf` ahora tiene headers de seguridad y CSP: mantener ambos archivos sincronizados.

## Resueltos el 2026-10-06 (QA manual Sprint 1 y 2), dejados de referencia

- TC-10: alerta diaria de ficha sin observaciones en 30 días (`StaleRecordAlertService`) + click en la notificación abre la ficha.
- Directora/Coordinador accedían por ID a personas ambulatorias (observaciones, historia, timeline, asistencia, conteo del filtro): ahora 404.
- Egreso de estadía solo permitía Referente; ahora Referente, Directora y Coordinador (como indica PROJECT.md).
- CSV de observaciones con inyección de fórmulas; un Escucha podía marcar como leídas notificaciones de otro rol; `page <= 0` daba 500; `authGuard` comentado en `/dashboard`.
- Botón "Exportar Ficha" sin acción: ahora imprime / guarda como PDF desde el navegador.
- Contraste del azul primario (`#6B8DF5` → `#4167D9`, 5.0:1) y responsive de la ficha en celular.
