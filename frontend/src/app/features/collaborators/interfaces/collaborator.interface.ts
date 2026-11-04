// Colaboradores del hogar (voluntarios y empleados).

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

export type UpdateCollaboratorDto = CreateCollaboratorDto & { isActive?: boolean };
