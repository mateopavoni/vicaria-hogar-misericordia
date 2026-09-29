// forma real que devuelve el backend (ObservationCategoryDto: Id, Name, Description, IsActive, CreatedAt).
// bug reportado 2026-09-29: esta interfaz traía isPredefined/usageCount, campos que el
// backend nunca devuelve (no existen en ObservationCategory.cs ni en el DTO) — quedaban
// siempre undefined. Se sacan y se agrega createdAt, que sí llega.
export interface ObservationCategory {
  id: string;
  name: string;
  description?: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface CreateCategoryDto {
  name: string;
  description?: string | null;
}

export type UpdateCategoryDto = CreateCategoryDto;
