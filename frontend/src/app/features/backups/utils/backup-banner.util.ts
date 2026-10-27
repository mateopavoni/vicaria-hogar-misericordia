import { BackupBanner, BackupRecord } from '../interfaces/backup.interface';

/** Cantidad mínima de respaldos diarios que se conservan (criterio de aceptación). */
export const BACKUP_RETENTION = 30;

/** Pasadas estas horas sin un respaldo nuevo se avisa que el diario no corrió. */
export const BACKUP_STALE_HOURS = 36;

export function sortBackups(records: BackupRecord[]): BackupRecord[] {
  return [...records].sort((a, b) => b.createdAt.localeCompare(a.createdAt));
}

/** Banner con el resultado del último respaldo. */
export function backupBanner(records: BackupRecord[], now: Date): BackupBanner {
  // un respaldo en curso todavía no tiene resultado: el banner informa el último terminado
  const last = sortBackups(records).find((r) => r.status !== 'InProgress');
  if (!last) {
    return { tone: 'neutral', title: 'Todavía no hay respaldos', message: 'El primer respaldo automático se genera en la próxima corrida diaria.' };
  }

  if (last.status === 'Failed') {
    return {
      tone: 'danger',
      title: 'El último respaldo falló',
      message: `${last.errorMessage ? last.errorMessage + ' ' : ''}Se avisó por email al administrador. Revisá la configuración del almacenamiento externo.`,
    };
  }

  const hours = (now.getTime() - new Date(last.createdAt).getTime()) / 3_600_000;
  if (hours > BACKUP_STALE_HOURS) {
    return {
      tone: 'warning',
      title: 'No hay un respaldo reciente',
      message: `El último respaldo correcto tiene más de ${BACKUP_STALE_HOURS} horas. El respaldo diario podría no estar corriendo.`,
    };
  }

  return { tone: 'success', title: 'Último respaldo correcto', message: 'El respaldo automático diario funciona con normalidad.' };
}
