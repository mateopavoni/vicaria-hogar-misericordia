import { Injectable } from '@angular/core';
import { Observable, delay, of, throwError } from 'rxjs';
import { BackupDownload, BackupRecord } from '../interfaces/backup.interface';
import { BACKUP_RETENTION, backupFilename, sortBackups } from '../utils/backup-banner.util';

/**
 * SCRUM-267: respaldos de la base de datos.
 *
 * El backend todavía no expone respaldos. Mientras tanto este servicio devuelve datos DE EJEMPLO
 * (30 respaldos diarios, uno fallido) y un archivo de demostración. Para conectarlo solo hay que
 * reemplazar el cuerpo de `list()` por `GET /api/backups` y el de `download()` por
 * `GET /api/backups/{id}/download` con `responseType: 'blob'`.
 */
@Injectable({ providedIn: 'root' })
export class BackupsService {
  private readonly records: BackupRecord[];

  constructor() {
    this.records = BackupsService.sampleData(new Date());
  }

  list(): Observable<BackupRecord[]> {
    return of(sortBackups(this.records)).pipe(delay(200));
  }

  download(id: string): Observable<BackupDownload> {
    const record = this.records.find((r) => r.id === id);
    if (!record) {
      return throwError(() => ({ error: { message: 'El respaldo ya no existe.' } }));
    }
    if (record.status === 'Failed') {
      return throwError(() => ({ error: { message: 'Ese respaldo falló: no hay archivo para descargar.' } }));
    }
    const text = `ARCHIVO DE DEMOSTRACIÓN: el backend de respaldos todavía no está conectado.\nRespaldo del ${record.createdAt}\n`;
    return of({
      blob: new Blob([text], { type: 'text/plain' }),
      filename: backupFilename(record).replace('.sql.gz', '-DEMO.txt'),
    }).pipe(delay(200));
  }

  static sampleData(now: Date): BackupRecord[] {
    const out: BackupRecord[] = [];
    for (let i = 0; i < BACKUP_RETENTION; i++) {
      const date = new Date(now);
      date.setUTCDate(now.getUTCDate() - i);
      date.setUTCHours(3, 0, 0, 0);
      if (date.getTime() > now.getTime()) {
        date.setUTCDate(date.getUTCDate() - 1);
      }
      const failed = i === 9;
      out.push({
        id: `backup-${i}`,
        createdAt: date.toISOString(),
        sizeBytes: failed ? null : 18_000_000 + (i % 7) * 350_000,
        status: failed ? 'Failed' : 'Completed',
        errorMessage: failed ? 'No se pudo subir el archivo al almacenamiento externo.' : null,
      });
    }
    return out;
  }
}
