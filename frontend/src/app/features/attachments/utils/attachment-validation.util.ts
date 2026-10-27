import { AttachmentKind } from '../interfaces/attachment.interface';

export const MAX_ATTACHMENT_BYTES = 10 * 1024 * 1024; // 10 MB por archivo

export interface FileLike {
  name: string;
  type: string;
  size: number;
}

/** Tipo de archivo permitido (imágenes JPG/PNG y PDF), según tipo MIME o, si falta, la extensión. */
export function attachmentKind(file: Pick<FileLike, 'name' | 'type'>): AttachmentKind | null {
  const type = file.type.toLowerCase();
  const ext = file.name.toLowerCase().split('.').pop() ?? '';
  if (type === 'image/jpeg' || type === 'image/png') return 'image';
  if (type === 'application/pdf') return 'pdf';
  if (!type) {
    if (ext === 'jpg' || ext === 'jpeg' || ext === 'png') return 'image';
    if (ext === 'pdf') return 'pdf';
  }
  return null;
}

/** "850 KB", "2,4 MB" */
export function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${Math.round(bytes / 1024)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1).replace('.', ',')} MB`;
}

/** Devuelve el mensaje de error del archivo, o null si se puede adjuntar. */
export function validateAttachment(file: FileLike): string | null {
  if (!attachmentKind(file)) {
    return `«${file.name}» no se puede adjuntar: solo se aceptan imágenes JPG o PNG y documentos PDF.`;
  }
  if (file.size <= 0) {
    return `«${file.name}» está vacío.`;
  }
  if (file.size > MAX_ATTACHMENT_BYTES) {
    return `«${file.name}» supera el tamaño máximo de 10 MB (pesa ${formatFileSize(file.size)}).`;
  }
  return null;
}
