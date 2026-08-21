// TODO(SCRUM-83): endpoint no existe todavía, contrato asumido en base al JWT.

export interface LoginRequest {
  email: string;
  password: string;
}

// nombre/apellido/rol: claims del JWT, no traducir.
export interface User {
  id: string;
  nombre: string;
  apellido: string;
  email: string;
  rol: string | null;
}

export interface LoginResponse {
  token: string;
  user: User;
}

// qué pantalla mostrar
export type LoginErrorType = 'credentials' | 'blocked' | 'pending' | 'unknown';

// body asumido, no confirmado
export interface LoginErrorBody {
  estado?: 'Bloqueada' | 'Pending';
  message?: string;
}
