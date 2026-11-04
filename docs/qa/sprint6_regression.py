"""Regresión final del Sprint 6 (03 y 04/11/2026) sobre la versión que se entrega en main.
Lo usan build_test_cycles_xlsx.py (pestaña "Ciclo - Sprint 6") y build_test_report_docx.py.
"""
from datetime import date

RUN_DATE = date(2026, 11, 4)

SUITES = {"backend_unit": 364, "backend_integration": 270, "frontend_tests": 181}

# Defectos encontrados en la regresión, a continuación de los 36 de la hoja "Defectos y pendientes".
# (#, tipo, descripción, origen, estado, severidad)
DEFECTS = [
    ("37", "Defecto", "El día de \"hoy\" se calculaba en UTC: entre las 21 y las 24 h de Córdoba, corregir la asistencia del día pedía justificación como si fuera un día cerrado, y 8 pruebas de integración fallaban en ese horario.",
     "Regresión Sprint 6", "Corregido", "Mayor"),
    ("38", "Defecto", "Las horas de última modificación y de auditoría se mostraban en UTC, 3 horas adelantadas.",
     "Regresión Sprint 6", "Corregido", "Menor"),
    ("39", "Defecto", "El frontend pedía datos que el rol no tiene permitidos (historial de asistencia con Escucha, historia de vida con Coordinador, sincronización del calendario con Directora y Escucha) y el servidor respondía 403.",
     "Regresión Sprint 6", "Corregido", "Menor"),
    ("40", "Defecto", "En el celular, la lista de fichas cortaba la columna de acciones y no se podía abrir ni editar una ficha.",
     "Regresión Sprint 6", "Corregido", "Mayor"),
    ("41", "Defecto", "Las estadísticas y el PDF mensual de asistencia decían \"Centro de Día\" en lugar de \"Hogar de día\", como el resto del sistema.",
     "Regresión Sprint 6", "Corregido", "Menor"),
    ("42", "Defecto", "En el celular, los márgenes laterales eran demasiado anchos, el perfil tenía bordes duplicados y los días vacíos del calendario ocupaban demasiado alto.",
     "Regresión Sprint 6", "Corregido", "Menor"),
    ("43", "Defecto", "La agenda de medicación mostraba la fecha con mayúsculas de más (\"4 De Noviembre De 2026\").",
     "Regresión Sprint 6", "Corregido", "Menor"),
    ("44", "Defecto", "La página de estadísticas de asistencia no tenía margen lateral: el contenido quedaba pegado al menú y el botón Exportar a PDF tocaba el borde.",
     "Regresión Sprint 6", "Corregido", "Menor"),
]

STEPS_SUITE = "1. Levantar el stack limpio en Docker\n2. Ejecutar la suite indicada en el título"

