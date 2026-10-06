export interface Observation {
  id: string;
  socialRecordId: string;    // ID de la ficha social a la que pertenece
  categoryId: string;        // ID de la categoría (Salud, Documentación, etc.)
  categoryName: string;      // Nombre de la categoría (para mostrar directamente en la tarjeta)
  content: string;           // Texto libre de la observación
  createdBy: string;         // Nombre del usuario (Referente / Escucha) que la registró
  createdAt: string;         // Fecha de creación (ISO string)
  updatedBy?: string | null; // Nombre del último usuario que la editó (opcional)
  updatedAt?: string | null; // Fecha de última modificación (opcional)
}

export interface CreateObservationDto {
  socialRecordId: string;
  categoryId: string | null;
  content: string;
}

export interface UpdateObservationDto {
  categoryId?: string;
  content?: string;
}