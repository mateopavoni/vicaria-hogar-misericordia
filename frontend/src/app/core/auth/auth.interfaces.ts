
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
  role: string | null;
}

export interface LoginResponse {
  token: string;
  user: User;
}

export type LoginErrorType = 'credentials' | 'blocked' | 'pending' | 'unknown';


export interface LoginErrorBody {
  estado?: 'Blocked' | 'Pending';
  message?: string;
}