# (id, componente, título, pasos, esperado, resultado real, estado, bugs, comentario)
CASES = [
    ("S6-01", "Pruebas automatizadas", "Backend: pruebas unitarias (xUnit)", "1. dotnet test tests/Vicaria.UnitTests",
     "Todas las pruebas pasan", "364 de 364 pasan.", "PASÓ", "DEF-37, DEF-38", "Se agregaron pruebas para DEF-37 y DEF-38."),
    ("S6-02", "Pruebas automatizadas", "Backend: pruebas de integración contra SQL Server", "1. dotnet test tests/Vicaria.IntegrationTests",
     "Todas las pruebas pasan", "270 de 270 pasan, también entre las 21 y las 24 h.", "PASÓ", "DEF-37", "En la primera corrida fallaron 8 por DEF-37; corregido y re-ejecutado."),
    ("S6-03", "Pruebas automatizadas", "Frontend: pruebas unitarias (Vitest) y build de producción", "1. ng test --watch=false\n2. ng build",
     "Todas las pruebas pasan y el build no da advertencias", "181 de 181 pasan; build sin advertencias.", "PASÓ", None, None),
    ("S6-04", "API", "Re-ejecución de las pruebas de API del Sprint 4", STEPS_SUITE,
     "Las 21 pruebas pasan", "21 de 21 pasan (e2e/sprint4-api-flows.mjs).", "PASÓ", None, None),
    ("S6-05", "API", "Re-ejecución de las pruebas de API del Sprint 5", STEPS_SUITE,
     "Las 65 pruebas pasan", "65 de 65 pasan (e2e/sprint5-api-flows.mjs).", "PASÓ", None, None),
    ("S6-06", "Seguridad y carga", "Re-ejecución de las pruebas de seguridad y carga", STEPS_SUITE,
     "Las 17 pruebas pasan", "17 de 17 pasan (security/api-security-and-load.mjs).", "PASÓ", None, None),
    ("S6-07", "Interfaz", "Re-ejecución de los flujos de interfaz del Sprint 1 y 2", STEPS_SUITE,
     "Los 35 flujos pasan", "35 de 35 pasan (e2e/sprint1-2-ui-flows.mjs).", "PASÓ", None, "El script de TC-11 ahora espera a que cargue el perfil antes de leerlo."),
    ("S6-08", "Interfaz", "Re-ejecución de la integración front-back del Sprint 4", STEPS_SUITE,
     "Los 24 flujos pasan", "24 de 24 pasan (e2e/sprint4-wired-flows.mjs).", "PASÓ", "DEF-41", None),
    ("S6-09", "Interfaz", "Re-ejecución de los casos de interfaz del Sprint 4 sobre una base nueva", STEPS_SUITE,
     "Los casos que no dependen de datos fijos pasan", "45 de 48 pasan (e2e/sprint4-ui-cases.mjs).", "PASÓ", "DEF-37", "Los 3 restantes están en S6-10."),
    ("S6-10", "Interfaz", "TC-153, TC-154 y TC-193 sobre una base nueva", STEPS_SUITE,
     "Los casos pasan", "No se pudieron ejecutar: usan fechas fijas (15 al 19/10) y un residente sin evaluación vigente, datos que solo existían en la base del ciclo del Sprint 4.",
     "BLOQUEADO", None, "Pasaron en el ciclo del Sprint 4 (20/10). No es un defecto del sistema."),
    ("S6-11", "Interfaz", "Re-ejecución de los casos de interfaz del Sprint 5 sobre una base nueva", STEPS_SUITE,
     "Los casos que no dependen del respaldo diario pasan", "45 de 50 pasan (e2e/sprint5-ui-cases.mjs).", "PASÓ", None, "Los 5 restantes están en S6-12."),
    ("S6-12", "Interfaz", "TC-246, TC-247, TC-248, TC-250 y TC-251 sobre una base nueva", STEPS_SUITE,
     "Los casos pasan", "No se pudieron ejecutar: necesitan que el proceso de respaldo diario ya haya corrido, y en una base recién creada todavía no corrió.",
     "BLOQUEADO", None, "Pasaron en el ciclo del Sprint 5 (27/10). No es un defecto del sistema."),
    ("S6-13", "Usabilidad / UI", "Revisión visual de todas las pantallas por rol, en escritorio y en celular",
     "1. Iniciar sesión con cada uno de los 4 roles\n2. Abrir cada pantalla a 1366 px y a 390 px de ancho\n3. Revisar la consola, las respuestas de la API y el desborde horizontal",
     "Ninguna pantalla muestra errores, respuestas 4xx/5xx ni desborde",
     "76 pantallas revisadas sin errores de consola, sin respuestas 4xx/5xx y sin desborde.", "PASÓ", "DEF-39, DEF-40, DEF-42, DEF-43, DEF-44",
     "En la primera revisión aparecieron DEF-39, DEF-40, DEF-42, DEF-43 y DEF-44; corregidos y revisados de nuevo."),
    ("S6-14", "Asistencia", "Corregir la asistencia de hoy después de las 21 h",
     "1. A las 21:30 de Córdoba, abrir Asistencia con la fecha de hoy\n2. Cambiar el conteo de personas sin ficha y guardar",
     "Se guarda sin pedir justificación", "Se guarda sin pedir justificación.", "PASÓ", "DEF-37", None),
    ("S6-15", "Fichas", "La hora de última modificación se ve en hora de Córdoba",
     "1. Editar una ficha a las 21:51\n2. Abrir el perfil",
     "El perfil muestra 21:51", "El perfil muestra 21:51.", "PASÓ", "DEF-38", None),
    ("S6-16", "Fichas", "Abrir y editar una ficha desde el celular",
     "1. Abrir Fichas a 390 px de ancho\n2. Tocar Ver y Editar en una fila",
     "Las acciones se ven y responden", "Las acciones se ven y responden.", "PASÓ", "DEF-40", None),
]
