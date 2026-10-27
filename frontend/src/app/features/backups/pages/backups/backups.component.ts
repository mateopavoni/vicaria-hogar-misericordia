import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { extractApiError } from '../../../../core/http/api-error';
import { UiButtonComponent } from '../../../../shared/ui';
import { formatFileSize } from '../../../attachments/utils/attachment-validation.util';
import { BackupRecord } from '../../interfaces/backup.interface';
import { BackupsService } from '../../services/backups.service';
import { BACKUP_RETENTION, backupBanner } from '../../utils/backup-banner.util';

/**
 * SCRUM-267: panel de respaldos (/dashboard/configuracion/respaldos). SMART: tabla con fecha,
 * tamaño y estado, banner con el resultado del último respaldo y descarga manual.
 * Solo con permiso configuracion.respaldos (Referente).
 */
@Component({
  selector: 'app-backups',
  imports: [DatePipe, UiButtonComponent],
  templateUrl: './backups.component.html',
})
export class BackupsComponent implements OnInit {
  private backupsService = inject(BackupsService);

  readonly retention = BACKUP_RETENTION;
  readonly formatSize = formatFileSize;

  records = signal<BackupRecord[]>([]);
  loading = signal(true);
  errorMessage = signal<string | null>(null);
  downloadingId = signal<string | null>(null);

  banner = computed(() => backupBanner(this.records(), new Date()));

  bannerClass = computed(() => {
    switch (this.banner().tone) {
      case 'success': return 'bg-emerald-50 text-emerald-800';
      case 'warning': return 'bg-amber-50 text-amber-800';
      case 'danger': return 'bg-red-50 text-red-700';
      default: return 'bg-slate-100 text-slate-700';
    }
  });

  ngOnInit(): void {
    this.backupsService.list().subscribe({
      next: (records) => {
        this.records.set(records);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(extractApiError(err, 'No se pudieron cargar los respaldos.'));
      },
    });
  }

  download(record: BackupRecord): void {
    if (this.downloadingId()) {
      return;
    }
    this.errorMessage.set(null);
    this.downloadingId.set(record.id);
    this.backupsService.download(record.id).subscribe({
      next: ({ blob, filename }) => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        link.remove();
        setTimeout(() => URL.revokeObjectURL(url), 1000);
        this.downloadingId.set(null);
      },
      error: (err) => {
        this.downloadingId.set(null);
        this.errorMessage.set(extractApiError(err, 'No se pudo descargar el respaldo.'));
      },
    });
  }
}
