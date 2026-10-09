/**
 * Fecha de hoy en zona horaria LOCAL, formato YYYY-MM-DD (para <input type="date">).
 * `new Date().toISOString().substring(0, 10)` (usado antes en varios modales) devuelve
 * la fecha en UTC: en Argentina (UTC-3), desde las 21:00 ya da el día siguiente.
 */
export function todayLocalIso(): string {
  const d = new Date();
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
}

/**
 * true si `dateIso` (YYYY-MM-DD) es anterior al día de hoy (zona local). Las fechas en
 * ese formato se pueden comparar como strings sin parsear (orden lexicográfico ==
 * orden cronológico), evitando problemas de huso horario de `new Date(dateIso)`.
 *
 * Usado en todos los formularios de calendario (eventos general/personal, visitas de
 * la Casa de Convivencia) para la regla "la fecha no puede ser anterior a hoy" — no
 * aplica si `dateIso` está vacío, eso lo cubre el validator de `required` de cada form.
 */
export function isBeforeToday(dateIso: string): boolean {
  return !!dateIso && dateIso < todayLocalIso();
}
