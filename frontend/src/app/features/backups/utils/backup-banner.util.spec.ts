import { BackupRecord } from '../interfaces/backup.interface';
import { backupBanner, sortBackups } from './backup-banner.util';

const rec = (createdAt: string, status: 'Completed' | 'Failed' = 'Completed', errorMessage: string | null = null): BackupRecord => ({
  id: createdAt, createdAt, status, sizeBytes: status === 'Completed' ? 1000 : null, errorMessage,
});
const now = new Date('2026-10-14T12:00:00Z');

describe('backupBanner', () => {
  it('sin respaldos es neutral', () => {
    expect(backupBanner([], now).tone).toBe('neutral');
  });

  it('último correcto y reciente es success', () => {
    expect(backupBanner([rec('2026-10-14T03:00:00Z')], now).tone).toBe('success');
  });

  it('último fallido es danger y menciona el aviso por email', () => {
    const banner = backupBanner([rec('2026-10-13T03:00:00Z'), rec('2026-10-14T03:00:00Z', 'Failed', 'Sin espacio.')], now);
    expect(banner.tone).toBe('danger');
    expect(banner.message.includes('Sin espacio.')).toBe(true);
    expect(banner.message.includes('email')).toBe(true);
  });

  it('último correcto pero viejo es warning', () => {
    expect(backupBanner([rec('2026-10-12T03:00:00Z')], now).tone).toBe('warning');
  });

  it('ordena del más nuevo al más viejo', () => {
    const sorted = sortBackups([rec('2026-10-01T03:00:00Z'), rec('2026-10-03T03:00:00Z')]);
    expect(sorted[0].createdAt).toBe('2026-10-03T03:00:00Z');
  });

  it('un respaldo en curso no tapa el resultado del último terminado', () => {
    const inProgress: BackupRecord = { ...rec('2026-10-14T03:00:00Z'), status: 'InProgress', sizeBytes: null };
    const failed: BackupRecord = { ...rec('2026-10-13T03:00:00Z'), status: 'Failed', sizeBytes: null };
    expect(backupBanner([inProgress, failed], now).tone).toBe('danger');
  });
});
