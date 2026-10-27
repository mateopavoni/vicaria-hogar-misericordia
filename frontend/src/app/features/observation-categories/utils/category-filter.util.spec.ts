import { ObservationCategory } from '../../social-records/interfaces/observation-category.interface';
import { countByStatus, filterCategories, validateCategory } from './category-filter.util';

const cat = (name: string, isActive: boolean): ObservationCategory => ({ id: name, name, isActive });
const list = [cat('Salud', true), cat('Judicial', false), cat('Consumo', true)];

describe('filterCategories', () => {
  it('ordena por nombre', () => {
    expect(filterCategories(list, 'all').map((c) => c.name)).toEqual(['Consumo', 'Judicial', 'Salud']);
  });

  it('separa activas de inactivas', () => {
    expect(filterCategories(list, 'active').map((c) => c.name)).toEqual(['Consumo', 'Salud']);
    expect(filterCategories(list, 'inactive').map((c) => c.name)).toEqual(['Judicial']);
  });

  it('cuenta por estado', () => {
    expect(countByStatus(list)).toEqual({ all: 3, active: 2, inactive: 1 });
  });
});

describe('validateCategory', () => {
  it('el nombre es obligatorio y se recorta', () => {
    expect(validateCategory('   ', '').name).not.toBeNull();
    expect(validateCategory(' Familia ', '').name).toBeNull();
  });

  it('respeta los máximos del backend', () => {
    expect(validateCategory('a'.repeat(101), '').name).not.toBeNull();
    expect(validateCategory('a'.repeat(100), '').name).toBeNull();
    expect(validateCategory('ok', 'd'.repeat(501)).description).not.toBeNull();
    expect(validateCategory('ok', 'd'.repeat(500)).description).toBeNull();
  });
});
