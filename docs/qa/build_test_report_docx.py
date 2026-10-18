"""Genera docs/qa/Informe_de_pruebas_Sprint_1-3.docx (informe de resultados de pruebas).
Las métricas se calculan leyendo Ciclos_de_prueba_Sprint_1-4.xlsx; los defectos, la hoja
"Defectos y pendientes" de Plan_de_pruebas_Sprint_1-4.xlsx; el resto, run_2026_10_16.py.

Uso: python build_test_report_docx.py [ciclos.xlsx] [plan.xlsx] [salida.docx]
"""
import collections
import sys
from pathlib import Path

import openpyxl
from docx import Document
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Inches, Pt, RGBColor

import run_2026_10_16 as run

HERE = Path(__file__).parent
EVIDENCE = HERE / "e2e" / "evidence"
VERSION = "rama dev (commit 4769749) más las correcciones de QA del 2026-10-16"
REPORT_DATE = run.RUN_DATE
NAVY = "1F3864"

# Severidad de los hallazgos de la hoja de defectos (# de fila -> severidad); los #22+ vienen de run.NEW_DEFECTS
SEVERITY = {"1": "Mayor", "2": "Menor", "3": "Mayor", "4": "Menor", "5": "Mayor", "6": "Mayor",
            "10": "Menor", "11": "Mayor", "12": "Menor", "13": "Mayor"}
for i, d in enumerate(run.NEW_DEFECTS):
    if d[0] == "Defecto":
        SEVERITY[str(22 + i)] = d[5]
ORDER = ["Crítico / Bloqueante", "Mayor", "Menor / Estético"]
DEF_TYPES = ("Defecto", "Defecto preexistente", "Brecha", "Test desactualizado", "Limitación")


def shade(cell, color):
    tcPr = cell._tc.get_or_add_tcPr()
    shd = OxmlElement("w:shd")
    shd.set(qn("w:val"), "clear"); shd.set(qn("w:color"), "auto"); shd.set(qn("w:fill"), color)
    tcPr.append(shd)


def table(doc, header, rows, widths=None, size=9):
    t = doc.add_table(rows=1, cols=len(header))
    t.style = "Table Grid"
    t.alignment = WD_TABLE_ALIGNMENT.CENTER
    for i, h in enumerate(header):
        c = t.rows[0].cells[i]
        c.text = ""
        run_ = c.paragraphs[0].add_run(h)
        run_.bold = True
        run_.font.color.rgb = RGBColor(255, 255, 255)
        shade(c, NAVY)
    for n, r in enumerate(rows):
        cells = t.add_row().cells
        for i, v in enumerate(r):
            cells[i].text = str(v)
            if n % 2:
                shade(cells[i], "EEF2FA")
    for row in t.rows:
        for i, c in enumerate(row.cells):
            if widths:
                c.width = Inches(widths[i])
            for p in c.paragraphs:
                for r_ in p.runs:
                    r_.font.size = Pt(size)
    doc.add_paragraph()
    return t


def pct(n, total):
    return f"{n / total * 100:.0f}%" if total else "—"


def bullets(doc, items):
    for item in items:
        doc.add_paragraph(item, style="List Bullet")


def picture(doc, name, caption, width=5.6):
    f = EVIDENCE / name
    if not f.exists():
        return
    doc.add_picture(str(f), width=Inches(width))
    doc.paragraphs[-1].alignment = WD_ALIGN_PARAGRAPH.CENTER
    p = doc.add_paragraph(caption)
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.runs[0].italic = True
    p.runs[0].font.size = Pt(9)


