import { PersonStatus } from '../interfaces/social-record.interface';
import { StatusChangeLogService, mergeStatusHistory } from './status-change-log.service';

describe('StatusChangeLogService', () => {
  it('registra fecha, usuario y motivo (sin espacios sobrantes)', () => {
    const service = new StatusChangeLogService();
    const item = service.record('p1', PersonStatus.Active, PersonStatus.Inactive, 'Ana Ruiz', '  Internación  ', new Date('2026-10-01T10:00:00Z'));
    expect(item.previousValue).toBe('Activo');
    expect(item.newValue).toBe('Inactivo');
    expect(item.reason).toBe('Internación');
    expect(item.modifiedBy).toBe('Ana Ruiz');
    expect(service.forPerson('p1').length).toBe(1);
    expect(service.forPerson('p2').length).toBe(0);
  });

  it('mergeStatusHistory ordena del más nuevo al más viejo y no duplica', () => {
    const a = { previousValue: 'Activo', newValue: 'Inactivo', modifiedBy: 'Sistema', modifiedAt: '2026-09-01T00:00:00Z' };
    const b = { previousValue: 'Inactivo', newValue: 'Activo', modifiedBy: 'Ana', modifiedAt: '2026-10-01T00:00:00Z' };
    const merged = mergeStatusHistory([a, b], [b]);
    expect(merged.length).toBe(2);
    expect(merged[0].newValue).toBe('Activo');
    expect(mergeStatusHistory(undefined, []).length).toBe(0);
  });
});
