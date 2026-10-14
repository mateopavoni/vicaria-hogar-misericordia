import { firstValueFrom } from 'rxjs';
import { errorOf } from '../../../shared/testing/error-of';
import { BackupsService } from './backups.service';

describe('BackupsService', () => {
  it('lista los últimos 30 respaldos del más nuevo al más viejo', async () => {
    const list = await firstValueFrom(new BackupsService().list());
    expect(list.length).toBe(30);
    expect(list[0].createdAt >= list[29].createdAt).toBe(true);
  });

  it('descarga un respaldo correcto', async () => {
    const service = new BackupsService();
    const list = await firstValueFrom(service.list());
    const ok = list.find((r) => r.status === 'Completed')!;
    const file = await firstValueFrom(service.download(ok.id));
    expect(file.filename.length > 0).toBe(true);
  });

  it('no deja descargar un respaldo fallido', async () => {
    const service = new BackupsService();
    const list = await firstValueFrom(service.list());
    const failed = list.find((r) => r.status === 'Failed')!;
    const err = await errorOf(service.download(failed.id));
    expect(err.error.message.includes('falló')).toBe(true);
  });
});
