"""Genera docs/qa/Informe_de_pruebas_Sprint_1-5.docx (informe de resultados de pruebas).
Las métricas se calculan leyendo Ciclos_de_prueba_Sprint_1-5.xlsx; los defectos, la hoja
"Defectos y pendientes" de Plan_de_pruebas_Sprint_1-5.xlsx; el resto, run_2026_10_16.py.

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

FRONT_TESTS = 188  # Vitest, 44 archivos (27/10)

HERE = Path(__file__).parent
EVIDENCE = HERE / "e2e" / "evidence"
VERSION = "rama dev, Sprint 5 integrado (27/10/2026)"
REPORT_DATE = "27/10/2026"
# suites automatizadas re-ejecutadas el 27/10 con el Sprint 5 integrado
SUITES = {**run.SUITES, "backend_unit": 335, "backend_integration": 261, "frontend_tests": FRONT_TESTS}
NAVY = "1F3864"

# Severidad de los hallazgos de la hoja de defectos (# de fila -> severidad); los #14+ vienen de run.NEW_DEFECTS
SEVERITY = {"1": "Mayor", "2": "Menor", "3": "Mayor", "4": "Menor", "5": "Mayor", "6": "Mayor",
            "10": "Menor", "11": "Mayor", "12": "Menor", "13": "Mayor"}
for i, d in enumerate(run.NEW_DEFECTS):
    if d[0] == "Defecto":
        SEVERITY[str(14 + i)] = d[5]
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
    ciclos = sys.argv[1] if len(sys.argv) > 1 else str(HERE / "Ciclos_de_prueba_Sprint_1-5.xlsx")
    plan = sys.argv[2] if len(sys.argv) > 2 else str(HERE / "Plan_de_pruebas_Sprint_1-5.xlsx")
    out = sys.argv[3] if len(sys.argv) > 3 else str(HERE / "Informe_de_pruebas_Sprint_1-5.docx")

    wbc = openpyxl.load_workbook(ciclos)
    stats = {ws.title: collections.Counter(ws.cell(row=r, column=7).value for r in range(4, ws.max_row + 1)) for ws in wbc}
    tot = sum(sum(c.values()) for c in stats.values())
    tc = collections.Counter()
    for c in stats.values():
        tc.update(c)

    defects = [r for r in openpyxl.load_workbook(plan)["Defectos y pendientes"].iter_rows(min_row=2, values_only=True) if r[0]]
    defects = [(str(d[0]),) + tuple(d[1:]) for d in defects]
    bugs = [d for d in defects if d[1] in DEF_TYPES]
    S = SUITES

    doc = Document()
    sec = doc.sections[0]
    sec.left_margin = sec.right_margin = Inches(0.9)
    doc.styles["Normal"].font.name = "Calibri"
    doc.styles["Normal"].font.size = Pt(11)

    p = doc.add_paragraph(); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r = p.add_run("INFORME DE RESULTADOS DE PRUEBAS"); r.bold = True; r.font.size = Pt(22)
    p = doc.add_paragraph("Vicaria de los Pobres, Pastoral de Adicciones — Sprint 1 a 5"); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    table(doc, ["Dato", "Detalle"], [
        ["Proyecto", "Sistema de gestión de la Vicaria de los Pobres, Pastoral de Adicciones"],
        ["Institución / Cátedra", "Instituto Superior Cura Gabriel Brochero · Tecnicatura en Desarrollo de Software · Prácticas Profesionalizantes II (Docente: Karina Salto)"],
        ["QA / quien ejecutó las pruebas", "Mateo Pavoni"],
        ["Versión probada", VERSION],
        ["Fecha del informe", REPORT_DATE],
    ], widths=[1.8, 4.9])

    doc.add_heading("1. Resumen", 1)
    doc.add_paragraph(
        f"Se ejecutaron {tot} casos de prueba de los Sprint 1 a 5: {tc['PASÓ']} terminaron en PASÓ, {tc['FALLÓ']} en FALLÓ y {tc['BLOQUEADO']} quedaron BLOQUEADOS. "
        "Todos los casos los ejecutó Mateo Pavoni, de forma manual en el navegador o con scripts automatizados, como se indica en cada ciclo. "
        f"Además se corrieron {S['backend_unit']} pruebas unitarias y {S['backend_integration']} de integración del backend y {S['frontend_tests']} del frontend, todas en verde."
    )

    doc.add_heading("2. Qué se probó y cómo", 1)
    c = lambda k: sum(stats[k].values())
    table(doc, ["Ciclo", "Qué se probó", "Casos", "Cómo se ejecutó", "Ejecutó"], [
        ["Sprint 1", "Registro, aprobación y rechazo de cuentas, login con bloqueo, roles y permisos, notificaciones", c("Ciclo - Sprint 1"), "Manual en el navegador (06/10) y flujos de UI con Playwright (16/10)", "Mateo Pavoni"],
        ["Sprint 2", "Fichas, observaciones, historia de vida, estadías de Casa de Convivencia", c("Ciclo - Sprint 2"), "Manual (06/10) y flujos de UI con Playwright (16/10)", "Mateo Pavoni"],
        ["Sprint 3", "Calendario, colaboradores y visitas", c("Ciclo - Sprint 3"), "API y UI contra el stack local (08 al 14/10), con tests automatizados", "Mateo Pavoni"],
        ["Sprint 4", "Asistencia, estadísticas y medicación; pruebas de API, integración front-back, seguridad y carga", c("Ciclo - Sprint 4"), "Scripts con Playwright y Node contra el stack local (16 al 20/10)", "Mateo Pavoni"],
        ["Sprint 5", "Ficha exportable, adjuntos, evaluación psiquiátrica, estado activo/inactivo, respaldos e informe de Cáritas; pruebas de API", c("Ciclo - Sprint 5"), "Scripts con Playwright y Node contra el stack local (27/10)", "Mateo Pavoni"],
    ], widths=[0.8, 2.7, 0.6, 2.0, 0.9], size=9)

    doc.add_heading("3. Resultados", 1)
    rows = []
    for k, cnt in stats.items():
        n = sum(cnt.values())
        rows.append([k.replace("Ciclo - ", ""), n, cnt["PASÓ"], cnt["FALLÓ"], cnt["BLOQUEADO"]])
    rows.append(["Total", tot, tc["PASÓ"], tc["FALLÓ"], tc["BLOQUEADO"]])
    table(doc, ["Ciclo", "Casos", "PASÓ", "FALLÓ", "BLOQUEADO"], rows, widths=[2.2, 1.0, 1.0, 1.0, 1.2])
    doc.add_paragraph("")
    table(doc, ["Pruebas automatizadas", "Resultado"], [
        ["Backend unitarias (xUnit)", f"{S['backend_unit']} de {S['backend_unit']} OK"],
        ["Backend integración (xUnit + SQL Server)", f"{S['backend_integration']} de {S['backend_integration']} OK"],
        ["Frontend (Vitest) y build", f"{S['frontend_tests']} de {S['frontend_tests']} OK"],
    ], widths=[4.4, 2.3])

    doc.add_heading("4. Defectos encontrados", 1)
    table(doc, ["#", "Descripción", "Origen", "Estado"], [[d[0], d[2], d[3], d[4]] for d in bugs], widths=[0.4, 4.0, 1.4, 0.9], size=8)

    doc.add_heading("5. Evidencia", 1)
    bullets(doc, [
        "Capturas: docs/qa/e2e/evidence.",
        "Scripts de prueba: docs/qa/e2e (interfaz y API) y docs/qa/security (seguridad y carga).",
    ])
    doc.save(out)
    print(out, f"tot={tot} pass={tc['PASÓ']} fail={tc['FALLÓ']}")


if __name__ == "__main__":
    main()
