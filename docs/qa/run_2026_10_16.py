"""Resultados de la corrida de QA del 2026-10-16 (UI en navegador real de Sprint 1-2 + seguridad y carga).

Lo usan build_test_plan_xlsx.py, build_test_cycles_xlsx.py y build_test_report_docx.py para que los tres
documentos cuenten lo mismo. Scripts: e2e/sprint1-2-ui-flows.mjs y security/api-security-and-load.mjs.
Evidencia: e2e/evidence/*.png y security/security-results.json.
"""
import json
from pathlib import Path

HERE = Path(__file__).parent
RUN_DATE = "2026-10-16"
RUNNER = "Mateo Pavoni"
UI_SCRIPT = "docs/qa/e2e/sprint1-2-ui-flows.mjs"
SEC_SCRIPT = "docs/qa/security/api-security-and-load.mjs"

# Defectos nuevos de esta corrida. Se agregan al final de la hoja "Defectos y pendientes" (#22 en adelante).
# (tipo, descripción, origen, estado, detalle, severidad)
NEW_DEFECTS = [
    ("Defecto", "Cuenta bloqueada: el login navegaba a /auth/blocked, una ruta que no existe, y dejaba la pantalla en blanco sin ningún mensaje.",
     "SCRUM-84 / 94 (frontend)", "Corregido en dev",
     "login.component.ts ahora muestra el aviso de bloqueo en el propio formulario. Re-ejecutado OK en navegador.", "Mayor"),
    ("Defecto", "El quinto intento fallido de login bloqueaba la cuenta pero respondía 401 'Credenciales inválidas': no informaba el bloqueo hasta el sexto intento.",
     "SCRUM-84 (AuthService)", "Corregido en dev",
     "AuthService.LoginAsync devuelve AccountLocked en el intento que dispara el bloqueo; test unitario nuevo.", "Menor"),
    ("Defecto", "Una cuenta desactivada que intentaba ingresar veía 'No pudimos iniciar sesión. Probá de nuevo en unos minutos.' (el frontend no mapeaba el estado Inactive de la API).",
     "SCRUM-20 (frontend)", "Corregido en dev",
     "AuthService.mapError distingue Inactive y Rejected; mensajes específicos y tests nuevos.", "Menor"),
    ("Defecto", "Al guardar una ficha con fecha futura la UI mostraba el mensaje genérico 'Ocurrió un error al guardar la ficha.' aunque la API devolvía 'La fecha de ingreso no puede ser futura.'.",
     "SCRUM-5 (frontend)", "Corregido en dev",
     "Las pantallas de alta y edición usan extractApiError y muestran el motivo de validación.", "Menor"),
    ("Defecto", "El rol Escucha veía el botón 'Editar ficha' en el detalle y el ícono de editar en el listado (la ruta /edit sí lo rechazaba con acceso denegado).",
     "SCRUM-13 / 64 (frontend)", "Corregido en dev",
     "Ambos controles se muestran solo con el permiso fichas.edit.", "Mayor"),
    ("Defecto", "Una observación con solo espacios habilitaba el botón Guardar; el backend la rechazaba con 400.",
     "SCRUM-9 (frontend)", "Corregido en dev",
     "El formulario valida el largo mínimo sobre el texto recortado.", "Menor"),
    ("Pregunta abierta", "La API acepta crear dos fichas con el mismo DNI (201 y 201). Los colaboradores sí rechazan DNI duplicado (409).",
     "SCRUM-5 / EP-01", "Abierta", "Confirmar si el DNI debe ser único en fichas antes de agregar la regla.", "Menor"),
    ("Pregunta abierta", "La API permite crear fichas al Coordinador de Casa de Convivencia (POST api/social-records), pero la UI le deniega /fichas/crear.",
     "SCRUM-13 / 64", "Abierta", "Se suma a la pregunta #20 sobre los permisos del Coordinador.", "Menor"),
    ("Defecto", "Los PDF del resumen mensual de asistencia y del historial de medicación respondían 500: PDFsharp 6.5.0 exige una licencia y la imagen de la API no tenía ninguna fuente instalada.",
     "SCRUM-225 / 241 (backend)", "Corregido en dev",
     "PDFsharp 6.2.4, resolvedor de fuentes (Arial en Windows, Liberation Sans en Linux), fuentes en la imagen Docker y tests unitarios y de integración nuevos.", "Mayor"),
    ("Defecto", "El panel de estadísticas de asistencia se rompía (NG02100) cuando la lista de inactivos incluía a alguien que nunca asistió: la fecha llegaba vacía al formato de fecha.",
     "SCRUM-226 (frontend)", "Corregido en dev",
     "La lista muestra 'nunca registró asistencia' cuando no hay fecha; verificado en navegador.", "Menor"),
    ("Pregunta abierta", "El backend solo registra asistencia de Hogar de día y Casa de Convivencia; las actividades configurables (SCRUM-57) existen en el catálogo pero no se pueden usar al registrar asistencia.",
     "SCRUM-57 / 216", "Abierta", "Definir si la asistencia por actividad se guarda con el id de la actividad del catálogo (cambio en el modelo de Attendance).", "Mayor"),
    ("Pregunta abierta", "No existe forma de quitar a una persona ya registrada en un día, ni de reactivar un esquema de medicación desactivado, ni el backend informa cuántos esquemas están pendientes de stock en la agenda.",
     "SCRUM-54 / 59 / 60", "Abierta", "El frontend lo informa con un mensaje; confirmar si se necesitan esos endpoints.", "Menor"),
    ("Pregunta abierta", "La agenda del día se genera al registrar la asistencia: un esquema cargado después de la asistencia no tiene tomas ese día.",
     "SCRUM-236", "Abierta", "Confirmar si al cargar un esquema debe generarse la toma del día para quienes ya asistieron.", "Menor"),
    ("Defecto", "Una actividad del calendario desactivada vuelve a aparecer: al abrir la pantalla de actividades se sincroniza con el calendario y se crea una actividad nueva con el mismo nombre, activa, en lugar de respetar la desactivada.",
     "SCRUM-57 (backend: sincronizar-calendario)", "Abierto",
     "Visto en la ejecución en navegador del 20/10 (TC-165): se desactivó 'Almuerzo' y el auditor registró una sincronización dos segundos después con una 'Almuerzo' nueva. La sincronización debería ignorar o reactivar la existente.", "Menor"),
    ("Pregunta abierta", "'Exportar PDF' del historial de medicación abre la impresión del navegador (window.print); el endpoint del backend api/persons/{id}/medication-history/pdf devuelve un PDF válido pero la pantalla no lo usa.",
     "SCRUM-241 / 242", "Abierta", "Confirmar si la exportación debe descargar el PDF del backend (con datos de la persona y filtros) en lugar de imprimir la pantalla.", "Menor"),
    ("Pregunta abierta", "No existe stock de ropa en el backend: la independencia entre stock de medicamentos y de ropa (TC-188) solo se pudo verificar mostrando que el inventario de medicamentos no lista otro tipo de ítems.",
     "SCRUM-71", "Abierta", "Confirmar si el stock de ropa está fuera del alcance del Sprint 4.", "Menor"),
]

