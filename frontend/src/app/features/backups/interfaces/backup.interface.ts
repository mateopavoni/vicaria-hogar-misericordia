export type BackupStatus = 'Completed' | 'Failed';

/** SCRUM-267: un respaldo de la base de datos (uno por día). */
export interface BackupRecord {
  id: string;
  createdAt: string;           // ISO
  sizeBytes: number | null;    // null si falló
  status: BackupStatus;
  errorMessage?: string | null;
}

export interface BackupDownload {
  blob: Blob;
  filename: string;
}

export type BackupBannerTone = 'success' | 'warning' | 'danger' | 'neutral';

export interface BackupBanner {
  tone: BackupBannerTone;
  title: string;
  message: string;
}
