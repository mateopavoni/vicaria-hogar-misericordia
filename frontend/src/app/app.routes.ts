import { Routes } from '@angular/router';
import { permissionGuard } from './core/guards/permission.guard';
import { authGuard } from './core/guards/auth.guard';

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
    // canActivate: [authGuard],
    loadComponent: () =>
      import('./core/layout/layout.component')
        .then(m => m.LayoutComponent),

    children: [
      {
        path: 'users',

        loadChildren: () =>
          import('./features/users/users.routes')
            .then(m => m.usersRoutes),

        // canActivate: [
        //   permissionGuard('users.view')
        // ]
      },

      // Redirect por compatibilidad (URL vieja del botón "Nueva ficha")
      {
        path: 'ficha-nueva',
        redirectTo: 'fichas/crear',
        pathMatch: 'full',
      },

      // SCRUM-6 (listado) y el resto de las rutas de fichas, agrupadas con lazy loading por feature
      {
        path: 'fichas',
        loadChildren: () =>
          import('./features/social-records/social-records.routes')
            .then(m => m.socialRecordsRoutes),

        // canActivate: [
        //   permissionGuard('fichas.view')
        // ]
      },

      // SCRUM-181: configuración de categorías de observaciones — solo Referente
      // (el backend ya restringe alta/edición/baja a ese rol; esto lo refleja en el front)
      {
        path: 'configuracion/categorias',
        loadComponent: () =>
          import('./features/social-records/observations/pages/category-settings/category-settings.component')
            .then(m => m.CategorySettingsComponent),
        canActivate: [
          permissionGuard('categorias.manage')
        ]
      },

      // SCRUM-29/SCRUM-18: alta, edición, baja y reactivación de colaboradores.
      // colaboradores.view ya está en ROLE_PERMISSIONS solo para Referente y
      // DirectoraDeCasona, así que bloquea a Escucha (y a Coordinador) como pide el AC.
      // NOTA: el backend de esta feature todavía no existe (ver comentario en
      // collaborator.interface.ts) — ruta y pantalla listas para cuando esté.
      {
        path: 'colaboradores',
        loadComponent: () =>
          import('./features/collaborators/pages/collaborator-management/collaborator-management.component')
            .then(m => m.CollaboratorManagementComponent),
        canActivate: [
          permissionGuard('colaboradores.view')
        ]
      },

      // SCRUM-28/SCRUM-15: calendario general del Centro Barrial. calendario.view ya
      // está en ROLE_PERMISSIONS para Referente y DirectoraDeCasona, no para Escucha
      // (que según el AC solo puede ver, nunca crear/editar — el componente lo oculta
      // además chequeando calendario.create/calendario.edit).
      // NOTA: el backend de esta feature todavía no existe (ver comentario en
      // calendar-event.interface.ts) — ruta y pantalla listas para cuando esté.
      {
        path: 'calendario',
        loadComponent: () =>
          import('./features/calendar/pages/calendar/calendar.component')
            .then(m => m.CalendarComponent),
        canActivate: [
          permissionGuard('calendario.view')
        ]
      },
    ],
  },

  {
  path: 'access-denied',

  loadComponent: () =>
    import('./shared/pages/access-denied/access-denied.component')
      .then(m => m.AccessDeniedComponent),
   },
  // CUALQUIER RUTA DESCONOCIDA (Página 404, para que links muertos no fallen en silencio)
  {
    path: '**',
    loadComponent: () =>
      import('./shared/pages/not-found/not-found.component')
        .then(m => m.NotFoundComponent)
  },

];

