import { MAX_ATTACHMENT_BYTES, attachmentKind, formatFileSize, validateAttachment } from './attachment-validation.util';

const file = (name: string, type: string, size = 1000) => ({ name, type, size });

describe('attachment-validation.util', () => {
  it('acepta JPG, PNG y PDF', () => {
    expect(validateAttachment(file('a.jpg', 'image/jpeg'))).toBe(null);
    expect(validateAttachment(file('a.png', 'image/png'))).toBe(null);
    expect(validateAttachment(file('a.pdf', 'application/pdf'))).toBe(null);
  });

  it('rechaza otros tipos con un mensaje claro', () => {
    const msg = validateAttachment(file('a.docx', 'application/msword'));
    expect(msg!.includes('solo se aceptan')).toBe(true);
    expect(validateAttachment(file('a.gif', 'image/gif'))!.includes('a.gif')).toBe(true);
  });

  it('acepta exactamente 10 MB y rechaza más', () => {
    expect(validateAttachment(file('a.pdf', 'application/pdf', MAX_ATTACHMENT_BYTES))).toBe(null);
    const msg = validateAttachment(file('grande.pdf', 'application/pdf', MAX_ATTACHMENT_BYTES + 1));
    expect(msg!.includes('10 MB')).toBe(true);
    expect(msg!.includes('grande.pdf')).toBe(true);
  });

  it('rechaza archivos vacíos', () => {
    expect(validateAttachment(file('a.pdf', 'application/pdf', 0))!.includes('vacío')).toBe(true);
  });

  it('sin tipo MIME usa la extensión', () => {
    expect(attachmentKind(file('Foto.JPG', ''))).toBe('image');
    expect(attachmentKind(file('x.exe', ''))).toBe(null);
  });

  it('formatea tamaños', () => {
    expect(formatFileSize(500)).toBe('500 B');
    expect(formatFileSize(2048)).toBe('2 KB');
    expect(formatFileSize(2.5 * 1024 * 1024)).toBe('2,5 MB');
  });
});
