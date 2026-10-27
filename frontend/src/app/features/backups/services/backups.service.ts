import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { BackupDownload, BackupRecord, BackupStatus } from '../interfaces/backup.interface';
import { BACKUP_RETENTION, sortBackups } from '../utils/backup-banner.util';

/** BackupStatus del backend (serializado como número). */
const STATUS: Record<number, BackupStatus> = { 1: 'InProgress', 2: 'Completed', 3: 'Failed' };

/** BackupDto del backend (SCRUM-266). */
interface BackupDto {
  id: string;
  status: number;
  createdAt: string;
  sizeBytes: number;
  errorMessage: string | null;
}

/**
 * SCRUM-267: respaldos de la base de datos contra la API (SCRUM-266): listado de los últimos
 * (GET /api/admin/backups, una página con los que se conservan) y descarga del archivo.
 */
@Injectable({ providedIn: 'root' })
export class BackupsService {
  private readonly http = inject(HttpClient);

  list(): Observable<BackupRecord[]> {
    const params = new HttpParams().set('page', 1).set('pageSize', BACKUP_RETENTION);
    return this.http
      .get<{ items: BackupDto[] }>('/api/admin/backups', { params })
      .pipe(map((page) => sortBackups(page.items.map(toRecord))));
  }

  download(id: string): Observable<BackupDownload> {
    return this.http
      .get(`/api/admin/backups/${id}/download`, { responseType: 'blob', observe: 'response' })
      .pipe(
        map((response) => ({
          blob: response.body!,
          filename: filenameFrom(response.headers.get('Content-Disposition')) ?? `respaldo-${id}.bak`,
        })),
      );
  }
}

function toRecord(dto: BackupDto): BackupRecord {
  const status = STATUS[dto.status] ?? 'Failed';
  return {
    id: dto.id,
    createdAt: dto.createdAt.endsWith('Z') ? dto.createdAt : `${dto.createdAt}Z`,
    sizeBytes: status === 'Completed' ? dto.sizeBytes : null,
    status,
    errorMessage: dto.errorMessage,
  };
}

/** Nombre del archivo del header Content-Disposition (filename= o filename*=UTF-8''). */
function filenameFrom(header: string | null): string | null {
  if (!header) return null;
  const encoded = /filename\*=UTF-8''([^;]+)/i.exec(header);
  if (encoded) return decodeURIComponent(encoded[1]);
  const plain = /filename="?([^";]+)"?/i.exec(header);
  return plain ? plain[1] : null;
}
