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
  | 'medicamentos.edit';


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
    'medicamentos.edit'
  ],


  Escucha: [

    'fichas.view',

    'observaciones.view',
    'observaciones.create'
  ],


  'DirectoradeCasona': [

     // Fichas Casona
    'fichas.view',
    'fichas.create',
    'fichas.edit',

    // Observaciones Casona
    'observaciones.view',
    'observaciones.create',
    'observaciones.edit',

    // Calendario Casona
    'calendario.view',
    'calendario.create',
    'calendario.edit',

    // Colaboradores Casona
    'colaboradores.view',
    'colaboradores.create',
    'colaboradores.edit',

    // Stock Casona
    'stock.view',
    'stock.create',
    'stock.edit',

    // Usuarios Casona
    'users.view',
    'users.approve',
    'users.reject',
    'users.change-role',
    'users.disable',

    // Medicamentos Casona
    'medicamentos.view',
    'medicamentos.create',
    'medicamentos.edit'
  ]

};