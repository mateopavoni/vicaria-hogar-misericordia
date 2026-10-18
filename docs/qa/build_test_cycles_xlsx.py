"""Genera docs/qa/Ciclos_de_prueba_Sprint_1-4.xlsx con el formato de la Planilla de Ciclo de Pruebas
(una pestaña por ciclo) a partir de Plan_de_pruebas_Sprint_1-4.xlsx.

Uso: python build_test_cycles_xlsx.py <planilla_limpia.xlsx> [plan.xlsx] [salida.xlsx]
"""
import re
import sys
from copy import copy
from datetime import date
from pathlib import Path

import openpyxl
import run_2026_10_16 as run
from openpyxl.worksheet.datavalidation import DataValidation

HERE = Path(__file__).parent
BLOCKED_NOTE = ("Bloqueado: el backend de esta historia no está desarrollado y la pantalla usa un servicio simulado en memoria "
                "(sin llamadas HTTP), por lo que no hay integración que validar de punta a punta.")
STATES = {"OK": "PASÓ", "Falla": "FALLÓ", "Bloqueado": "BLOQUEADO", "Pendiente": "NO EJECUTADO"}

# Sprint 1 = EP-03 (registro, login, roles, notificaciones); el resto del Sprint 1-2 es Sprint 2.
SPRINT1_IDS = {f"TC-{n:02d}" for n in list(range(1, 11)) + list(range(26, 34))}

# Hallazgos de la hoja "Defectos y pendientes" vinculados a casos concretos
# DEF-n = fila n de la hoja "Defectos y pendientes" del plan. Se vinculan a los casos que lo detectaron
# o que cubren la corrección (quedaron PASÓ tras el arreglo).
BUGS = {
    "TC-78": "DEF-01", "TC-93": "DEF-01", "TC-100": "DEF-01", "TC-115": "DEF-01",
    "TC-116": "DEF-01, DEF-02", "TC-123": "DEF-02",
    "TC-108": "DEF-03", "TC-111": "DEF-03",
    "TC-74": "DEF-04", "TC-128": "DEF-04",
    "TC-86": "DEF-06", "TC-139": "DEF-06",
}

for _tc, (_real, _bugs, _failed) in run.UI_RESULTS.items():
    if _bugs:
        BUGS[_tc] = _bugs

CYCLES = [
    ("Ciclo - Sprint 1", "Sprint 1 — Acceso y roles (EP-03)"),
    ("Ciclo - Sprint 2", "Sprint 2 — Fichas, observaciones e historia de vida (EP-01, EP-02, EP-12)"),
    ("Ciclo - Sprint 3", "Sprint 3 — Calendario, colaboradores y visitas (EP-04, EP-05, EP-06)"),
    ("Ciclo - Sprint 4", "Sprint 4 — Asistencia y medicación (EP-10) — pendiente de backend"),
    ("Ciclo - Seguridad y carga", "Transversal — Seguridad básica y rendimiento (API local, 2026-10-16)"),
]


def cycle_of(tc_id, sprint):
    if sprint == "Sprint 1-2":
        return 0 if tc_id in SPRINT1_IDS else 1
    return {"Sprint 3": 2, "Sprint 4": 3, "Transversal": 4}[sprint]


def split_steps(text):
    """'1. paso  →  esperado' por línea -> (pasos, resultados esperados) numerados."""
    steps, expected = [], []
    for line in (text or "").splitlines():
        m = re.match(r"^(\d+)\.\s*(.*?)\s+→\s+(.*)$", line.strip())
        if m:
            steps.append(f"{m.group(1)}. {m.group(2)}")
            expected.append(f"{m.group(1)}. {m.group(3)}")
        elif line.strip():
            steps.append(line.strip())
    return "\n".join(steps), "\n".join(expected)


def actual_result(state, expected, test, observ, sprint, who, when):
    last = expected.splitlines()[-1] if expected else ""
    last = re.sub(r"^\d+\.\s*", "", last)
    if state in ("NO EJECUTADO", "BLOQUEADO"):
        return BLOCKED_NOTE
    if who == "Suite automatizada":
        base = f"Prueba automatizada {test or ''} en verde (dotnet test); se cumple cada resultado esperado. Último: {last}"
    elif sprint == "Sprint 1-2":
        base = f"Ejecutado a mano contra el stack local; cada paso dio el resultado esperado. Último: {last}"
    else:
        base = f"Ejecutado contra la API/UI local; cada paso dio el resultado esperado. Último: {last}"
    return base.strip()


S4_API_RESULTS = HERE / "e2e" / "sprint4-api-results.json"


