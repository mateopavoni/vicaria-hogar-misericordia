export type LifeHistoryStage = 'beforeHome' | 'duringHome' | 'afterHome';

// una entrada histórica de la etapa (bug reportado 2026-09-23: guardar pisaba el
// contenido anterior en vez de sumar una entrada nueva)
export interface LifeHistoryEntry {
  id: string;
  text: string;
  authoredBy: string | null;
  authoredAt: string;
}

export interface LifeHistorySection {
  text: string | null;
  lastEditedBy: string | null;
  lastEditedAt: string | null;
  entries: LifeHistoryEntry[];
}

export interface LifeHistory {
  beforeHome: LifeHistorySection | null;
  duringHome: LifeHistorySection | null;
  afterHome: LifeHistorySection | null;
}