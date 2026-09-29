// bug reportado 2026-09-23: faltaba CoordinadorDeCasaConvivencia (RoleNames.cs tiene 4
// roles, el frontend solo conocía 3) — un usuario con ese rol no se podía gestionar
// desde la UI de aprobación/asignación de roles.
export type UserRole =
  | 'Referente'
  | 'DirectoraDeCasona'
  | 'Escucha'
  | 'CoordinadorDeCasaConvivencia';
