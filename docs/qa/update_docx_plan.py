"""Agrega los Sprint 3 y 4 al docx "Plan de prueba - completo formateado" (que trae Sprint 1-2).

Parte A (manuales): secciones nuevas al final de la Parte A, antes del título de la Parte B.
Parte B (automatizados): sección del Sprint 3 al final del documento.
El Sprint 4 suma casos manuales y los automatizados del esquema de medicación (SCRUM-232).

Uso: python docs/qa/update_docx_plan.py <docx de entrada> <docx de salida>
Los formatos replican el XML que ya usa el documento (tabla de título, ficha y pasos).
"""
import re
import shutil
import sys
import zipfile
from pathlib import Path
from xml.sax.saxutils import escape

sys.path.insert(0, str(Path(__file__).parent))
from build_test_plan_xlsx import parse_sprint3_md, parse_sprint3_tests, QA_MD  # noqa: E402
from sprint4_cases import STORIES  # noqa: E402

BORDER = '<w:tcBorders>' + ''.join(f'<w:{s} w:val="single" w:color="000000" w:sz="8"/>' for s in ("top", "left", "bottom", "right")) + '</w:tcBorders>'
MAR = lambda v: f'<w:tcMar><w:top w:type="dxa" w:w="{v}"/><w:left w:type="dxa" w:w="110"/><w:bottom w:type="dxa" w:w="{v}"/><w:right w:type="dxa" w:w="110"/></w:tcMar>'
TBL_PR = ('<w:tblPr><w:tblW w:type="dxa" w:w="9026"/><w:tblBorders>'
          + ''.join(f'<w:{s} w:val="single" w:color="auto" w:sz="4"/>' for s in ("top", "left", "bottom", "right", "insideH", "insideV"))
          + '</w:tblBorders></w:tblPr>')
FONT = '<w:rFonts w:ascii="Arial" w:cs="Arial" w:eastAsia="Arial" w:hAnsi="Arial"/>'


def para(text, size=20, bold=False, color=None, keep=True):
    rpr = FONT + ('<w:b/><w:bCs/>' if bold else '') + (f'<w:color w:val="{color}"/>' if color else '') + f'<w:sz w:val="{size}"/><w:szCs w:val="{size}"/>'
    runs = []
    for i, line in enumerate(str(text).split("\n")):
        br = '<w:br/>' if i else ''
        runs.append(f'<w:r><w:rPr>{rpr}</w:rPr>{br}<w:t xml:space="preserve">{escape(line)}</w:t></w:r>')
    return f'<w:p><w:pPr>{"<w:keepNext/>" if keep else ""}<w:spacing w:after="0"/></w:pPr>{"".join(runs)}</w:p>'


def cell(width, text, fill=None, bold=False, color=None, keep=True):
    shd = f'<w:shd w:fill="{fill}" w:color="auto" w:val="clear"/>' if fill else ''
    return (f'<w:tc><w:tcPr><w:tcW w:type="dxa" w:w="{width}"/>{BORDER}{shd}{MAR(70)}<w:vAlign w:val="top"/></w:tcPr>'
            f'{para(text, 20, bold, color, keep)}</w:tc>')


def title_table(text):
    return (f'<w:tbl>{TBL_PR}<w:tblGrid><w:gridCol w:w="9026"/></w:tblGrid><w:tr><w:trPr><w:cantSplit/></w:trPr><w:tc><w:tcPr>'
            f'<w:tcW w:type="dxa" w:w="9026"/><w:tcBorders>' + ''.join(f'<w:{s} w:val="single" w:color="203864" w:sz="8"/>' for s in ("top", "left", "bottom", "right"))
            + f'</w:tcBorders><w:shd w:fill="203864" w:color="auto" w:val="clear"/>{MAR(90)}</w:tcPr>'
            f'{para(text, 22, True, "FFFFFF")}</w:tc></w:tr></w:tbl>')


def info_table(rows):
    body = ''.join(f'<w:tr><w:trPr><w:cantSplit/></w:trPr>{cell(2000, k, "EAF1F9", True, "203864")}{cell(7026, v)}</w:tr>' for k, v in rows)
    return f'<w:tbl>{TBL_PR}<w:tblGrid><w:gridCol w:w="2000"/><w:gridCol w:w="7026"/></w:tblGrid>{body}</w:tbl>'


def steps_table(steps):
    widths = (760, 3700, 1950, 2616)
    head = '<w:tr><w:trPr><w:cantSplit/><w:tblHeader/></w:trPr>' + ''.join(cell(w, t, "203864", True, "FFFFFF") for w, t in zip(widths, ("#", "Paso", "Dato", "Esperamos"))) + '</w:tr>'
    rows = ''
    for i, (n, p, d, e) in enumerate(steps):
        keep = i < len(steps) - 1
        rows += '<w:tr><w:trPr><w:cantSplit/></w:trPr>' + ''.join(cell(w, t, keep=keep) for w, t in zip(widths, (n, p, d or "—", e))) + '</w:tr>'
    grid = ''.join(f'<w:gridCol w:w="{w}"/>' for w in widths)
    return f'<w:tbl>{TBL_PR}<w:tblGrid>{grid}</w:tblGrid>{head}{rows}</w:tbl>'


