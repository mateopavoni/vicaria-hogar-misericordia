import { UserRole } from './../../core/auth/userRole';

export type Permission =
  | 'fichas.view'
  | 'fichas.create'
  | 'fichas.edit'
  // Exportar / imprimir la ficha completa, solo Referente
  | 'fichas.export'
  // Evaluación psiquiátrica (ver y cargar), Referente y Directora
  | 'evaluaciones.view'
  | 'evaluaciones.create'
  // Documentos adjuntos de la ficha; eliminar es solo del Referente
  | 'adjuntos.view'
  | 'adjuntos.create'
  | 'adjuntos.delete'
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
  // Historial de tomas de una persona, solo Referente y Directora (no Coordinador ni Escucha)
  | 'medicamentos.history'
  // Inventario de medicamentos (ingresos, umbral, movimientos), solo Referente y Directora; separado del stock de ropa
  | 'medicamentos.stock'
  // El backend (CasonaVisitsController) deja leer a los 4 roles y escribir solo a
  // Referente, Directora y Coordinador; Escucha nunca tiene visitas.*
  | 'visitas.view'
  | 'visitas.create'
  | 'visitas.edit'
  // Asistencia diaria. 'asistencia.edit' = modificar un día ya cerrado (solo Referente, con justificación)
  | 'asistencia.view'
  | 'asistencia.create'
  | 'asistencia.edit'
  // Estadísticas de asistencia del Centro Barrial, solo Referente
  | 'asistencia.stats'
  // Configurar las actividades de asistencia (activar, desactivar, crear), solo Referente
  | 'asistencia.config'
  // Panel de respaldos de la base de datos (administrador técnico = Referente, no hay rol aparte)
  | 'configuracion.respaldos'
  // Informe estadístico para Cáritas, solo Referente
  | 'informes.caritas'
  // Configurar los valores de los campos del informe de Cáritas, solo Referente
  | 'informes.config'
  // Gestionar categorías de observaciones, solo Referente (Escucha no accede)
  | 'categorias.manage'
  // historia de vida: ver (Referente y Directora; Escucha no) y editar (solo Referente)
  | 'historia.view'
  | 'historia.edit'
  // estadías de la Casa de Convivencia: marcar ingreso (Referente y Directora) y registrar egreso (solo Referente)
  | 'estadias.entry'
  | 'estadias.exit';


export const ROLE_PERMISSIONS: Record<UserRole, Permission[]> = {

  Referente: [

    // Fichas
    'fichas.view',
    'fichas.create',
    'fichas.edit',
    'fichas.export',
    'adjuntos.view',
    'adjuntos.create',
    'evaluaciones.view',
    'evaluaciones.create',
    'adjuntos.delete',

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
    'medicamentos.history',
    'medicamentos.stock',

    // Visitas de la Casa de Convivencia
    'visitas.view',
    'visitas.create',
    'visitas.edit',

    // Asistencia diaria
    'asistencia.view',
    'asistencia.create',
    'asistencia.edit',
    'asistencia.stats',
    'asistencia.config',
    'configuracion.respaldos',
    'informes.caritas',
    'informes.config',
    'categorias.manage',
    'historia.view',
    'historia.edit',
    'estadias.entry',
    'estadias.exit'
  ],


  Escucha: [

    'fichas.view',

    'observaciones.view',
    'observaciones.create',

    // calendario de solo lectura (el backend le permite GET, no POST)
    'calendario.view',

    // el backend deja a Escucha cargar la asistencia del día a día (POST api/attendance)
    'asistencia.view',
    'asistencia.create'
  ],


  'DirectoraDeCasaConvivencia': [

     // Fichas Casa de Convivencia
    'fichas.view',
    'historia.view',
    'estadias.entry',
    'fichas.create',
    'fichas.edit',
    'adjuntos.view',
    'adjuntos.create',
    'evaluaciones.view',
    'evaluaciones.create',

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
    'medicamentos.history',
    'medicamentos.stock',

    // Visitas de la Casa de Convivencia
    'visitas.view',
    'visitas.create',
    'visitas.edit',

    // Asistencia diaria; modificar días cerrados es solo del Referente
    'asistencia.view',
    'asistencia.create'
  ],

  // ve fichas de residentes, carga observaciones y ve la agenda de medicamentos
  CoordinadorDeCasaConvivencia: [
    'fichas.view',
    'observaciones.view',
    'observaciones.create',
    'medicamentos.view',

    // calendario general de solo lectura y visitas de la casona
    'calendario.view',
    'visitas.view',
    'visitas.create',
    'visitas.edit',

    'asistencia.view',
    'asistencia.create'
  ]

};