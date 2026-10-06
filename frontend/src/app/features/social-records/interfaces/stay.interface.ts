// opciones que ofrece el modal de egreso (subset de StayExitReason del backend)
export type ExitReason = 'Alta voluntaria' | 'Derivación' | 'Otro';

// mapeo a StayExitReason del backend (Domain/Entities/StayExitReason.cs), que no serializa como string
export const EXIT_REASON_TO_BACKEND: Record<ExitReason, number> = {
  'Alta voluntaria': 0, // VoluntaryDischarge
  'Derivación': 2,      // Referral
  'Otro': 4,             // Other
};

const EXIT_REASON_LABELS: Record<number, string> = {
  0: 'Alta voluntaria',
  1: 'Alta decidida por el equipo',
  2: 'Derivación',
  3: 'Abandono',
  4: 'Otro',
};

export function getExitReasonLabel(exitReason: number | string | null | undefined): string {
  if (exitReason === null || exitReason === undefined) {
    return '';
  }
  return EXIT_REASON_LABELS[Number(exitReason)] ?? String(exitReason);
}

export interface Stay {
  id?: string;
  entryDate: string;        // Fecha de ingreso (ISO string)
  exitDate?: string | null;  // Fecha de egreso (ISO string, opcional si está activa)
  exitReason?: number | string | null; // el backend lo serializa como numero (StayExitReason)
  durationInDays?: number;
}

