import { Routes } from '@angular/router';
import { permissionGuard } from './core/guards/permission.guard';
import { authGuard } from './core/auth/auth.guard';
import { pendingChangesGuard } from './core/guards/pending-changes.guard';

export const routes: Routes = [

  // AUTENTICACIÓN (Centraliza login, register y pending-approval)
  {
    path: 'auth',
    loadChildren: () =>
      import('./features/auth/auth.routes')
        .then(m => m.authRoutes),
  },

  // REDIRECCIÓN INICIAL
  {
    path: '',
    redirectTo: 'auth',
    pathMatch: 'full',
  },

  // SISTEMA PRINCIPAL
  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./shared/layout/layout.component')
        .then(m => m.LayoutComponent),

    children: [
      // INICIO (/dashboard): estadísticas de asistencia para Referente (SCRUM-226)
      {
        path: '',
        pathMatch: 'full',
        loadComponent: () =>
          import('./features/home/pages/home/home.component')
            .then(m => m.HomeComponent),
      },
      {
        path: 'users',

        loadComponent: () =>
          import('./features/users/pages/user-management/user-management.component')
            .then(m => m.UserManagementComponent),

        canActivate: [permissionGuard('users.view')]
      },
      // SCRUM-6 (listado)
      {
        path: 'fichas',

        loadComponent: () =>
          import(
            './features/social-records/pages/social-record-list/social-record-list.component'
          )
            .then(
              m => m.SocialRecordListComponent
            ),
      },

      // Redirect por compatibilidad (URL vieja del botón "Nueva ficha")
      {
        path: 'ficha-nueva',
        redirectTo: 'fichas/crear',
        pathMatch: 'full',
      },

      {
        path: 'fichas/crear',

        loadComponent: () =>
          import('./features/social-records/pages/new-social-record/new-social-record.component')
            .then(m => m.NewSocialRecordComponent),

        canActivate: [permissionGuard('fichas.create')]
      },

      {
        path: 'fichas/:id',
        loadComponent: () =>
          import('./features/social-records/pages/social-record-detail/social-record-detail.component')
            .then(m => m.SocialRecordDetailComponent)
      },

      // EP-05: gestión y búsqueda de colaboradores (backend: api/collaborators)
      {
        path: 'colaboradores',
        loadComponent: () =>
          import('./features/collaborators/pages/collaborator-management/collaborator-management.component')
            .then(m => m.CollaboratorManagementComponent),
        canActivate: [permissionGuard('colaboradores.view')]
      },

      // EP-04: calendario general/personal; las visitas de la Casa de Convivencia son una pestaña
      {
        path: 'calendario',
        loadComponent: () =>
          import('./features/calendar/pages/calendar/calendar.component')
            .then(m => m.CalendarComponent),
        canActivate: [permissionGuard('calendario.view')]
      },

      // SCRUM-218: registro de asistencia diaria
      {
        path: 'asistencia',
        loadComponent: () =>
          import('./features/attendance/pages/daily-attendance/daily-attendance.component')
            .then(m => m.DailyAttendanceComponent),
        canActivate: [permissionGuard('asistencia.view')]
      },

      // SCRUM-230: configuración de actividades de asistencia, solo Referente
      {
        path: 'asistencia/actividades',
        loadComponent: () =>
          import('./features/attendance/pages/activity-settings/activity-settings.component')
            .then(m => m.ActivitySettingsComponent),
        canActivate: [permissionGuard('asistencia.config')]
      },

      // SCRUM-226: panel de estadísticas de asistencia, solo Referente
      {
        path: 'asistencia/estadisticas',
        loadComponent: () =>
          import('./features/attendance/components/attendance-stats-panel/attendance-stats-panel.component')
            .then(m => m.AttendanceStatsPanelComponent),
        canActivate: [permissionGuard('asistencia.stats')]
      },

      // SCRUM-238: agenda diaria de medicamentos (ver: medicamentos.view; marcar tomas: medicamentos.edit)
      {
        path: 'medicacion',
        loadComponent: () =>
          import('./features/medication/pages/medication-agenda/medication-agenda.component')
            .then(m => m.MedicationAgendaComponent),
        canActivate: [permissionGuard('medicamentos.view')]
      },

      // SCRUM-246: inventario de medicamentos, solo Referente y Directora
      {
        path: 'medicacion/stock',
        loadComponent: () =>
          import('./features/medication-stock/pages/medication-stock/medication-stock.component')
            .then(m => m.MedicationStockComponent),
        canActivate: [permissionGuard('medicamentos.stock')]
      },

      // SCRUM-267: respaldos de la base de datos, solo administrador técnico (Referente)
      {
        path: 'configuracion/respaldos',
        loadComponent: () =>
          import('./features/backups/pages/backups/backups.component')
            .then(m => m.BackupsComponent),
        canActivate: [permissionGuard('configuracion.respaldos')]
      },

      // RUTA DE EDICIÓN
      {
        path: 'fichas/:id/edit',
        loadComponent: () =>
          import('./features/social-records/pages/social-record-edit/social-record-edit.component')
            .then(m => m.SocialRecordEditComponent),
        canActivate: [permissionGuard('fichas.edit')],
        canDeactivate: [pendingChangesGuard]
      }
    ],
  },

  {
  path: 'access-denied',

  loadComponent: () =>
    import('./shared/components/access-denied/access-denied.component')
      .then(m => m.AccessDeniedComponent),
   },
  // CUALQUIER RUTA DESCONOCIDA (Página 404, para que links muertos no fallen en silencio)
  {
    path: '**',
    loadComponent: () =>
      import('./shared/components/not-found/not-found.component')
        .then(m => m.NotFoundComponent)
  },

];

