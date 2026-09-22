export type ExitReason = 'Alta voluntaria' | 'Derivación' | 'Otro';

export interface Stay {
  id?: string;
  entryDate: string;        // Fecha de ingreso (ISO string)
  exitDate?: string | null;  // Fecha de egreso (ISO string, opcional si está activa)
  exitReason?: ExitReason | string | null;
  durationInDays?: number;   // Calculado
}

