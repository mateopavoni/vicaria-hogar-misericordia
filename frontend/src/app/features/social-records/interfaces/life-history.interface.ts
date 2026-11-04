export type LifeHistoryStage = 'beforeHome' | 'duringHome' | 'afterHome';

// una entrada del historial de la etapa
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