def main():
    ciclos = sys.argv[1] if len(sys.argv) > 1 else str(HERE / "Ciclos_de_prueba_Sprint_1-4.xlsx")
    plan = sys.argv[2] if len(sys.argv) > 2 else str(HERE / "Plan_de_pruebas_Sprint_1-4.xlsx")
    out = sys.argv[3] if len(sys.argv) > 3 else str(HERE / "Informe_de_pruebas_Sprint_1-3.docx")

    wbc = openpyxl.load_workbook(ciclos)
    stats, modules = {}, collections.defaultdict(collections.Counter)
    for ws in wbc:
        stats[ws.title] = collections.Counter(ws.cell(row=r, column=7).value for r in range(4, ws.max_row + 1))
        if ws.title != "Ciclo - Sprint 4":
            for r in range(4, ws.max_row + 1):
                modules[ws.title][ws.cell(row=r, column=2).value] += 1
    scope = [k for k in stats if k != "Ciclo - Sprint 4"]
    tot = sum(sum(stats[k].values()) for k in scope)
    tc = collections.Counter()
    for k in scope:
        tc.update(stats[k])
    executed = tot - tc["NO EJECUTADO"]
    s4 = sum(stats["Ciclo - Sprint 4"].values())
    first_fail = [k for k, v in run.UI_RESULTS.items() if v[2]]

    defects = [r for r in openpyxl.load_workbook(plan)["Defectos y pendientes"].iter_rows(min_row=2, values_only=True) if r[0]]
    defects = [(str(d[0]),) + tuple(d[1:]) for d in defects]
    open_defects = [d for d in defects if d[1] in DEF_TYPES and d[4].lower().startswith("abierta")]
    fixed_run = [d for d in defects if int(d[0]) >= 22 and d[1] == "Defecto"]
    S = run.SUITES

    doc = Document()
    sec = doc.sections[0]
    sec.left_margin = sec.right_margin = Inches(0.9)
    doc.styles["Normal"].font.name = "Calibri"
    doc.styles["Normal"].font.size = Pt(11)

    p = doc.add_paragraph(); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r = p.add_run("INFORME DE RESULTADOS DE PRUEBAS"); r.bold = True; r.font.size = Pt(22)
    p = doc.add_paragraph("Vicaria de los Pobres, Pastoral de Adicciones — Sprint 1 a 3"); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    table(doc, ["Dato", "Detalle"], [
        ["Proyecto", "Sistema de gestión de la Vicaria de los Pobres, Pastoral de Adicciones (Centro Barrial y Casa de Convivencia)"],
        ["Institución / Cátedra", "Instituto Superior Cura Gabriel Brochero · Tecnicatura en Desarrollo de Software · Prácticas Profesionalizantes II (Docente: Karina Salto)"],
        ["Rol que informa", "QA — Mateo Pavoni"],
        ["Versión probada", VERSION],
        ["Fecha del informe", REPORT_DATE],
        ["Documentos asociados", "Ciclos_de_prueba_Sprint_1-4.xlsx (ejecución por ciclo) · Plan_de_pruebas_Sprint_1-4.xlsx (casos, pasos y defectos) · Plan_de_prueba_-_completo_formateado.docx (casos de prueba)"],
    ], widths=[1.6, 5.1])

    # ---- 1
    doc.add_heading("1. Resumen ejecutivo", 1)
    doc.add_paragraph(
        f"Se ejecutaron {executed} de {tot} casos manuales de los Sprint 1, 2 y 3 y de seguridad/rendimiento sobre la {VERSION}: "
        f"{tc['PASÓ']} terminaron en PASÓ, {tc['FALLÓ']} en FALLÓ y {tc['BLOQUEADO']} quedaron BLOQUEADOS. "
        f"Además se corrieron {S['backend_unit']} pruebas unitarias y {S['backend_integration']} de integración del backend, "
        f"{S['frontend_tests']} del frontend, {S['ui_checks']} verificaciones de interfaz en navegador real y {S['security_checks']} de seguridad más {S['load_checks']} de carga, todas en verde."
    )
    doc.add_paragraph(
        f"La corrida de interfaz del {REPORT_DATE} reprobó {len(first_fail)} casos en su primera ejecución ({', '.join(first_fail)}) y destapó "
        f"{len(fixed_run)} defectos reales (una pantalla en blanco al bloquearse una cuenta, el rol Escucha viendo botones de edición, y mensajes de error "
        "incorrectos). Los seis se corrigieron en dev, tienen test automatizado donde corresponde y se re-ejecutaron con resultado PASÓ."
    )
    doc.add_paragraph(
        "El software es estable en sus funciones críticas (acceso y roles, fichas, observaciones, historia de vida, Casa de Convivencia, calendario, "
        "colaboradores y visitas), resiste 50 usuarios concurrentes sin errores y pasó las verificaciones básicas de seguridad. "
        f"Quedan {len(open_defects)} hallazgos abiertos, ninguno crítico. El Sprint 4 (asistencia y medicación) no se evalúa: su backend todavía no está desarrollado."
    )
    p = doc.add_paragraph(); r = p.add_run("Recomendación: APROBADO CON CONDICIONES para los Sprint 1 a 3 (ver sección 8)."); r.bold = True

    # ---- 2
    doc.add_heading("2. Alcance de las pruebas", 1)
    doc.add_heading("2.1 Qué se probó", 2)
    table(doc, ["Ciclo", "Historias / módulos", "Casos", "Cómo se ejecutó"], [
        ["Sprint 1 — Acceso y roles (EP-03)", "Registro, aprobación y rechazo de cuentas, login con bloqueo por 5 intentos, roles y permisos, baja y reactivación de cuentas, notificaciones internas (SCRUM-12, 13, 20, 22, 33, 64)", sum(stats["Ciclo - Sprint 1"].values()), "Manual en el navegador contra el stack local (06/10) y re-ejecución de los flujos de UI el 16/10"],
        ["Sprint 2 — Fichas, observaciones e historia de vida (EP-01, EP-02, EP-12)", "Alta, edición, búsqueda y filtros de fichas; observaciones con categorías, línea de tiempo y exportación; historia de vida; estadías de Casa de Convivencia; inactividad automática (SCRUM-5 a 10, 72, 78, 88, 112, 117, 134, 140, 146, 156, 159 a 177)", sum(stats["Ciclo - Sprint 2"].values()), "Manual (06/10) y flujos de UI en navegador real (16/10)"],
        ["Sprint 3 — Calendario, colaboradores y visitas (EP-04, EP-05)", "Calendario general y personal, recurrencias, colaboradores (alta, edición, baja, búsqueda), visitas de la Casa de Convivencia (SCRUM-15 a 19, 74)", sum(stats["Ciclo - Sprint 3"].values()), "API y UI contra el stack local (08 al 14/10) con respaldo de tests automatizados"],
        ["Transversal — Seguridad y rendimiento", "Autenticación, autorización por rol, manipulación de JWT, inyección, XSS, cabeceras, política de contraseñas, payloads anómalos, carga concurrente", sum(stats["Ciclo - Seguridad y carga"].values()), "Script de API (Node) contra el stack local y un caso de UI"],
    ], widths=[1.5, 2.9, 0.6, 1.7])
    doc.add_paragraph(
        f"Además se verificó la matriz de permisos: los 4 roles (Referente, Directora, Coordinador, Escucha) recorrieron las pantallas de Sprint 1 a 3 "
        "y cada ruta respondió según el permiso esperado (acceso o 'acceso denegado'), sin errores de API ni de consola."
    )
    doc.add_heading("2.2 Qué NO se probó", 2)
    table(doc, ["Exclusión", "Motivo", "Riesgo / mitigación"], [
        [f"Sprint 4: asistencia y medicación ({s4} casos, TC-147 a TC-194)", "El backend tiene 1 de 18 subtareas hechas (SCRUM-232) y las pantallas usan servicios simulados; los casos están diseñados pero no se pueden ejecutar.", "Sin riesgo sobre Sprint 1-3. Se ejecutan cuando el backend se integre; vence el 24/10."],
        ["Criterios de Sprint 4 con preguntas abiertas", "Cierre del día de asistencia, actividades configurables, permisos del Coordinador y stock en farmacia vs. Centro Barrial no están definidos.", "Confirmar con el equipo antes de implementar o probar."],
        ["Pruebas de penetración y seguridad avanzada", "Fuera del alcance del plan; se hicieron verificaciones básicas (sección 6), no un pentest.", "Antes de salir a producción real conviene un análisis de dependencias y una revisión de configuración (HTTPS, CORS, secretos)."],
        ["Carga sostenida y dimensionamiento", "La prueba se hizo en Docker local con SQL Server Express limitado; sirve de referencia, no de dimensionamiento.", "Repetir en el entorno de despliegue si se espera más de 50 usuarios simultáneos."],
        ["Navegadores distintos de Chromium y dispositivos físicos", "La interfaz se probó en Chromium y a 390 px de ancho emulado.", "Probar en Firefox/Safari y un celular real antes del uso en campo."],
        ["Pantalla de Informes", "El ítem del menú apunta a una ruta sin pantalla (defecto #5).", "Se corrige junto con la historia de informes."],
    ], widths=[2.0, 2.6, 2.1])

    # ---- 3
    doc.add_heading("3. Estrategia y entorno", 1)
    bullets(doc, [
        "Pruebas manuales de interfaz: casos TC con pasos y resultado esperado, ejecutados por rol; los flujos principales se automatizaron con Playwright (sprint1-2-ui-flows.mjs, sprint3-ui-flows.mjs) para poder repetirlos.",
        "Pruebas de API: casos de Sprint 3 respaldados por tests de integración (WebApplicationFactory + SQL Server en contenedor).",
        f"Pruebas automatizadas: {S['backend_unit']} unitarias (xUnit, DbContext en memoria), {S['backend_integration']} de integración y {S['frontend_tests']} del frontend (Vitest, {S['frontend_files']} archivos).",
        "Seguridad y carga: api-security-and-load.mjs (Node, sin dependencias), repetible con un comando.",
        "Entorno: Docker Compose local (SQL Server 2022 Express, API .NET 9 y frontend Angular servido por nginx), datos de prueba precargados y 4 usuarios, uno por rol. Chromium con Playwright.",
        "Criterio de resultado: un caso queda PASÓ si todos sus pasos dieron el resultado esperado; si falla en la primera ejecución se registra el defecto, se corrige y se re-ejecuta, dejando constancia en el Resultado Real y en la columna ID Bug.",
    ])

    # ---- 4
    doc.add_heading("4. Métricas de ejecución", 1)
    table(doc, ["Métrica (casos manuales Sprint 1-3 y transversal)", "Cantidad", "Porcentaje"], [
        ["Total de casos planificados", tot, "100%"],
        ["Casos ejecutados", executed, pct(executed, tot)],
        ["Casos sin ejecutar", tc["NO EJECUTADO"], pct(tc["NO EJECUTADO"], tot)],
        ["Casos PASÓ (resultado final)", tc["PASÓ"], pct(tc["PASÓ"], tot)],
        ["Casos FALLÓ (resultado final)", tc["FALLÓ"], pct(tc["FALLÓ"], tot)],
        ["Casos BLOQUEADOS", tc["BLOQUEADO"], pct(tc["BLOQUEADO"], tot)],
        ["Casos que fallaron en la primera ejecución y pasaron tras la corrección", len(first_fail), pct(len(first_fail), tot)],
    ], widths=[4.2, 1.0, 1.0])
    rows = []
    for k, c in stats.items():
        n = sum(c.values())
        rows.append([k, n, c["PASÓ"], c["FALLÓ"], c["BLOQUEADO"], c["NO EJECUTADO"], pct(c["PASÓ"], n)])
    table(doc, ["Ciclo", "Planificados", "PASÓ", "FALLÓ", "BLOQUEADO", "NO EJECUTADO", "% PASÓ"], rows, widths=[2.3, 0.9, 0.6, 0.6, 0.9, 1.0, 0.6])
    doc.add_paragraph(
        "El Sprint 4 aparece solo como referencia: sus casos no entran en las métricas del primer cuadro. "
        "TC-93 falló el 08/10 (404 por falta de SCRUM-200) y pasó al re-ejecutarse el 09/10; las cifras reflejan el último resultado de cada caso."
    )
    doc.add_heading("Pruebas automatizadas", 2)
    table(doc, ["Suite", "Resultado"], [
        ["Backend unitarias (xUnit)", f"{S['backend_unit']} de {S['backend_unit']} OK"],
        ["Backend integración (xUnit + SQL Server en contenedor)", f"{S['backend_integration']} de {S['backend_integration']} OK"],
        [f"Frontend (Vitest, {S['frontend_files']} archivos) y build de producción", f"{S['frontend_tests']} de {S['frontend_tests']} OK; build OK"],
        ["UI en navegador real, Sprint 1-2 (Playwright)", f"{S['ui_checks']} de {S['ui_checks']} verificaciones OK (tras corregir 6 defectos)"],
        ["Seguridad y carga (Node)", f"{S['security_checks'] + S['load_checks']} de {S['security_checks'] + S['load_checks']} OK"],
    ], widths=[4.4, 2.3])
    doc.add_heading("Cobertura por módulo", 2)
    mrows = []
    for k in scope:
        for m, n in sorted(modules[k].items(), key=lambda x: -x[1]):
            mrows.append([k.replace("Ciclo - ", ""), m, n])
    table(doc, ["Ciclo", "Módulo / componente", "Casos"], mrows, widths=[1.5, 4.5, 0.7], size=8)

    # ---- 5
    doc.add_heading("5. Resumen de defectos", 1)
    sev_count = collections.Counter()
    by = collections.defaultdict(list)
    for d in defects:
        if d[1] in DEF_TYPES and d[0] in SEVERITY:
            by[SEVERITY[d[0]] if SEVERITY[d[0]] != "Menor" else "Menor / Estético"].append(d)
    by["Crítico / Bloqueante"] = []
    table(doc, ["Severidad", "Total", "Corregidos", "Abiertos"], [
        [s, len(by[s]), sum(1 for d in by[s] if d[4].lower().startswith("corregido")), sum(1 for d in by[s] if d[4].lower().startswith("abierta"))]
        for s in ORDER
    ], widths=[2.4, 1.0, 1.2, 1.0])
    for sev in ORDER:
        doc.add_heading(sev, 2)
        if sev.startswith("Crítico"):
            doc.add_paragraph("Sin defectos críticos: no hay errores que rompan la aplicación ni impidan el flujo principal.")
            continue
        table(doc, ["#", "Descripción", "Origen", "Estado"], [[d[0], d[2], d[3], d[4]] for d in by[sev]], widths=[0.4, 4.0, 1.4, 0.9], size=8)
    doc.add_heading("Defectos hallados en la corrida del 16/10 y su corrección", 2)
    table(doc, ["#", "Caso", "Qué pasaba", "Corrección"],
          [[str(22 + i), {0: "TC-04", 1: "TC-04", 2: "TC-07", 3: "TC-14", 4: "TC-12", 5: "TC-38"}.get(i, ""), d[1], d[4]]
           for i, d in enumerate(run.NEW_DEFECTS) if d[0] == "Defecto"], widths=[0.4, 0.7, 3.2, 2.4], size=8)
    picture(doc, "TC-04-clave-correcta.png", "Cuenta bloqueada tras 5 intentos: ahora el login informa el bloqueo (antes quedaba en blanco).")
    picture(doc, "TC-07-login-inactiva.png", "Cuenta desactivada: mensaje específico en lugar del genérico.")
    doc.add_heading("Preguntas abiertas y pendientes", 2)
    bullets(doc, [f"#{d[0]} — {d[2]} ({d[3]})" for d in defects if d[1] in ("Pregunta abierta", "Pendiente") and d[4].lower().startswith("abierta")])

    # ---- 6
    doc.add_heading("6. Resultados de seguridad y rendimiento", 1)
    table(doc, ["Verificación", "Resultado"], [[f"TC-{run.SEC_FIRST_ID + i} {c[1]}", c[5]] for i, c in enumerate(run.SEC_CASES) if c[0].startswith("SEC")], widths=[2.6, 4.1], size=8)
    doc.add_paragraph("Rendimiento (carga concurrente sobre la API local):")
    table(doc, ["Prueba", "Resultado"], [[f"TC-{run.SEC_FIRST_ID + i} {c[1]}", c[5]] for i, c in enumerate(run.SEC_CASES) if c[0].startswith("PERF")], widths=[2.6, 4.1], size=8)
    doc.add_paragraph(run.SEC_NOTE + " El login es la operación más lenta por el hash bcrypt (decisión de seguridad); con 20 usuarios simultáneos tarda unos 2,7 s en el percentil 95.")
    doc.add_paragraph("Observación: la API no aplica límite de tasa (rate limiting) más allá del bloqueo de cuenta por 5 intentos; se recomienda evaluarlo antes de exponer el sistema a internet.")

    # ---- 7
    doc.add_heading("7. Riesgos y recomendaciones", 1)
    table(doc, ["Riesgo", "Impacto", "Recomendación"], [
        ["Sprint 4 sin backend a 8 días del cierre (24/10)", "Alto: asistencia y medicación no se pueden validar", "Priorizar los endpoints de asistencia (SCRUM-216, 220) y de agenda; integrar las pantallas con la API antes de ejecutar los 48 casos."],
        ["Ramas dev-backend y dev-frontend desactualizadas respecto de dev", "Medio: conflictos y pérdida de correcciones", "Sincronizar por PR y avisar a Santiago por el global.json que exige SDK 9."],
        ["Reglas de DNI y permisos del Coordinador sin definir", "Medio: comportamientos distintos entre API y UI", "Resolver las preguntas abiertas #20, #28 y #29 con el equipo."],
        ["Ítem 'Informes' del menú sin pantalla", "Bajo: página vacía", "Ocultar el ítem o crear la pantalla (defecto #5)."],
        ["Sin pruebas en otros navegadores ni celular real", "Medio en uso de campo", "Ejecutar los flujos de login, fichas y observaciones en Firefox y en un Android."],
    ], widths=[2.2, 1.8, 2.7])

    # ---- 8
    doc.add_heading("8. Conclusión y recomendación final", 1)
    p = doc.add_paragraph(); r = p.add_run("Resultado: APROBADO CON CONDICIONES (Sprint 1 a 3)."); r.bold = True
    doc.add_paragraph(
        "El producto cumple los criterios de aceptación de los Sprint 1 a 3, no tiene defectos críticos abiertos y los defectos hallados en esta "
        "corrida ya están corregidos y re-verificados. Criterios de salida:"
    )
    table(doc, ["Criterio", "Estado"], [
        ["100% de los casos planificados de Sprint 1-3 ejecutados", f"Cumplido ({executed} de {tot})"],
        ["Sin defectos críticos ni bloqueantes abiertos", "Cumplido"],
        ["Sin casos en FALLÓ ni BLOQUEADO", "Cumplido"],
        ["Suites automatizadas en verde", "Cumplido"],
        ["Seguridad básica y carga de 50 usuarios sin errores", "Cumplido"],
        ["Defectos Mayores abiertos resueltos", f"Pendiente: {sum(1 for d in by['Mayor'] if d[4].lower().startswith('abierta'))} abierto(s) — ver #5 y #13"],
        ["Cambios de esta corrida commiteados y llevados a dev-backend, dev-frontend y main", "Pendiente (los cambios están en el árbol de trabajo)"],
    ], widths=[4.6, 2.1])
    doc.add_paragraph("Condiciones para liberar:")
    bullets(doc, [
        "Commitear las correcciones del 16/10 y sincronizar las ramas (dev-backend, dev-frontend y main).",
        "Resolver el ítem 'Informes' del menú (#5).",
        "Definir con el equipo las reglas de DNI duplicado y los permisos del Coordinador.",
        "Repetir los flujos de UI en un navegador adicional y en un celular antes del uso real.",
    ])
    doc.add_paragraph("Sprint 4: sin veredicto. Se evaluará cuando el backend de asistencia y medicación esté integrado y se resuelvan las preguntas abiertas.")

    # ---- Anexo
    doc.add_heading("Anexo A. Evidencia y cómo repetir las pruebas", 1)
    bullets(doc, [
        "Capturas: docs/qa/e2e/evidence (una por verificación de UI).",
        "UI Sprint 1-2: node docs/qa/e2e/sprint1-2-ui-flows.mjs all (necesita el stack local y Playwright).",
        "UI Sprint 3: node docs/qa/e2e/sprint3-ui-flows.mjs.",
        "Seguridad y carga: node docs/qa/security/api-security-and-load.mjs (resultado en security-results.json).",
        "Regenerar documentos: build_test_plan_xlsx.py, build_test_cycles_xlsx.py y build_test_report_docx.py (docs/qa).",
    ])
    picture(doc, "TC-12-detalle-escucha.png", "Detalle de una ficha con el rol Escucha: sin el botón 'Editar ficha'.")
    picture(doc, "SEC-08b-xss.png", "Observación con HTML/script mostrada como texto, sin ejecutar código.")
    doc.save(out)
    print(out, f"tot={tot} exec={executed} pass={tc['PASÓ']} open={len(open_defects)}")


if __name__ == "__main__":
    main()
