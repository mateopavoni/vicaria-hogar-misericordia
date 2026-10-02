// SCRUM-18/SCRUM-29: colaboradores del hogar (voluntarios y empleados).
// NOTA: el backend de esta feature todavía no existe (se decidió por ahora trabajar
// solo en el frontend). Este es el contrato asumido, siguiendo las mismas convenciones
// que ObservationCategory (GET con onlyActive, PATCH /status para baja/reactivación,
// DTOs separados de create/update) — cuando el backend esté, ajustar acá si difiere.

export enum CollaboratorType {
  Volunteer = 'Volunteer',
  Employee = 'Employee',
}

export interface Collaborator {
  id: string;
  firstName: string;
  lastName?: string | null;
  dni?: string | null;
  phone?: string | null;
  email?: string | null;
  type?: CollaboratorType | null;
  workArea?: string | null;
  isActive: boolean;
  createdAt: string;
  createdByName: string;
}

export interface CreateCollaboratorDto {
  firstName: string;
  lastName?: string | null;
  dni?: string | null;
  phone?: string | null;
  email?: string | null;
  type?: CollaboratorType | null;
  workArea?: string | null;
}

export type UpdateCollaboratorDto = CreateCollaboratorDto;
