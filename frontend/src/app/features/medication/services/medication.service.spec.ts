import { MedicationService } from './medication.service';

describe('MedicationService', () => {
  let service: MedicationService;
  const actor = { id: 'u1', name: 'Belén Test' };
  const request = (overrides = {}) => ({ personId: 'p1', medicationId: 'med-1', dose: '500 mg', time: '08:00', ...overrides });

  beforeEach(() => {
    service = new MedicationService();
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
});
