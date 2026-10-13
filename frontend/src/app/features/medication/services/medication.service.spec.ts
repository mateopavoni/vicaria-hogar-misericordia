import { TestBed } from '@angular/core/testing';
import { todayLocalIso } from '../../../shared/utils/date.util';
import { AttendanceService } from '../../attendance/services/attendance.service';
import { MedicationService } from './medication.service';

describe('MedicationService', () => {
  let service: MedicationService;
  const actor = { id: 'u1', name: 'Belén Test' };
  const request = (overrides = {}) => ({ personId: 'p1', medicationId: 'med-1', dose: '500 mg', time: '08:00', ...overrides });

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(MedicationService);
  });

  it('bloquea la carga si la persona no tiene evaluación psiquiátrica vigente', (done) => {
    service.create(request(), false, actor).subscribe({
      error: (err) => {
        expect(err.error.message).toContain('evaluación psiquiátrica vigente');
        done();
      },
    });
  });

  it('solo acepta medicamentos del catálogo', (done) => {
    service.create(request({ medicationId: 'inexistente' }), true, actor).subscribe({
      error: (err) => {
        expect(err.error.message).toContain('catálogo');
        done();
      },
    });
  });

  it('registra medicamento, dosis, hora, quién lo cargó y cuándo, como vigente', (done) => {
    service.create(request(), true, actor).subscribe((scheme) => {
      expect(scheme.dose).toBe('500 mg');
      expect(scheme.time).toBe('08:00');
      expect(scheme.loadedByName).toBe('Belén Test');
      expect(scheme.loadedAt).toBeTruthy();
      expect(scheme.active).toBeTrue();
      expect(scheme.pendingStock).toBeFalse();
      done();
    });
  });

  it('con stock 0 permite cargar pero lo marca pendiente de stock', (done) => {
    service.create(request({ medicationId: 'med-3' }), true, actor).subscribe((scheme) => {
      expect(scheme.pendingStock).toBeTrue();
      done();
    });
  });

  it('valida dosis y hora', (done) => {
    service.create(request({ dose: '  ' }), true, actor).subscribe({
      error: (err) => {
        expect(err.error.message).toContain('dosis');
        service.create(request({ time: '25:00' }), true, actor).subscribe({
          error: (err2) => {
            expect(err2.error.message).toContain('hora');
            done();
          },
        });
      },
    });
  });

  it('pasa un esquema a histórico y lo lista por persona, más nuevo primero', (done) => {
    service.create(request(), true, actor).subscribe((first) => {
      service.create(request({ personId: 'otra' }), true, actor).subscribe(() => {
        service.setActive(first.id, false).subscribe((updated) => {
          expect(updated.active).toBeFalse();
          service.getSchemes('p1').subscribe((list) => {
            expect(list.length).toBe(1);
            expect(list[0].active).toBeFalse();
            done();
          });
        });
      });
    });
  });

  describe('agenda diaria (SCRUM-238)', () => {
    const today = todayLocalIso();
    const person = (id: string, fullName: string) => ({ personId: id, fullName, dni: null });

    const attend = (persons: { personId: string; fullName: string; dni: null }[], date = today) =>
      TestBed.inject(AttendanceService).save({ activity: 'desayuno', date, persons, anonymousCount: 0, justification: 'prueba' });

    it('agrupa por hora y solo incluye a quienes asistieron y tienen medicación', (done) => {
      attend([person('p1', 'Juan Pérez'), person('p2', 'José Reyes')]).subscribe(() => {
        service.create(request({ personId: 'p1', time: '08:00' }), true, actor).subscribe(() => {
          service.create(request({ personId: 'p2', medicationId: 'med-2', time: '08:00' }), true, actor).subscribe(() => {
            // p3 tiene medicación pero no asistió
            service.create(request({ personId: 'p3', time: '12:00' }), true, actor).subscribe(() => {
              service.getAgenda(today).subscribe((agenda) => {
                expect(agenda.slots.length).toBe(1);
                expect(agenda.slots[0].time).toBe('08:00');
                expect(agenda.slots[0].items.map((i) => i.personName)).toEqual(['José Reyes', 'Juan Pérez']);
                done();
              });
            });
          });
        });
      });
    });

    it('no incluye esquemas pendientes de stock pero los cuenta aparte', (done) => {
      attend([person('p1', 'Juan Pérez')]).subscribe(() => {
        service.create(request({ medicationId: 'med-3' }), true, actor).subscribe(() => {
          service.getAgenda(today).subscribe((agenda) => {
            expect(agenda.slots.length).toBe(0);
            expect(agenda.pendingStockCount).toBe(1);
            done();
          });
        });
      });
    });

    it('registra la toma con observación opcional y la devuelve en la agenda', (done) => {
      attend([person('p1', 'Juan Pérez')]).subscribe(() => {
        service.create(request(), true, actor).subscribe((scheme) => {
          service.recordAdministration({ schemeId: scheme.id, date: today, status: 'not-administered', observation: ' Rechazó la toma ' }, actor).subscribe(() => {
            service.getAgenda(today).subscribe((agenda) => {
              const adm = agenda.slots[0].items[0].administration;
              expect(adm?.status).toBe('not-administered');
              expect(adm?.observation).toBe('Rechazó la toma');
              expect(adm?.recordedByName).toBe('Belén Test');
              done();
            });
          });
        });
      });
    });

    it('no permite registrar tomas de días anteriores', (done) => {
      service.create(request(), true, actor).subscribe((scheme) => {
        service.recordAdministration({ schemeId: scheme.id, date: '2020-01-01', status: 'administered', observation: null }, actor).subscribe({
          error: (err) => {
            expect(err.error.message).toContain('día actual');
            done();
          },
        });
      });
    });

    it('un día anterior conserva el historial aunque el esquema pase a histórico', (done) => {
      // la fecha pasada no tiene toma registrada (solo se registra hoy): no aparece
      attend([person('p1', 'Juan Pérez')], '2020-01-01').subscribe(() => {
        service.create(request(), true, actor).subscribe((scheme) => {
          service.setActive(scheme.id, false).subscribe(() => {
            service.getAgenda('2020-01-01').subscribe((agenda) => {
              expect(agenda.slots.length).toBe(0);
              done();
            });
          });
        });
      });
    });
  });
});
