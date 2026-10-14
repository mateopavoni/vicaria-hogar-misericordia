import { UserRole } from './../../core/auth/userRole';

export type Permission =
  | 'fichas.view'
  | 'fichas.create'
  | 'fichas.edit'
  // SCRUM-253: exportar / imprimir la ficha completa, solo Referente
  | 'fichas.export'
  // SCRUM-260: evaluación psiquiátrica (ver y cargar), Referente y Directora
  | 'evaluaciones.view'
  | 'evaluaciones.create'
  // SCRUM-256: documentos adjuntos de la ficha; eliminar es solo del Referente
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
  // SCRUM-242: historial de tomas de una persona, solo Referente y Directora (no Coordinador ni Escucha)
  | 'medicamentos.history'
  // SCRUM-246: inventario de medicamentos (ingresos, umbral, movimientos), solo Referente y Directora; separado del stock de ropa
  | 'medicamentos.stock'
  // SCRUM-74: el backend (CasonaVisitsController) deja leer a los 4 roles y escribir solo a
  // Referente, Directora y Coordinador; Escucha nunca tiene visitas.*
  | 'visitas.view'
  | 'visitas.create'
  | 'visitas.edit'
  // SCRUM-218: asistencia diaria. 'asistencia.edit' = modificar un día ya cerrado (solo Referente, con justificación)
  | 'asistencia.view'
  | 'asistencia.create'
  | 'asistencia.edit'
  // SCRUM-226: estadísticas de asistencia del Centro Barrial, solo Referente
  | 'asistencia.stats'
  // SCRUM-230: configurar las actividades de asistencia (activar, desactivar, crear), solo Referente
  | 'asistencia.config'
  // SCRUM-267: panel de respaldos de la base de datos (administrador técnico = Referente, no hay rol aparte)
  | 'configuracion.respaldos'
  // SCRUM-271: informe estadístico para Cáritas, solo Referente
  | 'informes.caritas'
  // SCRUM-275: configurar los valores de los campos del informe de Cáritas, solo Referente
  | 'informes.config'
  // SCRUM-180: gestionar categorías de observaciones, solo Referente (Escucha no accede)
  | 'categorias.manage';


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

    // Visitas de la Casa de Convivencia (SCRUM-74)
    'visitas.view',
    'visitas.create',
    'visitas.edit',

    // Asistencia diaria (SCRUM-218)
    'asistencia.view',
    'asistencia.create',
    'asistencia.edit',
    'asistencia.stats',
    'asistencia.config',
    'configuracion.respaldos',
    'informes.caritas',
    'informes.config',
    'categorias.manage'
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

    // Visitas de la Casa de Convivencia (SCRUM-74)
    'visitas.view',
    'visitas.create',
    'visitas.edit',

    // Asistencia diaria (SCRUM-218); modificar días cerrados es solo del Referente
    'asistencia.view',
    'asistencia.create'
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
    'visitas.edit',

    'asistencia.view',
    'asistencia.create'
  ]

};