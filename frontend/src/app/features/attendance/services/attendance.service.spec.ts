import { TestBed } from '@angular/core/testing';
import { todayLocalIso } from '../../../shared/utils/date.util';
import { SaveDailyAttendanceRequest } from '../interfaces/attendance.interface';
import { AttendanceService } from './attendance.service';

describe('AttendanceService', () => {
  let service: AttendanceService;

  const ana = { personId: 'p1', fullName: 'Ana Pérez', dni: null };

  const request = (
    overrides: Partial<SaveDailyAttendanceRequest> = {},
  ): SaveDailyAttendanceRequest => ({
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

  it('guarda la asistencia de hoy y la devuelve al consultarla', () =>
    new Promise<void>((done) => {
      service.save(request()).subscribe(() => {
        service.get('hogar-de-dia', todayLocalIso()).subscribe((saved) => {
          expect(saved?.persons.length).toBe(1);
          expect(saved?.anonymousCount).toBe(2);
          done();
        });
      });
    }));

  it('rechaza a la misma persona repetida en la misma actividad y fecha', () =>
    new Promise<void>((done) => {
      service.save(request({ persons: [ana, ana] })).subscribe({
        error: (err) => {
          expect(err.error.message).toContain('repetidas');
          done();
        },
      });
    }));

  it('exige justificación para modificar un día cerrado', () =>
    new Promise<void>((done) => {
      service.save(request({ date: '2000-01-01' })).subscribe({
        error: (err) => {
          expect(err.error.message).toContain('justificación');
          done();
        },
      });
    }));

  it('acepta modificar un día cerrado con justificación', () =>
    new Promise<void>((done) => {
      service
        .save(request({ date: '2000-01-01', justification: 'Se cargó tarde la lista' }))
        .subscribe((saved) => {
          expect(saved.justification).toBe('Se cargó tarde la lista');
          done();
        });
    }));

  it('no mezcla actividades distintas del mismo día', () =>
    new Promise<void>((done) => {
      service.save(request()).subscribe(() => {
        service.get('casa-convivencia', todayLocalIso()).subscribe((saved) => {
          expect(saved).toBeNull();
          done();
        });
      });
    }));

  it('arma el historial de una persona con todas sus actividades, de más reciente a más antigua', () =>
    new Promise<void>((done) => {
      service.save(request({ date: '2000-01-01', justification: 'carga tardía' })).subscribe(() => {
        service.save(request({ activity: 'casa-convivencia' })).subscribe(() => {
          service.getPersonHistory('p1').subscribe((history) => {
            expect(history.map((h) => h.activity)).toEqual(['casa-convivencia', 'hogar-de-dia']);
            expect(history[0].date >= history[1].date).toBe(true);
            done();
          });
        });
      });
    }));
});
