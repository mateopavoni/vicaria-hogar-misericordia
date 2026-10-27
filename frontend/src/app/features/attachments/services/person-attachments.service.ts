import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, forkJoin, map, of, switchMap, throwError } from 'rxjs';
import { AttachmentActor, AttachmentKind, PersonAttachment } from '../interfaces/attachment.interface';
import { validateAttachment } from '../utils/attachment-validation.util';

/** PersonAttachmentDto del backend (SCRUM-255). */
interface AttachmentDto {
  id: string;
  personId: string;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  fileTypeIcon: string;
  uploadedAt: string;
  uploadedByUserId: string;
  uploadedByName: string;
}

/**
 * SCRUM-256: adjuntos de la ficha de una persona contra la API (SCRUM-255):
 * POST multipart, GET del listado, GET del archivo y DELETE (solo Referente).
 * El archivo se pide con HttpClient (lleva el token) y se expone como URL blob: en `url`,
 * así el visor y la descarga funcionan sin exponer el token en un <img src>.
 */
@Injectable({ providedIn: 'root' })
export class PersonAttachmentsService {
  private readonly http = inject(HttpClient);
  /** URLs blob: creadas por persona, para liberarlas al recargar. */
  private readonly objectUrls = new Map<string, string[]>();

  /** De más reciente a más antiguo. */
  getByPerson(personId: string): Observable<PersonAttachment[]> {
    return this.http.get<AttachmentDto[]>(`/api/persons/${personId}/attachments`).pipe(
      switchMap((list) => {
        this.revoke(personId);
        if (list.length === 0) {
          return of([]);
        }
        return forkJoin(list.map((dto) => this.withFileUrl(dto)));
      }),
      map((list) => {
        this.objectUrls.set(personId, list.map((a) => a.url));
        return list.sort((a, b) => b.uploadedAt.localeCompare(a.uploadedAt));
      }),
    );
  }

  upload(personId: string, file: File, _actor: AttachmentActor): Observable<PersonAttachment> {
    // mismas reglas que el backend (tipo y 10 MB) para avisar sin subir el archivo
    const error = validateAttachment(file);
    if (error) {
      return throwError(() => ({ error: { message: error } }));
    }
    const form = new FormData();
    form.append('files', file, file.name);
    return this.http
      .post<AttachmentDto[]>(`/api/persons/${personId}/attachments`, form)
      .pipe(map((created) => toAttachment(created[0], '')));
  }

  delete(personId: string, attachmentId: string): Observable<void> {
    return this.http.delete<void>(`/api/persons/${personId}/attachments/${attachmentId}`);
  }

  private withFileUrl(dto: AttachmentDto): Observable<PersonAttachment> {
    return this.http
      .get(`/api/persons/${dto.personId}/attachments/${dto.id}`, { responseType: 'blob' })
      .pipe(map((blob) => toAttachment(dto, URL.createObjectURL(blob))));
  }

  private revoke(personId: string): void {
    for (const url of this.objectUrls.get(personId) ?? []) {
      URL.revokeObjectURL(url);
    }
    this.objectUrls.delete(personId);
  }
}

function kindOf(contentType: string): AttachmentKind {
  return contentType === 'application/pdf' ? 'pdf' : 'image';
}

function toAttachment(dto: AttachmentDto, url: string): PersonAttachment {
  return {
    id: dto.id,
    personId: dto.personId,
    name: dto.fileName,
    kind: kindOf(dto.contentType),
    mimeType: dto.contentType,
    size: dto.fileSizeBytes,
    uploadedAt: dto.uploadedAt.endsWith('Z') ? dto.uploadedAt : `${dto.uploadedAt}Z`,
    uploadedByName: dto.uploadedByName,
    url,
  };
}
