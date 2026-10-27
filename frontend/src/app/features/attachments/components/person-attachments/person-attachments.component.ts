import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { AuthService } from '../../../../core/auth/auth.service';
import { PermissionService } from '../../../../core/auth/permission.service';
import { extractApiError } from '../../../../core/http/api-error';
import { UiButtonComponent, UiConfirmDialogComponent } from '../../../../shared/ui';
import { PersonAttachment } from '../../interfaces/attachment.interface';
import { PersonAttachmentsService } from '../../services/person-attachments.service';
import { formatFileSize, validateAttachment } from '../../utils/attachment-validation.util';
import { AttachmentViewerModalComponent } from '../attachment-viewer-modal/attachment-viewer-modal.component';

/**
 * SCRUM-256: sección "Documentos" del perfil de la persona (SMART). Carga múltiple de imágenes
 * (JPG, PNG) y PDF de hasta 10 MB cada uno; listado con ícono por tipo, nombre, fecha y usuario;
 * visor para imágenes, descarga de PDF y eliminación con confirmación.
 * Ver: adjuntos.view; subir: adjuntos.create; eliminar: adjuntos.delete (solo Referente).
 */
@Component({
  selector: 'app-person-attachments',
  imports: [DatePipe, UiButtonComponent, UiConfirmDialogComponent, AttachmentViewerModalComponent],
  templateUrl: './person-attachments.component.html',
})
export class PersonAttachmentsComponent implements OnInit {
  private service = inject(PersonAttachmentsService);
  private authService = inject(AuthService);
  private permissionService = inject(PermissionService);

  personId = input.required<string>();

  readonly accept = '.jpg,.jpeg,.png,.pdf,image/jpeg,image/png,application/pdf';
  readonly formatSize = formatFileSize;

  attachments = signal<PersonAttachment[]>([]);
  loading = signal(true);
  uploading = signal(false);
  /** Un mensaje por archivo rechazado. */
  uploadErrors = signal<string[]>([]);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  private successTimeout: ReturnType<typeof setTimeout> | null = null;

  viewing = signal<PersonAttachment | null>(null);
  pendingDelete = signal<PersonAttachment | null>(null);
  deleting = signal(false);

  canCreate = computed(() => this.permissionService.hasPermission('adjuntos.create'));
  canDelete = computed(() => this.permissionService.hasPermission('adjuntos.delete'));

  ngOnInit(): void {
    this.reload();
  }

  private reload(): void {
    this.service.getByPerson(this.personId()).subscribe({
      next: (list) => {
        this.attachments.set(list);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.errorMessage.set('No se pudieron cargar los documentos.');
      },
    });
  }

  /** Sube varios archivos: los válidos se cargan y de los demás se informa el motivo. */
  onFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    input.value = ''; // permite volver a elegir el mismo archivo
    const user = this.authService.user();
    if (!user || !this.canCreate() || files.length === 0) {
      return;
    }

    const errors: string[] = [];
    const valid: File[] = [];
    for (const file of files) {
      const error = validateAttachment(file);
      if (error) errors.push(error);
      else valid.push(file);
    }
    this.uploadErrors.set(errors);
    this.errorMessage.set(null);
    if (valid.length === 0) {
      return;
    }

    const actor = { id: user.id, name: `${user.name} ${user.lastname}`.trim() };
    this.uploading.set(true);
    let pending = valid.length;
    let uploaded = 0;
    const done = () => {
      if (--pending > 0) return;
      this.uploading.set(false);
      if (uploaded > 0) {
        this.showSuccess(uploaded === 1 ? 'Archivo adjuntado correctamente.' : `${uploaded} archivos adjuntados correctamente.`);
      }
      this.reload();
    };
    for (const file of valid) {
      this.service.upload(this.personId(), file, actor).subscribe({
        next: () => {
          uploaded++;
          done();
        },
        error: (err) => {
          this.uploadErrors.update((list) => [...list, extractApiError(err, `No se pudo adjuntar «${file.name}».`)]);
          done();
        },
      });
    }
  }

  confirmDelete(): void {
    const target = this.pendingDelete();
    if (!target || !this.canDelete()) {
      return;
    }
    this.deleting.set(true);
    this.service.delete(this.personId(), target.id).subscribe({
      next: () => {
        this.deleting.set(false);
        this.pendingDelete.set(null);
        this.showSuccess('Archivo eliminado.');
        this.reload();
      },
      error: (err) => {
        this.deleting.set(false);
        this.pendingDelete.set(null);
        this.errorMessage.set(extractApiError(err, 'No se pudo eliminar el archivo.'));
      },
    });
  }

  private showSuccess(message: string): void {
    this.successMessage.set(message);
    if (this.successTimeout) clearTimeout(this.successTimeout);
    this.successTimeout = setTimeout(() => this.successMessage.set(null), 4000);
  }
}
