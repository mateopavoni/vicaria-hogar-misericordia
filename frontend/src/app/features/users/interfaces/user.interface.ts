import { UserRole } from "./../../../core/auth/userRole";

export interface ManagedUser {
  id: string;
  name: string;
  lastname: string;
  email: string;
  requestDate: string;
  status: UserStatus;
  role: UserRole | null;
}

export type UserStatus =
  | 'Pending'
  | 'Approved'
  | 'Suspended'
  | 'Blocked';



export interface ApproveUserRequest {
  role: UserRole;
}


export interface RejectUserRequest {
  reason: string;
}