SPACER = '<w:p><w:pPr><w:spacing w:after="240" w:before="0"/></w:pPr></w:p>'


def heading2(text):
    return (f'<w:p><w:pPr><w:pStyle w:val="Heading2"/><w:keepNext/><w:spacing w:after="160" w:before="240"/></w:pPr><w:r><w:rPr>{FONT}'
            f'<w:b/><w:bCs/><w:color w:val="1F3864"/><w:sz w:val="32"/><w:szCs w:val="32"/></w:rPr><w:t xml:space="preserve">{escape(text)}</w:t></w:r></w:p>')


def case(cid, title, desc, pre, jira, test, steps):
    return (title_table(f"{cid} — {title}")
            + info_table([("Description", desc), ("Pre-conditions", pre), ("Jira Requirement", jira), ("Test (xUnit)", test)])
            + steps_table(steps) + SPACER)


def main():
    src, dst = Path(sys.argv[1]), Path(sys.argv[2])
    with zipfile.ZipFile(src) as z:
        xml = z.read("word/document.xml").decode("utf8")
    if "Sprint 3 —" in xml:
        sys.exit("El documento ya tiene las secciones del Sprint 3/4; partir del original.")

    s3 = sorted(parse_sprint3_md(QA_MD), key=lambda t: int(t["id"][3:]))
    tests = parse_sprint3_tests()
    last_tc = max(int(t["id"][3:]) for t in s3)
    last_ta = max(int(m) for m in re.findall(r"TA-(\d+) —", xml))

    # ---- Parte A: Sprint 3 agrupado por módulo
    part_a = ""
    modules = {}
    for t in s3:
        modules.setdefault(t["module"], []).append(t)
    for module, items in modules.items():
        part_a += heading2(f"Sprint 3 — {module}")
        for t in items:
            ui = t["kind"] == "UI"
            pre = ("Stack local levantado; usuarios de prueba *@test.com." if ui
                   else "API levantada con WebApplicationFactory; sesión con el rol indicado en los pasos.")
            part_a += case(t["id"], t["title"], t["title"], pre, t["jira"], t["test"],
                           [("1", t["step"], "—", t["expected"])])

    # ---- Parte A: Sprint 4 desde los criterios de aceptación
    n = last_tc
    for jira, module, cases in STORIES:
        part_a += heading2(f"Sprint 4 — {module} ({jira})")
        for title, desc, pre, steps in cases:
            n += 1
            part_a += case(f"TC-{n}", title, desc, pre, jira, "Sin automatizar (Sprint 4 sin desarrollo todavía)",
                           [(str(i), p, d, e) for i, (p, d, e) in enumerate(steps, 1)])

    # ---- Parte B: Sprint 3 automatizados (misma derivación que el Excel)
    by_method = {}
    for t in s3:
        if t["kind"] == "API":
            for name in t["test"].split(","):
                by_method.setdefault(name.strip(), t)
    part_b = ""
    last_module = None
    k = last_ta
    for t in tests:
        k += 1
        if t["module"] != last_module:
            part_b += heading2(f"{t['sprint']} — Pruebas automatizadas — {t['module']}")
            last_module = t["module"]
        tc = by_method.get(t["method"])
        pre = "API levantada con WebApplicationFactory." if t["proj"].endswith("IntegrationTests") else "Servicio con DbContext InMemory."
        if tc:
            title, desc = f"Validar: {tc['title']}", f"Respalda {tc['id']}: {tc['expected']}"
            steps = [("1", tc["step"], "—", tc["expected"])]
        else:
            title, desc = f"Validar: {t['human']}", "Sin TC manual asociado; descripción derivada del nombre del test."
            steps = [("1", "Ejecutar el test", "—", "El test pasa")]
        part_b += case(f"TA-{k:03d}", title, desc, pre, t["jira"], f"{t['proj']} › {t['cls']} › {t['method']}", steps)

    # insertar Parte A antes del título de la Parte B, Parte B antes de sectPr
    marker = xml.find("Parte B — Casos de prueba automatizados")
    h1 = xml.rfind("<w:p>", 0, marker)
    xml = xml[:h1] + part_a + xml[h1:]
    sect = xml.rfind("<w:sectPr>")
    xml = xml[:sect] + part_b + xml[sect:]

    shutil.copy(src, dst) if src != dst else None
    tmp = dst.with_suffix(".tmp")
    with zipfile.ZipFile(src) as zin, zipfile.ZipFile(tmp, "w", zipfile.ZIP_DEFLATED) as zout:
        for item in zin.infolist():
            data = xml.encode("utf8") if item.filename == "word/document.xml" else zin.read(item.filename)
            zout.writestr(item, data)
    tmp.replace(dst)
    print(f"{dst} — Parte A: +{len(s3)} (Sprint 3) +{n - last_tc} (Sprint 4) | Parte B: +{len(tests)} (Sprint 3 y 4)")


if __name__ == "__main__":
    main()
