export interface ManagedUser {
  id: string;
  name: string;
  lastname: string;
  email: string;
  requestType: string;
  requestDate: string;
  status: UserStatus;
  role: UserRole | null;
}

export type UserStatus =
  | 'Pending'
  | 'Approved'
  | 'Suspended';

export type UserRole =
  | 'Referente'
  | 'Directora de Casona'
  | 'Escucha';


export interface ApproveUserRequest {
  role: UserRole;
}


export interface RejectUserRequest {
  reason: string;
}