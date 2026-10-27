export interface ObservationCategory {
  id: string;
  name: string;
  description?: string | null;
  isActive: boolean;
  createdAt?: string;
  isPredefined?: boolean; // Categorías base (Salud, Documentación, etc.)
  usageCount?: number;     // Para validar si tiene observaciones asociadas
}

// Lo que acepta el backend (CreateObservationCategoryDto / UpdateObservationCategoryDto)
export interface CreateCategoryDto {
  name: string;
  description?: string | null;
}

export type UpdateCategoryDto = CreateCategoryDto;
