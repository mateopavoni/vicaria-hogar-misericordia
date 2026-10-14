import { ReportFieldCatalogService } from './report-field-catalog.service';

describe('ReportFieldCatalogService', () => {
  it('trae los valores iniciales de cada campo', () => {
    const service = new ReportFieldCatalogService();
    expect(service.values('gender').map((v) => v.label)).toEqual(['Masculino', 'Femenino', 'Otro']);
    expect(service.values('housing').length).toBe(3);
    expect(service.values('document').map((v) => v.label)).toEqual(['Con DNI', 'Sin DNI', 'En trámite']);
    expect(service.values('mainReason').length).toBe(5);
  });

  it('agrega un valor nuevo y rechaza duplicados y textos inválidos', () => {
    const service = new ReportFieldCatalogService();
    const added = service.add('mainReason', '  Situación   de salud ');
    expect(added.ok).toBe(true);
    expect(service.label('mainReason', 'situacion-de-salud')).toBe('Situación de salud');

    const dup = service.add('mainReason', 'violencia');
    expect(dup.ok).toBe(false);
    expect(service.add('mainReason', 'a').ok).toBe(false);
    expect(service.add('mainReason', 'x'.repeat(61)).ok).toBe(false);
    // el mismo texto en otro campo sí se puede
    expect(service.add('origin', 'Violencia').ok).toBe(true);
  });

  it('edita un valor sin cambiar su id', () => {
    const service = new ReportFieldCatalogService();
    const result = service.rename('document', 'en-tramite', 'DNI en trámite');
    expect(result.ok).toBe(true);
    expect(service.label('document', 'en-tramite')).toBe('DNI en trámite');
    expect(service.rename('document', 'con-dni', 'sin dni').ok).toBe(false);
    expect(service.rename('document', 'nope', 'Algo').ok).toBe(false);
  });

  it('desactivar saca el valor de las opciones pero conserva su texto', () => {
    const service = new ReportFieldCatalogService();
    service.setActive('gender', 'otro', false);
    expect(service.active('gender').map((v) => v.id)).toEqual(['masculino', 'femenino']);
    expect(service.label('gender', 'otro')).toBe('Otro');
    service.setActive('gender', 'otro', true);
    expect(service.active('gender').length).toBe(3);
  });

  it('no deja desactivar el último valor activo de un campo', () => {
    const service = new ReportFieldCatalogService();
    service.setActive('gender', 'masculino', false);
    service.setActive('gender', 'femenino', false);
    expect(service.setActive('gender', 'otro', false).ok).toBe(false);
    expect(service.active('gender').length).toBe(1);
  });
});
