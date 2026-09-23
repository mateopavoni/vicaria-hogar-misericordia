export type LifeHistoryStage = 'beforeHome' | 'duringHome' | 'afterHome';

export interface LifeHistorySection {
  text: string | null;
  lastEditedBy: string | null;
  lastEditedAt: string | null;
}

export interface LifeHistory {
  beforeHome: LifeHistorySection | null;
  duringHome: LifeHistorySection | null;
  afterHome: LifeHistorySection | null;
}