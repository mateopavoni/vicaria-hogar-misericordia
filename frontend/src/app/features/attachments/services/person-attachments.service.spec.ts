import { firstValueFrom } from 'rxjs';
import { errorOf } from '../../../shared/testing/error-of';
import { MAX_ATTACHMENT_BYTES } from '../utils/attachment-validation.util';
import { PersonAttachmentsService } from './person-attachments.service';

describe('PersonAttachmentsService', () => {
  let service: PersonAttachmentsService;
  const actor = { id: 'u1', name: 'Belén Test' };
  const file = (name: string, type: string, size = 1000) => ({ name, type, size }) as File;

  beforeEach(() => {
    // jsdom no trae createObjectURL/revokeObjectURL
    (URL as unknown as Record<string, unknown>)['createObjectURL'] = () => 'blob:test';
    (URL as unknown as Record<string, unknown>)['revokeObjectURL'] = () => undefined;
    service = new PersonAttachmentsService();
  });

  it('adjunta varios archivos a la misma ficha, con usuario, fecha y tipo', async () => {
    await firstValueFrom(service.upload('p1', file('recibo.pdf', 'application/pdf'), actor));
    await firstValueFrom(service.upload('p1', file('dni.png', 'image/png'), actor));
    await firstValueFrom(service.upload('otra', file('x.jpg', 'image/jpeg'), actor));

    const list = await firstValueFrom(service.getByPerson('p1'));
    expect(list.length).toBe(2);
    expect(list.map((a) => a.kind).sort()).toEqual(['image', 'pdf']);
    expect(list[0].uploadedByName).toBe('Belén Test');
    expect(list[0].uploadedAt).toBeTruthy();
  });

  it('rechaza archivos de más de 10 MB con un mensaje claro', async () => {
    const err = await errorOf(service.upload('p1', file('grande.pdf', 'application/pdf', MAX_ATTACHMENT_BYTES + 1), actor));
    expect(err.error.message).toContain('10 MB');
  });

  it('rechaza tipos que no son JPG, PNG o PDF', async () => {
    const err = await errorOf(service.upload('p1', file('a.docx', 'application/msword'), actor));
    expect(err.error.message).toContain('solo se aceptan');
  });

  it('elimina un archivo', async () => {
    const att = await firstValueFrom(service.upload('p1', file('a.pdf', 'application/pdf'), actor));
    await firstValueFrom(service.delete(att.id));
    expect((await firstValueFrom(service.getByPerson('p1'))).length).toBe(0);
    const err = await errorOf(service.delete(att.id));
    expect(err.error.message).toContain('no existe');
  });
});
