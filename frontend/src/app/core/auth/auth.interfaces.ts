import { UserRole } from "../../core/auth/userRole";

export interface LoginRequest {
  email: string;
  password: string;
}
export interface RegisterRequest {
  name: string;
  lastname: string;
  email: string;
  password: string;
}
// nombre/apellido/rol: claims del JWT, no traducir.
export interface User {
  id: string;
  name: string;
  lastname: string;
  email: string;
  role: UserRole | null;
}

export interface LoginResponse {
  token: string;
  refreshToken: string;
  user: User;
}

// forma cruda que devuelve el backend en /api/auth/login (campos en inglés, así los serializa el controller)
export interface BackendUser {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  role: UserRole | null;
}

export type LoginErrorType = 'credentials' | 'blocked' | 'pending' | 'unknown';


export interface LoginErrorBody {
  estado?: 'Bloqueada' | 'Pending' | 'Inactive';
  message?: string;
}

