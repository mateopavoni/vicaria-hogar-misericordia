"""Genera docs/qa/Informe_de_pruebas_Sprint_1-6.docx (informe de resultados de pruebas).
Las métricas se calculan leyendo Ciclos_de_prueba_Sprint_1-6.xlsx; los defectos, la hoja
"Defectos y pendientes" de Plan_de_pruebas_Sprint_1-5.xlsx más los de sprint6_regression.py.

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
import sprint6_regression as s6

HERE = Path(__file__).parent
EVIDENCE = HERE / "e2e" / "evidence"
VERSION = "rama main del 04/11/2026, versión final después de la regresión del Sprint 6"
REPORT_DATE = "04/11/2026"
# suites automatizadas re-ejecutadas en la regresión final del Sprint 6
SUITES = {**run.SUITES, **s6.SUITES}
NAVY = "1F3864"

# Severidad de cada hallazgo de la hoja de defectos (# de fila -> severidad); los #14 a #22 vienen de run.NEW_DEFECTS
CRIT, MAJOR, MINOR = "Crítico / Bloqueante", "Mayor", "Menor / Estético"
SEVERITY = {"1": MAJOR, "2": MINOR, "3": MAJOR, "4": MINOR, "5": MAJOR, "6": MAJOR,
            "10": MINOR, "11": MAJOR, "12": MINOR, "13": CRIT,
            "23": CRIT, "24": MAJOR, "25": MAJOR, "26": MAJOR, "27": MAJOR, "28": MINOR, "29": MAJOR,
            "30": MAJOR, "31": CRIT, "32": MINOR, "33": MINOR, "34": MAJOR, "35": MAJOR, "36": MINOR}
for i, d in enumerate(run.NEW_DEFECTS):
    SEVERITY[str(14 + i)] = {"Mayor": MAJOR, "Menor": MINOR}.get(d[5], d[5])
for d in s6.DEFECTS:
    SEVERITY[d[0]] = {"Mayor": MAJOR, "Menor": MINOR}[d[5]]
ORDER = [CRIT, MAJOR, MINOR]
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
    ciclos = sys.argv[1] if len(sys.argv) > 1 else str(HERE / "Ciclos_de_prueba_Sprint_1-6.xlsx")
    plan = sys.argv[2] if len(sys.argv) > 2 else str(HERE / "Plan_de_pruebas_Sprint_1-5.xlsx")
    out = sys.argv[3] if len(sys.argv) > 3 else str(HERE / "Informe_de_pruebas_Sprint_1-6.docx")

    wbc = openpyxl.load_workbook(ciclos)
    stats = {ws.title: collections.Counter(ws.cell(row=r, column=7).value for r in range(4, ws.max_row + 1)) for ws in wbc}
    tot = sum(sum(c.values()) for c in stats.values())
    tc = collections.Counter()
    for c in stats.values():
        tc.update(c)

    defects = [r for r in openpyxl.load_workbook(plan)["Defectos y pendientes"].iter_rows(min_row=2, values_only=True) if r[0]]
    defects = [(str(d[0]),) + tuple(d[1:]) for d in defects]
    defects += [d[:5] for d in s6.DEFECTS]
    bugs = [d for d in defects if d[1] in DEF_TYPES]
    S = SUITES

    doc = Document()
    sec = doc.sections[0]
    sec.left_margin = sec.right_margin = Inches(0.9)
    doc.styles["Normal"].font.name = "Calibri"
    doc.styles["Normal"].font.size = Pt(11)

    p = doc.add_paragraph(); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r = p.add_run("INFORME DE RESULTADOS DE PRUEBAS"); r.bold = True; r.font.size = Pt(22)
    p = doc.add_paragraph("Vicaria de los Pobres, Pastoral de Adicciones — Sprint 1 a 6"); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    table(doc, ["Dato", "Detalle"], [
        ["Proyecto", "Sistema de gestión de la Vicaria de los Pobres, Pastoral de Adicciones"],
        ["Institución / Cátedra", "Instituto Superior Cura Gabriel Brochero · Tecnicatura en Desarrollo de Software · Prácticas Profesionalizantes II (Docente: Karina Salto)"],
        ["QA / quien ejecutó las pruebas", "Mateo Pavoni"],
        ["Versión probada", VERSION],
        ["Fecha del informe", REPORT_DATE],
    ], widths=[1.8, 4.9])

    open_bugs = [d for d in bugs if not str(d[4]).startswith(("Corregido", "Resuelt", "Implementad"))]
    by_sev = collections.Counter(SEVERITY.get(d[0], MINOR) for d in bugs)
    open_sev = collections.Counter(SEVERITY.get(d[0], MINOR) for d in open_bugs)
    executed = tot - tc["NO EJECUTADO"]

    doc.add_heading("1. Resumen ejecutivo", 1)
    doc.add_paragraph(
        f"Se probó el sistema de gestión de la Vicaria, versión {VERSION}: los módulos entregados en los Sprint 1 a 5, en {tot} casos de prueba "
        f"agrupados en {len(stats)} ciclos, más {S['backend_unit'] + S['backend_integration'] + S['frontend_tests']} pruebas automatizadas. "
        f"Se ejecutaron {executed} casos ({pct(executed, tot)}) y pasaron {tc['PASÓ']}. Durante los ciclos se registraron {len(bugs)} defectos: "
        f"{len(bugs) - len(open_bugs)} están corregidos y verificados y queda abierto {len(open_bugs)}, de severidad menor y con una alternativa de uso. "
        "No queda abierto ningún defecto crítico ni mayor. El software es estable en sus funciones críticas (acceso y roles, fichas, asistencia, medicación, "
        "respaldos e informe de Cáritas) y cumple los criterios mínimos de calidad para salir a producción, con las condiciones del punto 5."
    )

    doc.add_heading("2. Alcance de las pruebas", 1)
    doc.add_heading("2.1 Qué se probó", 2)
    c = lambda k: sum(stats[k].values())
    table(doc, ["Ciclo", "Qué se probó", "Casos", "Cómo se ejecutó", "Ejecutó"], [
        ["Sprint 1", "Registro, aprobación y rechazo de cuentas, login con bloqueo, roles y permisos, notificaciones", c("Ciclo - Sprint 1"), "Manual en el navegador (06/10) y flujos de UI con Playwright (16/10)", "Mateo Pavoni"],
        ["Sprint 2", "Fichas, observaciones, historia de vida, estadías de Casa de Convivencia", c("Ciclo - Sprint 2"), "Manual (06/10) y flujos de UI con Playwright (16/10)", "Mateo Pavoni"],
        ["Sprint 3", "Calendario, colaboradores y visitas", c("Ciclo - Sprint 3"), "API y UI contra el stack local (08 al 14/10), con tests automatizados", "Mateo Pavoni"],
        ["Sprint 4", "Asistencia, estadísticas y medicación; pruebas de API, integración front-back, seguridad y carga", c("Ciclo - Sprint 4"), "Scripts con Playwright y Node contra el stack local (16 al 20/10)", "Mateo Pavoni"],
        ["Sprint 5", "Ficha exportable, adjuntos, evaluación psiquiátrica, estado activo/inactivo, respaldos e informe de Cáritas; pruebas de API", c("Ciclo - Sprint 5"), "Scripts con Playwright y Node contra el stack local (27/10)", "Mateo Pavoni"],
        ["Sprint 6", "Regresión final sobre la versión que se entrega: pruebas automatizadas, re-ejecución de todos los scripts de API, interfaz, seguridad y carga, y revisión visual de 76 pantallas por rol en escritorio y celular", c("Ciclo - Sprint 6"), "xUnit, Vitest y scripts con Playwright y Node sobre un stack limpio (01 al 04/11)", "Mateo Pavoni"],
    ], widths=[0.8, 2.7, 0.6, 2.0, 0.9], size=9)
    doc.add_heading("2.2 Qué NO se probó", 2)
    table(doc, ["Exclusión", "Motivo"], [
        ["Entorno de producción", "Todavía no hay un servidor de producción: todas las pruebas se hicieron contra el stack local en Docker (SQL Server, API y frontend), que usa la misma configuración que el despliegue."],
        ["Rendimiento con volumen real", "Las pruebas de carga se corrieron en el entorno local con SQL Server Express: sus cifras son de referencia y no sirven para dimensionar el servidor."],
        ["Navegadores distintos de Chrome", "Los flujos de interfaz se ejecutaron en Chromium (Playwright) y en Chrome. No se probaron Firefox, Safari ni Edge."],
        ["Interfaz de exportación con el PDF del backend", "TC-212 a TC-218 se ejecutaron con la exportación anterior, que generaba el PDF en el navegador. El PDF que genera ahora el backend se validó por API (S5-X01 a S5-X03)."],
        ["TC-153, TC-154, TC-193 y los casos del respaldo diario en la regresión", "Sobre una base recién creada no se pueden ejecutar: usan fechas fijas, un residente sin evaluación vigente o necesitan que el respaldo diario ya haya corrido. Pasaron en sus ciclos originales y quedan BLOQUEADOS en el ciclo del Sprint 6 (S6-10 y S6-12)."],
        ["Stock de ropa (EP-06), convenios (EP-08) y sitio público (EP-09)", "Son historias del backlog que no se desarrollaron (SCRUM-34 a 36, 43 a 46 y 48 a 52), así que no forman parte de esta versión."],
    ], widths=[2.0, 4.7], size=9)

    doc.add_heading("3. Métricas de ejecución", 1)
    table(doc, ["Métrica", "Cantidad", "Porcentaje (%)"], [
        ["Total de casos planificados", tot, "100%"],
        ["Casos ejecutados", executed, pct(executed, tot)],
        ["Casos sin ejecutar", tc["NO EJECUTADO"], pct(tc["NO EJECUTADO"], tot)],
        ["Casos PASÓ (exitosos)", tc["PASÓ"], f"{pct(tc['PASÓ'], tot)} (del total)"],
        ["Casos FALLÓ (con error)", tc["FALLÓ"], f"{pct(tc['FALLÓ'], tot)} (del total)"],
        ["Casos BLOQUEADOS", tc["BLOQUEADO"], f"{pct(tc['BLOQUEADO'], tot)} (del total)"],
    ], widths=[3.4, 1.4, 1.9])
    doc.add_paragraph("Por ciclo:")
    rows = []
    for k, cnt in stats.items():
        n = sum(cnt.values())
        rows.append([k.replace("Ciclo - ", ""), n, cnt["PASÓ"], cnt["FALLÓ"], cnt["BLOQUEADO"], cnt["NO EJECUTADO"]])
    rows.append(["Total", tot, tc["PASÓ"], tc["FALLÓ"], tc["BLOQUEADO"], tc["NO EJECUTADO"]])
    table(doc, ["Ciclo", "Casos", "PASÓ", "FALLÓ", "BLOQUEADO", "Sin ejecutar"], rows, widths=[1.6, 0.9, 0.9, 0.9, 1.1, 1.1])
    doc.add_paragraph(
        "Los casos que fallaron en una primera ejecución se volvieron a ejecutar después de la corrección. La planilla registra su "
        "resultado final y, en las columnas ID Bug y Comentarios, el defecto que encontraron."
    )
    doc.add_paragraph("Pruebas automatizadas, re-ejecutadas el 04/11 sobre la versión final:")
    table(doc, ["Pruebas automatizadas", "Resultado"], [
        ["Backend unitarias (xUnit)", f"{S['backend_unit']} de {S['backend_unit']} OK"],
        ["Backend integración (xUnit + SQL Server)", f"{S['backend_integration']} de {S['backend_integration']} OK"],
        ["Frontend (Vitest)", f"{S['frontend_tests']} de {S['frontend_tests']} OK"],
    ], widths=[4.4, 2.3])

    doc.add_heading("4. Resumen de defectos", 1)
    table(doc, ["Severidad", "Encontrados", "Corregidos", "Abiertos"], [
        [sev, by_sev[sev], by_sev[sev] - open_sev[sev], open_sev[sev]] for sev in ORDER
    ] + [["Total", len(bugs), len(bugs) - len(open_bugs), len(open_bugs)]], widths=[2.4, 1.4, 1.4, 1.4])
    doc.add_paragraph(
        "Críticos / Bloqueantes: rompen la aplicación o impiden el flujo principal. Mayores: afectan un flujo importante, pero tienen un camino "
        "alternativo. Menores / Estéticos: no afectan la funcionalidad."
    )
    for sev in ORDER:
        items = [d for d in bugs if SEVERITY.get(d[0], MINOR) == sev]
        if not items:
            continue
        doc.add_heading(sev, 2)
        table(doc, ["#", "Descripción", "Origen", "Estado"], [[d[0], d[2], d[3], d[4]] for d in items], widths=[0.4, 4.0, 1.4, 0.9], size=8)

    doc.add_heading("5. Conclusión y recomendación final", 1)
    p = doc.add_paragraph()
    r_ = p.add_run("Recomendación: APROBADO CON CONDICIONES.")
    r_.bold = True
    doc.add_paragraph(
        f"Los {by_sev[CRIT]} defectos críticos y los {by_sev[MAJOR]} mayores que se encontraron están corregidos y verificados, todos los casos ejecutados pasan "
        f"y las pruebas automatizadas están en verde. La regresión final del Sprint 6 encontró {len(s6.DEFECTS)} defectos nuevos (#37 a #44: la fecha de "
        "\"hoy\" calculada en UTC, horas mostradas en UTC, pedidos sin permiso y detalles de diseño en el celular), todos corregidos y verificados. "
        "El sistema se puede liberar con estas condiciones:"
    )
    bullets(doc, [
        "Corregir en un parche el defecto menor abierto (#10: al editar una ocurrencia de un evento recurrente, la serie pasa a empezar ese día). Mientras tanto, la serie se edita desde su primera ocurrencia.",
        "Apenas se despliegue, ejecutar una prueba de humo en el servidor de producción: login con cada rol, alta de ficha, registro de asistencia, respaldo diario e informe de Cáritas.",
    ])

    doc.add_heading("Anexo: evidencia", 1)
    bullets(doc, [
        "Ciclos de prueba con el resultado de cada caso: Ciclos_de_prueba_Sprint_1-6.xlsx.",
        "Capturas: docs/qa/e2e/evidence.",
        "Scripts de prueba: docs/qa/e2e (interfaz y API) y docs/qa/security (seguridad y carga).",
    ])
    doc.save(out)
    print(out, f"tot={tot} pass={tc['PASÓ']} fail={tc['FALLÓ']}")


if __name__ == "__main__":
    main()
