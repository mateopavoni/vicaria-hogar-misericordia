import { ObservationCategory } from '../../social-records/interfaces/observation-category.interface';

export type CategoryFilter = 'all' | 'active' | 'inactive';

export const NAME_MAX = 100;
export const DESCRIPTION_MAX = 500;

export function filterCategories(categories: ObservationCategory[], filter: CategoryFilter): ObservationCategory[] {
  const sorted = [...categories].sort((a, b) => a.name.localeCompare(b.name, 'es'));
  if (filter === 'active') return sorted.filter((c) => c.isActive);
  if (filter === 'inactive') return sorted.filter((c) => !c.isActive);
  return sorted;
}

export function countByStatus(categories: ObservationCategory[]): { all: number; active: number; inactive: number } {
  const active = categories.filter((c) => c.isActive).length;
  return { all: categories.length, active, inactive: categories.length - active };
}

/** Mensaje de error del nombre/descripción, o null si es válido (mismas reglas que el backend). */
export function validateCategory(name: string, description: string): { name: string | null; description: string | null } {
  const trimmed = name.trim();
  return {
    name: !trimmed ? 'El nombre de la categoría es obligatorio.' : trimmed.length > NAME_MAX ? `El nombre no puede superar los ${NAME_MAX} caracteres.` : null,
    description: description.trim().length > DESCRIPTION_MAX ? `La descripción no puede superar los ${DESCRIPTION_MAX} caracteres.` : null,
  };
}
