import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { errorOf } from '../../../shared/testing/error-of';
import { todayLocalIso } from '../../../shared/utils/date.util';
import { SaveDailyAttendanceRequest } from '../interfaces/attendance.interface';
import { AttendanceService } from './attendance.service';

describe('AttendanceService', () => {
  let service: AttendanceService;

  const ana = { personId: 'p1', fullName: 'Ana Pérez', dni: null };

  const request = (overrides: Partial<SaveDailyAttendanceRequest> = {}): SaveDailyAttendanceRequest => ({
    activity: 'hogar-de-dia',
    date: todayLocalIso(),
    persons: [ana],
    anonymousCount: 2,
    justification: null,
    ...overrides,
  });

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(AttendanceService);
  });

  it('guarda la asistencia de hoy y la devuelve al consultarla', async () => {
    await firstValueFrom(service.save(request()));
    const saved = await firstValueFrom(service.get('hogar-de-dia', todayLocalIso()));
    expect(saved?.persons.length).toBe(1);
    expect(saved?.anonymousCount).toBe(2);
  });

  it('rechaza a la misma persona repetida en la misma actividad y fecha', async () => {
    const err = await errorOf(service.save(request({ persons: [ana, ana] })));
    expect(err.error.message).toContain('repetidas');
  });

  it('exige justificación para modificar un día cerrado', async () => {
    const err = await errorOf(service.save(request({ date: '2000-01-01' })));
    expect(err.error.message).toContain('justificación');
  });

  it('acepta modificar un día cerrado con justificación', async () => {
    const saved = await firstValueFrom(service.save(request({ date: '2000-01-01', justification: 'Se cargó tarde la lista' })));
    expect(saved.justification).toBe('Se cargó tarde la lista');
  });

  it('no mezcla actividades distintas del mismo día', async () => {
    await firstValueFrom(service.save(request()));
    const saved = await firstValueFrom(service.get('casa-convivencia', todayLocalIso()));
    expect(saved).toBeNull();
  });

  it('arma el historial de una persona con todas sus actividades, de más reciente a más antigua', async () => {
    await firstValueFrom(service.save(request({ date: '2000-01-01', justification: 'carga tardía' })));
    await firstValueFrom(service.save(request({ activity: 'casa-convivencia' })));
    const history = await firstValueFrom(service.getPersonHistory('p1'));
    expect(history.map((h) => h.activity)).toEqual(['casa-convivencia', 'hogar-de-dia']);
    expect(history[0].date >= history[1].date).toBe(true);
  });
});
