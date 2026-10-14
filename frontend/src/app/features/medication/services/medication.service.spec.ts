import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { errorOf } from '../../../shared/testing/error-of';
import { todayLocalIso } from '../../../shared/utils/date.util';
import { AttendanceService } from '../../attendance/services/attendance.service';
import { MedicationStockService } from '../../medication-stock/services/medication-stock.service';
import { MedicationService } from './medication.service';

describe('MedicationService', () => {
  let service: MedicationService;
  const actor = { id: 'u1', name: 'Belén Test' };
  const today = todayLocalIso();
  const request = (overrides = {}) => ({ personId: 'p1', medicationId: 'med-1', dose: '500 mg', time: '08:00', ...overrides });
  const create = (overrides = {}, isResident = true, evaluatedAt: string | null = null) =>
    firstValueFrom(service.create(request(overrides), isResident, actor, evaluatedAt));
  const stock = () => TestBed.inject(MedicationStockService);

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(MedicationService);
  });

  // ───────── SCRUM-234: esquema ─────────

  it('bloquea la carga si la persona no tiene evaluación psiquiátrica vigente', async () => {
    const err = await errorOf(service.create(request(), false, actor));
    expect(err.error.message).toContain('evaluación psiquiátrica vigente');
  });

  it('solo acepta medicamentos del catálogo', async () => {
    const err = await errorOf(service.create(request({ medicationId: 'inexistente' }), true, actor));
    expect(err.error.message).toContain('catálogo');
  });

  it('registra medicamento, dosis, hora, quién lo cargó y cuándo, como vigente', async () => {
    const scheme = await create();
    expect(scheme.dose).toBe('500 mg');
    expect(scheme.time).toBe('08:00');
    expect(scheme.loadedByName).toBe('Belén Test');
    expect(scheme.loadedAt).toBeTruthy();
    expect(scheme.active).toBe(true);
    expect(scheme.pendingStock).toBe(false);
  });

  it('con stock 0 permite cargar pero lo marca pendiente de stock', async () => {
    const scheme = await create({ medicationId: 'med-3' });
    expect(scheme.pendingStock).toBe(true);
  });

  it('valida dosis y hora', async () => {
    const e1 = await errorOf(service.create(request({ dose: '  ' }), true, actor));
    expect(e1.error.message).toContain('dosis');
    const e2 = await errorOf(service.create(request({ time: '25:00' }), true, actor));
    expect(e2.error.message).toContain('hora');
  });

  it('pasa un esquema a histórico y lo lista por persona, más nuevo primero', async () => {
    const first = await create();
    await create({ personId: 'otra' });
    const updated = await firstValueFrom(service.setActive(first.id, false));
    expect(updated.active).toBe(false);
    const list = await firstValueFrom(service.getSchemes('p1'));
    expect(list.length).toBe(1);
    expect(list[0].active).toBe(false);
  });

  // ───────── SCRUM-238: agenda diaria ─────────

  describe('agenda diaria', () => {
    const person = (id: string, fullName: string) => ({ personId: id, fullName, dni: null });
    const attend = (persons: { personId: string; fullName: string; dni: null }[], date = today) =>
      firstValueFrom(
        TestBed.inject(AttendanceService).save({ activity: 'desayuno', date, persons, anonymousCount: 0, justification: 'prueba' }),
      );

    it('agrupa por hora y solo incluye a quienes asistieron y tienen medicación', async () => {
      await attend([person('p1', 'Juan Pérez'), person('p2', 'José Reyes')]);
      await create({ personId: 'p1', time: '08:00' });
      await create({ personId: 'p2', medicationId: 'med-2', time: '08:00' });
      await create({ personId: 'p3', time: '12:00' }); // tiene medicación pero no asistió

      const agenda = await firstValueFrom(service.getAgenda(today));
      expect(agenda.slots.length).toBe(1);
      expect(agenda.slots[0].time).toBe('08:00');
      expect(agenda.slots[0].items.map((i) => i.personName)).toEqual(['José Reyes', 'Juan Pérez']);
    });

    it('no incluye esquemas pendientes de stock pero los cuenta aparte', async () => {
      await attend([person('p1', 'Juan Pérez')]);
      await create({ medicationId: 'med-3' });
      const agenda = await firstValueFrom(service.getAgenda(today));
      expect(agenda.slots.length).toBe(0);
      expect(agenda.pendingStockCount).toBe(1);
    });

    it('registra la toma con observación opcional y la devuelve en la agenda', async () => {
      await attend([person('p1', 'Juan Pérez')]);
      const scheme = await create();
      await firstValueFrom(
        service.recordAdministration({ schemeId: scheme.id, date: today, status: 'not-administered', observation: ' Rechazó la toma ' }, actor),
      );
      const agenda = await firstValueFrom(service.getAgenda(today));
      const adm = agenda.slots[0].items[0].administration;
      expect(adm?.status).toBe('not-administered');
      expect(adm?.observation).toBe('Rechazó la toma');
      expect(adm?.recordedByName).toBe('Belén Test');
    });

    it('no permite registrar tomas de días anteriores', async () => {
      const scheme = await create();
      const err = await errorOf(
        service.recordAdministration({ schemeId: scheme.id, date: '2020-01-01', status: 'administered', observation: null }, actor),
      );
      expect(err.error.message).toContain('día actual');
    });

    it('un día sin tomas registradas no muestra un esquema que hoy es histórico', async () => {
      await attend([person('p1', 'Juan Pérez')], '2020-01-01');
      const scheme = await create();
      await firstValueFrom(service.setActive(scheme.id, false));
      const agenda = await firstValueFrom(service.getAgenda('2020-01-01'));
      expect(agenda.slots.length).toBe(0);
    });
  });

  // ───────── SCRUM-246: stock ─────────

  describe('stock', () => {
    const mark = (schemeId: string, status: 'administered' | 'not-administered') =>
      firstValueFrom(service.recordAdministration({ schemeId, date: today, status, observation: null }, actor));

    it('marcar una toma como administrada descuenta una unidad y corregirla la devuelve', async () => {
      const scheme = await create();
      await mark(scheme.id, 'administered');
      expect(stock().quantityOf('med-1')).toBe(39);
      await mark(scheme.id, 'administered');
      expect(stock().quantityOf('med-1')).toBe(39); // no descuenta dos veces
      await mark(scheme.id, 'not-administered');
      expect(stock().quantityOf('med-1')).toBe(40);
    });

    it('no descuenta si la toma queda como no administrada', async () => {
      const scheme = await create();
      await mark(scheme.id, 'not-administered');
      expect(stock().quantityOf('med-1')).toBe(40);
    });

    it('un ingreso de stock libera los esquemas pendientes de stock', async () => {
      const scheme = await create({ medicationId: 'med-3' });
      expect(scheme.pendingStock).toBe(true);
      await firstValueFrom(
        stock().registerEntry({ medicationId: 'med-3', newName: null, quantity: 5, expirationDate: '2999-01-01', supplier: 'Proveedor' }, actor),
      );
      const schemes = await firstValueFrom(service.getSchemes('p1'));
      expect(schemes[0].pendingStock).toBe(false);
    });
  });

  // ───────── SCRUM-250: revisión mensual y evaluación vencida ─────────

  describe('revisión mensual y evaluación vencida', () => {
    it('un esquema nuevo queda vigente y sin revisar', async () => {
      const scheme = await create();
      expect(scheme.active).toBe(true);
      expect(scheme.reviewedAt).toBeNull();
      expect(scheme.reviewedByName).toBeNull();
    });

    it('marcar como revisado guarda la fecha y el usuario', async () => {
      const scheme = await create();
      const reviewed = await firstValueFrom(service.markReviewed(scheme.id, { id: 'u2', name: 'Otra Persona' }));
      expect(reviewed.reviewedAt).toBeTruthy();
      expect(reviewed.reviewedByName).toBe('Otra Persona');
      expect(reviewed.reviewedByUserId).toBe('u2');
    });

    it('no se puede revisar un esquema desactivado, que se conserva como historial', async () => {
      const scheme = await create();
      const off = await firstValueFrom(service.setActive(scheme.id, false));
      expect(off.active).toBe(false);
      const err = await errorOf(service.markReviewed(scheme.id, actor));
      expect(err.error.message).toContain('vigente');
      const all = await firstValueFrom(service.getSchemes('p1'));
      expect(all.length).toBe(1);
    });

    it('bloquea agregar medicamentos si la evaluación venció (más de 6 meses)', async () => {
      const err = await errorOf(service.create(request(), true, actor, '2020-01-01'));
      expect(err.error.message).toContain('venció');
    });

    it('con una evaluación reciente permite cargar', async () => {
      const scheme = await create({}, true, today);
      expect(scheme.id).toBeTruthy();
    });

    it('el estado de la evaluación informa si está vencida', async () => {
      const status = await firstValueFrom(service.getEvaluationStatus('p1', true, '2020-01-01'));
      expect(status.valid).toBe(false);
      expect(status.expired).toBe(true);
    });
  });
});
