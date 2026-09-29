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
