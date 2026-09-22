

export interface ChangeHistoryItem<T = string> {
  id?: string;
  fieldName?: string;       // Opcional: Nombre legible del campo (ej. "Tipo de persona", "Estado")
  previousValue: T;        // Valor anterior
  newValue: T;             // Valor nuevo
  modifiedBy: string;      // Usuario/Operador que realizó la modificación
  modifiedAt: string;      // Fecha ISO (ej. "2026-09-16T14:00:00Z")
  reason?: string | null;  // Motivo opcional
}