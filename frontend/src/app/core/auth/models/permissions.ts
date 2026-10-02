import { UserRole } from './user-role';

export type Permission =
  | 'fichas.view'
  | 'fichas.create'
  | 'fichas.edit'
  | 'observaciones.view'
  | 'observaciones.create'
  | 'observaciones.edit'
  | 'calendario.view'
  | 'calendario.create'
  | 'calendario.edit'
  | 'colaboradores.view'
  | 'colaboradores.create'
  | 'colaboradores.edit'
  | 'stock.view'
  | 'stock.create'
  | 'stock.edit'
  | 'users.view'
  | 'users.approve'
  | 'users.reject'
  | 'users.change-role'
  | 'users.disable'
  | 'medicamentos.view'
  | 'medicamentos.create'
  | 'medicamentos.edit'
  // gestión de categorías de observaciones (crear/editar/activar/desactivar): el
  // backend restringe estos endpoints solo a Referente (ObservationCategoriesController),
  // así que acá se refleja igual — ningún otro rol tiene este permiso.
  | 'categorias.manage'
  // SCRUM-74 (AC): "solo pueden ver y editar este calendario: referentes, Directora y
  // Coordinador de la casona" — Escucha nunca tiene ninguno de estos tres, a
  // diferencia de calendario.view (que sí tiene, de solo lectura).
  | 'visitas.view'
  | 'visitas.create'
  | 'visitas.edit';


export const ROLE_PERMISSIONS: Record<UserRole, Permission[]> = {

  Referente: [

    // Fichas
    'fichas.view',
    'fichas.create',
    'fichas.edit',

    // Observaciones
    'observaciones.view',
    'observaciones.create',
    'observaciones.edit',

    // Calendario
    'calendario.view',
    'calendario.create',
    'calendario.edit',

    // Colaboradores
    'colaboradores.view',
    'colaboradores.create',
    'colaboradores.edit',

    // Stock
    'stock.view',
    'stock.create',
    'stock.edit',

    // Usuarios
    'users.view',
    'users.approve',
    'users.reject',
    'users.change-role',
    'users.disable',

    // Medicamentos
    'medicamentos.view',
    'medicamentos.create',
    'medicamentos.edit',

    // Configuración: categorías de observaciones (solo Referente, ver AC del SCRUM-181)
    'categorias.manage',

    // Visitas de la Casa de Convivencia (SCRUM-74)
    'visitas.view',
    'visitas.create',
    'visitas.edit'
  ],


  Escucha: [

    'fichas.view',

    'observaciones.view',
    'observaciones.create',

    // SCRUM-15 (AC): "el calendario es de solo lectura para el rol Escucha (puede ver
    // pero no crear ni editar eventos)" — por eso solo .view, nunca .create/.edit.
    'calendario.view'
  ],


  'DirectoraDeCasona': [

     // Fichas Casa de Convivencia
    'fichas.view',
    'fichas.create',
    'fichas.edit',

    // Observaciones Casa de Convivencia
    'observaciones.view',
    'observaciones.create',
    'observaciones.edit',

    // Calendario Casa de Convivencia
    'calendario.view',
    'calendario.create',
    'calendario.edit',

    // Colaboradores Casa de Convivencia
    'colaboradores.view',
    'colaboradores.create',
    'colaboradores.edit',

    // Stock Casa de Convivencia
    'stock.view',
    'stock.create',
    'stock.edit',

    // Usuarios Casa de Convivencia
    'users.view',
    'users.approve',
    'users.reject',
    'users.change-role',
    'users.disable',

    // Medicamentos Casa de Convivencia
    'medicamentos.view',
    'medicamentos.create',
    'medicamentos.edit',

    // Visitas de la Casa de Convivencia (SCRUM-74)
    'visitas.view',
    'visitas.create',
    'visitas.edit'
  ],

  // bug reportado 2026-09-23: faltaba este rol por completo. Los permisos granulares
  // reales que le da la migración AddCoordinadorRolePermissions son solo 3: ver fichas de
  // residentes de la Casa de Convivencia, cargar observaciones sobre residentes y ver la
  // agenda de medicamentos — mucho más acotado que DirectoraDeCasona. (Nota: varios
  // controllers además lo autorizan a nivel de [Authorize(Roles=...)] para crear fichas y
  // cambiar tipo/estado de persona, que no tiene equivalente en este mapa de permisos
  // granulares — discrepancia ya documentada en .ai/context/OPEN_QUESTIONS.md, no se
  // resuelve acá.)
  CoordinadorDeCasaConvivencia: [
    'fichas.view',
    'observaciones.view',
    'observaciones.create',
    'medicamentos.view',

    // Visitas de la Casa de Convivencia (SCRUM-74 AC): el Coordinador está
    // explícitamente incluido acá, a diferencia del resto de sus permisos (más
    // acotados que Referente/DirectoraDeCasona, ver nota arriba).
    'visitas.view',
    'visitas.create',
    'visitas.edit'
  ]

};