# Defectos de la hoja del plan que cambiaron de estado: el menú ya no tiene 4 ítems rotos sino uno (Informes).
DEFECT_5_NOTE = ("Los ítems Inicio, Asistencia y Medicación ya funcionan; solo 'Informes' apunta a /inicio, una ruta sin pantalla (página vacía). "
                 "Verificado en navegador con los 4 roles el 2026-10-16.")

# TC existentes ejecutados en navegador real el 16/10. id -> (resultado real, defectos vinculados, falló antes de la corrección)
UI_RESULTS = {
    "TC-01": ("Registro desde la UI: la cuenta queda en /auth/pending-approval y un login con esa cuenta vuelve a la pantalla de pendiente.", "", False),
    "TC-02": ("Un Referente aprueba la cuenta desde Usuarios > Pendientes eligiendo el rol; desaparece de Pendientes y la cuenta ingresa al dashboard.", "", False),
    "TC-03": ("Con clave incorrecta se muestra 'Correo o contraseña incorrectos.', no se ingresa y se limpia la contraseña.", "", False),
    "TC-04": ("Primera ejecución: el 5º intento respondía 401 sin avisar del bloqueo y, con la clave correcta, el login navegaba a /auth/blocked (ruta inexistente, pantalla en blanco). "
              "Tras la corrección: el 5º intento devuelve 403 y muestra 'Alcanzaste el máximo de intentos permitidos: tu cuenta quedó bloqueada por 30 minutos.'; con la clave correcta la cuenta sigue bloqueada.",
     "DEF-22, DEF-23", True),
    "TC-07": ("Primera ejecución: la cuenta desactivada no ingresaba pero veía 'No pudimos iniciar sesión. Probá de nuevo en unos minutos.'. "
              "Tras la corrección muestra 'Tu cuenta está desactivada. Contactá a un Referente para reactivarla.'.", "DEF-24", True),
    "TC-08": ("Inactivar desde Usuarios mueve la cuenta a Inactivos; al reactivarla vuelve a ingresar al dashboard.", "", False),
    "TC-09": ("Al registrarse una cuenta, el Referente ve la notificación en la campana y el contador de no leídas aumenta.", "", False),
    "TC-11": ("Alta mínima (nombre, apellido, DNI): la ficha se crea y aparece en el listado y el detalle. Una ficha vacía muestra los obligatorios y un DNI con letras muestra 'El DNI solo puede tener números, sin puntos ni espacios.'.", "", False),
    "TC-12": ("Primera ejecución: Escucha veía 'Editar ficha' en el detalle y el ícono de editar en el listado. Tras la corrección no los ve, no ve 'Nueva ficha' y /edit lo redirige a acceso denegado.", "DEF-26", True),
    "TC-13": ("Buscar 'jose<n> nunez' encuentra 'José<n> Núñez'; la búsqueda por DNI la encuentra y una búsqueda sin coincidencias no lista fichas.", "", False),
    "TC-14": ("Primera ejecución: con fecha futura la UI mostraba el mensaje genérico 'Ocurrió un error al guardar la ficha.'. Tras la corrección muestra el motivo de la API ('...no puede ser futura.') y no guarda.", "DEF-25", True),
    "TC-15": ("El filtro por DNI del listado se aplica sin errores de API y muestra el filtro activo.", "", False),
    "TC-16": ("Se registra una observación con categoría (6 disponibles) y aparece en la línea de tiempo con su autor.", "", False),
    "TC-17": ("Filtrar por categoría deja solo las observaciones de esa categoría y el filtro por autor responde sin errores.", "", False),
    "TC-18": ("'Exportar informe' descarga observaciones_<id>.csv.", "", False),
    "TC-20": ("La historia de vida muestra las 3 etapas y se agrega una entrada en la primera.", "", False),
    "TC-26": ("En la fila de la propia cuenta, 'Reasignar Rol' e 'Inactivar' están deshabilitados.", "", False),
    "TC-31": ("Login vacío: se muestran 'El correo electrónico es obligatorio.' y 'La contraseña es obligatoria.' y no se envía la petición.", "", False),
    "TC-32": ("Registrar un correo existente muestra 'Ya existe un usuario registrado con ese email.' y permanece en el formulario.", "", False),
    "TC-34": ("La edición carga el nombre guardado y los cambios se guardan y se ven en el detalle.", "", False),
    "TC-37": ("Con el texto vacío el botón Guardar está deshabilitado y el modal sigue abierto.", "", False),
    "TC-38": ("Primera ejecución: con solo espacios el botón quedaba habilitado y la API respondía 400. Tras la corrección el botón queda deshabilitado.", "DEF-27", True),
    "TC-39": ("En 390 px de ancho no hay desborde horizontal en el inicio ni en el listado de fichas.", "", False),
    "TC-40": ("En pantalla chica el menú se abre con el botón y se oculta al elegir una opción.", "", False),
}


