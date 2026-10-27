/**
 * SCRUM-256 (front de SCRUM-24): archivos adjuntos a la ficha de una persona.
 * OJO — backend pendiente: no hay endpoints de adjuntos (ver PersonAttachmentsService).
 */
export type AttachmentKind = 'image' | 'pdf';

export interface PersonAttachment {
  id: string;
  personId: string;
  name: string;
  kind: AttachmentKind;
  mimeType: string;
  /** Bytes. */
  size: number;
  /** ISO */
  uploadedAt: string;
  uploadedByName: string;
  /** Dirección para ver o descargar el archivo. */
  url: string;
}

export interface AttachmentActor {
  id: string;
  name: string;
}
