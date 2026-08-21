// TODO(SCRUM-83): POST /api/auth/login todavía no existe en el backend.
// Contrato asumido en base a los claims que ya usa el JWT (id, nombre, email, rol).
// Confirmar cuando se implemente el endpoint real.

export interface LoginRequest {
  email: string;
  password: string;
}

export interface Usuario {
  id: string;
  nombre: string;
  apellido: string;
  email: string;
  rol: string | null;
}

export interface LoginResponse {
  token: string;
  usuario: Usuario;
}

// Estado que le indica al componente qué pantalla mostrar.
export type LoginErrorTipo = 'credenciales' | 'bloqueada' | 'pendiente' | 'desconocido';

// Body asumido para los errores 401/403 (tampoco confirmado todavía).
export interface LoginErrorBody {
  estado?: 'Bloqueada' | 'Pending';
  message?: string;
}