def _loads():
    f = HERE / "security" / "security-results.json"
    if not f.exists():
        return {}
    return {l["name"]: l for l in json.loads(f.read_text(encoding="utf8")).get("loads", [])}


def _perf(name, label):
    l = _loads().get(name)
    if not l:
        return f"{label}: sin datos de la corrida."
    return (f"{label}: {l['concurrentes']} usuarios concurrentes, {l['requests']} pedidos, {l['errores']} errores, "
            f"{l['rps']} pedidos/s, p50 {l['p50']} ms, p95 {l['p95']} ms, máximo {l['max']} ms.")


# Casos nuevos transversales de seguridad y rendimiento: (id interno, título, descripción, pasos, script, resultado real)
SEC_CASES = [
    ("SEC-01", "Los endpoints protegidos exigen sesión", "Sin token, la API no entrega datos.",
     "1. GET de 7 rutas protegidas (fichas, listado, usuarios, notificaciones, colaboradores, calendarios) sin Authorization  →  Todas responden 401",
     SEC_SCRIPT, "Las 7 rutas respondieron 401."),
    ("SEC-02", "Autorización por rol en la API", "Cada rol solo accede a lo que le corresponde.",
     "1. Escucha intenta POST api/social-records  →  403\n2. Escucha, Coordinador y Directora piden api/auth/users/active  →  403",
     SEC_SCRIPT, "Crear ficha como Escucha: 403. Listar usuarios como Escucha, Coordinador y Directora: 403."),
    ("SEC-03", "Un token manipulado se rechaza", "Un JWT con el rol alterado, sin firma válida o malformado no da acceso.",
     "1. Cambiar el rol del payload a Referente conservando la firma  →  401\n2. Token con alg=none  →  401\n3. Token malformado  →  401",
     SEC_SCRIPT, "Firma alterada, alg=none y token malformado: 401 en los tres."),
    ("SEC-04", "Inyección SQL en la búsqueda", "La búsqueda está parametrizada.",
     "1. Buscar \"' OR 1=1 --\" y \"'; DROP TABLE Persons; --\"  →  Sin error 500 y sin devolver todo el listado",
     SEC_SCRIPT, "Sin errores 500; la búsqueda devolvió 4 de 15 fichas por coincidencia de texto (no un OR 1=1) y las tablas siguen intactas."),
    ("SEC-05", "El login no revela si el correo existe", "Mismo mensaje para correo inexistente y clave incorrecta.",
     "1. Login con correo inexistente  →  401 'Credenciales inválidas.'\n2. Login con correo existente y clave incorrecta  →  Misma respuesta",
     SEC_SCRIPT, "Ambas respuestas fueron 401 con el mismo cuerpo."),
    ("SEC-06", "Política de contraseñas", "El registro rechaza contraseñas débiles.",
     "1. Registrar con la contraseña '123'  →  400",
     SEC_SCRIPT, "Respondió 400."),
    ("SEC-07", "Asignación masiva en el registro", "Enviar role, roleId y status en el registro no otorga privilegios.",
     "1. Registrar enviando role=Referente, roleId y status=Active  →  La cuenta queda Pendiente\n2. Intentar loguear  →  403 Pending",
     SEC_SCRIPT, "Registro 201 y login 403 con estado Pending: los campos extra se ignoran."),
    ("SEC-08", "XSS: la API no cae ante HTML en los datos", "Un nombre con HTML no produce errores del servidor.",
     "1. Crear una ficha con nombre '<img src=x onerror=...>'  →  Rechazo 4xx o guardado sin error 500",
     SEC_SCRIPT, "La API respondió 400 (nombre inválido)."),
    ("SEC-09", "XSS almacenado en una observación (UI)", "El texto con HTML/script se muestra como texto y no ejecuta código.",
     "1. Registrar una observación con <img onerror> y <script>  →  Se muestra el texto literal\n2. Verificar que no se ejecutó código en la página  →  window.__xss no está definido",
     UI_SCRIPT, "La observación se muestra como texto y no se ejecutó ningún script."),
    ("SEC-10", "Cabeceras de seguridad del frontend", "El servidor web envía cabeceras defensivas.",
     "1. Pedir /auth y revisar cabeceras  →  X-Content-Type-Options, X-Frame-Options, Content-Security-Policy y Referrer-Policy presentes",
     SEC_SCRIPT, "Las cuatro cabeceras están presentes."),
    ("SEC-11", "La API no expone la versión del servidor", "Sin X-Powered-By ni versión en Server.",
     "1. Pedir una ruta de la API y revisar cabeceras  →  Sin X-Powered-By; Server sin número de versión",
     SEC_SCRIPT, "Server: Kestrel, sin X-Powered-By."),
    ("SEC-12", "Payload de 2 MB y JSON malformado", "La API rechaza entradas anómalas sin romperse ni filtrar trazas.",
     "1. POST con un nombre de 2 MB  →  4xx\n2. POST con JSON incompleto  →  400 sin stack trace",
     SEC_SCRIPT, "2 MB: 400. JSON malformado: 400 sin trazas."),
    ("SEC-13", "Ids inexistentes o mal formados", "Sin errores 500 ni datos de otros registros.",
     "1. GET de una ficha con un GUID inexistente  →  404\n2. GET con un id mal formado ('1;DROP')  →  4xx",
     SEC_SCRIPT, "Inexistente: 404. Mal formado: 404."),
    ("PERF-01", "Carga: listado de fichas", "50 usuarios concurrentes consultando el listado.",
     "1. 500 pedidos GET api/social-records/list con 50 en paralelo  →  Sin errores y p95 < 2000 ms",
     SEC_SCRIPT, _perf("GET /api/social-records/list", "Listado de fichas")),
    ("PERF-02", "Carga: notificaciones", "50 usuarios concurrentes consultando notificaciones.",
     "1. 500 pedidos GET api/notifications con 50 en paralelo  →  Sin errores y p95 < 2000 ms",
     SEC_SCRIPT, _perf("GET /api/notifications", "Notificaciones")),
    ("PERF-03", "Carga: calendario general", "30 usuarios concurrentes consultando el calendario.",
     "1. 300 pedidos GET api/general-calendar-events con 30 en paralelo  →  Sin errores y p95 < 2000 ms",
     SEC_SCRIPT, _perf("GET /api/general-calendar-events", "Calendario general")),
    ("PERF-04", "Carga: inicio de sesión", "20 usuarios concurrentes iniciando sesión (bcrypt).",
     "1. 100 pedidos POST api/auth/login con 20 en paralelo  →  Sin errores y p95 < 3000 ms",
     SEC_SCRIPT, _perf("POST /api/auth/login (bcrypt)", "Login")),
]
SEC_FIRST_ID = 195  # TC-195 en adelante
SEC_NOTE = "Entorno local en Docker (SQL Server Express limitado); las cifras sirven de referencia, no de dimensionamiento de producción."

# Cifras de las suites automatizadas de la corrida (se completan con los resultados reales de `dotnet test` y `ng test`)
SUITES = {"backend_unit": 318, "backend_integration": 258, "frontend_tests": 114, "frontend_files": 29,
          "ui_checks": 35, "security_checks": 13, "load_checks": 4}
