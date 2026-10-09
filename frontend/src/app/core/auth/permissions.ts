import { UserRole } from './../../core/auth/userRole';

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
  // SCRUM-74: el backend (CasonaVisitsController) deja leer a los 4 roles y escribir solo a
  // Referente, Directora y Coordinador; Escucha nunca tiene visitas.*
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

    // Visitas de la Casa de Convivencia (SCRUM-74)
    'visitas.view',
    'visitas.create',
    'visitas.edit'
  ],


  Escucha: [

    'fichas.view',

    'observaciones.view',
    'observaciones.create',

    // calendario de solo lectura (el backend le permite GET, no POST)
    'calendario.view'
  ],


  'DirectoraDeCasaConvivencia': [

     // Fichas Casa de Convivencia
    'fichas.view',
    'fichas.create',
    'fichas.edit',

    // Observaciones Casa de Convivencia
    'observaciones.view',
    'observaciones.create',
    'observaciones.edit',

    // Calendario Casa de Convivencia
    // crear/editar eventos es solo del Referente en el backend (GeneralCalendarEventsController)
    'calendario.view',

    // Colaboradores Casa de Convivencia
    // el alta de colaboradores es solo del Referente en el backend
    'colaboradores.view',

    // Stock Casa de Convivencia
    'stock.view',
    'stock.create',
    'stock.edit',

    // la gestión de usuarios es solo del Referente (el backend autoriza solo ese rol)

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
  // agenda de medicamentos — mucho más acotado que DirectoraDeCasaConvivencia. (Nota: varios
  // controllers además lo autorizan a nivel de [Authorize(Roles=...)] para crear fichas y
  // cambiar tipo/estado de persona, que no tiene equivalente en este mapa de permisos
  // granulares — discrepancia ya documentada en .ai/context/OPEN_QUESTIONS.md, no se
  // resuelve acá.)
  CoordinadorDeCasaConvivencia: [
    'fichas.view',
    'observaciones.view',
    'observaciones.create',
    'medicamentos.view',

    // calendario general de solo lectura y visitas de la casona (SCRUM-74)
    'calendario.view',
    'visitas.view',
    'visitas.create',
    'visitas.edit'
  ]

};