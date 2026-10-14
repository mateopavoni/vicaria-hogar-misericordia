import { Injectable } from '@angular/core';
import { Observable, delay, of, throwError } from 'rxjs';
import { AttachmentActor, PersonAttachment } from '../interfaces/attachment.interface';
import { attachmentKind, validateAttachment } from '../utils/attachment-validation.util';

/**
 * SCRUM-256: adjuntos de la ficha de una persona.
 *
 * OJO — backend pendiente (SCRUM-24): hoy no existe almacenamiento ni endpoints de archivos.
 * Hasta entonces los archivos viven en memoria del navegador (se pierden al recargar) y se
 * aplican acá las mismas reglas del AC (tipo, 10 MB) con la forma de error { error: { message } }
 * que lee extractApiError. Al llegar el backend, `upload` pasa a ser un POST multipart,
 * `getByPerson` un GET y `delete` un DELETE; `url` pasa a ser la dirección de descarga.
 */
@Injectable({ providedIn: 'root' })
export class PersonAttachmentsService {
  private readonly attachments: PersonAttachment[] = [];
  private nextId = 1;

  /** De más reciente a más antiguo. */
  getByPerson(personId: string): Observable<PersonAttachment[]> {
    const own = this.attachments
      .filter((a) => a.personId === personId)
      .sort((a, b) => b.uploadedAt.localeCompare(a.uploadedAt) || Number(b.id.split('-')[1]) - Number(a.id.split('-')[1]));
    return of(own.map((a) => ({ ...a }))).pipe(delay(100));
  }

  upload(personId: string, file: File, actor: AttachmentActor): Observable<PersonAttachment> {
    const error = validateAttachment(file);
    if (error) {
      return throwError(() => ({ error: { message: error } }));
    }
    const kind = attachmentKind(file)!;
    const attachment: PersonAttachment = {
      id: `att-${this.nextId++}`,
      personId,
      name: file.name,
      kind,
      mimeType: file.type || (kind === 'pdf' ? 'application/pdf' : 'image/jpeg'),
      size: file.size,
      uploadedAt: new Date().toISOString(),
      uploadedByName: actor.name,
      url: URL.createObjectURL(file),
    };
    this.attachments.push(attachment);
    return of({ ...attachment }).pipe(delay(150));
  }

  delete(attachmentId: string): Observable<void> {
    const index = this.attachments.findIndex((a) => a.id === attachmentId);
    if (index === -1) {
      return throwError(() => ({ error: { message: 'El archivo no existe.' } }));
    }
    URL.revokeObjectURL(this.attachments[index].url);
    this.attachments.splice(index, 1);
    return of(undefined).pipe(delay(100));
  }
}