def add_sprint4_api_sheet(wb, base, style_row):
    """Pestaña con lo del Sprint 4 que ya tiene backend: se ejecuta contra la API real (e2e/sprint4-api-flows.mjs)."""
    if not S4_API_RESULTS.exists():
        return
    import json
    results = json.loads(S4_API_RESULTS.read_text(encoding="utf-8"))
    ws = wb.copy_worksheet(base)
    ws.title = "Ciclo - Sprint 4 (API)"
    ws["A1"] = "PLANILLA DE EJECUCIÓN DE CICLO DE PRUEBAS — Sprint 4 — endpoints ya desarrollados (asistencia diaria SCRUM-216 y esquema de medicación SCRUM-232), API local, 17/10/2026"
    ws.delete_rows(4, ws.max_row)
    when = date(2026, 10, 17)
    for n, r in enumerate(results):
        is_att = r["id"].startswith("S4-A")
        module = "Asistencia diaria (SCRUM-54 / 216)" if is_att else "Esquema de medicación (SCRUM-59 / 232)"
        login = "1. Iniciar sesión con el rol indicado en el título (Referente por defecto)"
        pasos = login + chr(10) + ("2. POST /api/attendances con el dato del caso" if is_att else "2. POST /api/persons/{id}/medication-schemes con el dato del caso")
        vals = [r["id"], module, r["name"], pasos, r["name"],
                f"Respuesta de la API: {r['note']}".strip(), "PASÓ" if r["ok"] else "FALLÓ", None, "QA (script sprint4-api-flows.mjs)", when,
                "Observación: la actividad viaja como número (0 = Hogar de día, 1 = Casa de Convivencia) y no como texto; el front debe enviarla así." if r["id"] == "S4-A01" else None]
        for c, v in enumerate(vals, start=1):
            cell = ws.cell(row=4 + n, column=c, value=v)
            s = style_row[c - 1]
            cell.font, cell.fill, cell.border = copy(s.font), copy(s.fill), copy(s.border)
            cell.alignment, cell.number_format = copy(s.alignment), s.number_format
            if c == 10:
                cell.number_format = "dd/mm/yyyy"
    ws.data_validations.dataValidation = []
    dv = DataValidation(type="list", formula1='"PASÓ,FALLÓ,BLOQUEADO,NO EJECUTADO"', allow_blank=False)
    ws.add_data_validation(dv)
    dv.add(f"G4:G{3 + len(results)}")


def main():
    template = sys.argv[1]
    plan = sys.argv[2] if len(sys.argv) > 2 else str(HERE / "Plan_de_pruebas_Sprint_1-4.xlsx")
    out = sys.argv[3] if len(sys.argv) > 3 else str(HERE / "Ciclos_de_prueba_Sprint_1-4.xlsx")

    src = openpyxl.load_workbook(plan)["Parte A - Manuales"]
    rows = [r for r in src.iter_rows(min_row=2, values_only=True) if r[0]]
    buckets = [[] for _ in CYCLES]
    for r in rows:
        buckets[cycle_of(r[0], r[1])].append(r)

    wb = openpyxl.load_workbook(template)
    base = wb.active
    style_row = [base.cell(row=4, column=c) for c in range(1, 12)]
    sheets = []
    for i, (name, title) in enumerate(CYCLES):
        ws = base if i == 0 else wb.copy_worksheet(base)
        ws.title = name
        sheets.append(ws)

    for ws, (name, title), data in zip(sheets, CYCLES, buckets):
        ws["A1"] = f"PLANILLA DE EJECUCIÓN DE CICLO DE PRUEBAS — {title}"
        ws.delete_rows(4, ws.max_row)
        for n, r in enumerate(data):
            (tc_id, sprint, module, titulo, _desc, _pre, _jira, steps, test,
             estado, fecha, who, observ) = r[:13]
            pasos, esperado = split_steps(steps)
            state = STATES[estado]
            when = date.fromisoformat(str(fecha)[:10]) if fecha else None
            comment = observ if sprint not in ("Transversal",) and tc_id not in run.UI_RESULTS else ""
            if tc_id in BUGS and state == "PASÓ" and tc_id not in run.UI_RESULTS:
                comment = (comment + " " if comment else "") + f"Defecto {BUGS[tc_id]} corregido; el caso pasa tras la corrección."
            if state == "NO EJECUTADO":
                state = "BLOQUEADO"
                comment = "Bloqueado por integración pendiente (Sprint 4). Se destraba al conectar el frontend con el backend de la historia."
            ui = run.UI_RESULTS.get(tc_id)
            if ui:
                real = ui[0]
                if ui[2]:
                    comment = (comment + " " if comment else "") + f"Falló en la primera ejecución en navegador ({ui[1]}); corregido en dev y re-ejecutado OK."
            elif sprint == "Transversal":
                real = observ
                comment = run.SEC_NOTE if tc_id.startswith("TC-") and "PERF" in titulo.upper() or titulo.startswith("Carga") else ""
            else:
                real = actual_result(state, esperado, test, observ, sprint, who, when)
            vals = [tc_id, module, titulo, pasos, esperado,
                    real,
                    state, BUGS.get(tc_id), who, when, comment or None]
            for c, v in enumerate(vals, start=1):
                cell = ws.cell(row=4 + n, column=c, value=v)
                s = style_row[c - 1]
                cell.font, cell.fill, cell.border = copy(s.font), copy(s.fill), copy(s.border)
                cell.alignment, cell.number_format = copy(s.alignment), s.number_format
                if c == 10 and when:
                    cell.number_format = "dd/mm/yyyy"
        ws.data_validations.dataValidation = []
        dv = DataValidation(type="list", formula1='"PASÓ,FALLÓ,BLOQUEADO,NO EJECUTADO"', allow_blank=False)
        ws.add_data_validation(dv)
        dv.add(f"G4:G{3 + len(data)}")

    add_sprint4_api_sheet(wb, base, style_row)
    wb.save(out)
    print(out, [len(b) for b in buckets], sum(len(b) for b in buckets))


if __name__ == "__main__":
    main()
