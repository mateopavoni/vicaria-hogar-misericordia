export interface ObservationCategory {
  id: string;
  name: string;
  description?: string | null;
  isActive: boolean;
  isPredefined?: boolean; // Categorías base (Salud, Documentación, etc.)
  usageCount?: number;     // Para validar si tiene observaciones asociadas
}

export type CreateCategoryDto = Omit<ObservationCategory, 'id' | 'isActive' | 'usageCount'>;
export type UpdateCategoryDto = Partial<CreateCategoryDto> & { isActive?: boolean };