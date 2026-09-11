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
    // canActivate: [authGuard],
    loadComponent: () =>
      import('./shared/layout/layout.component')
        .then(m => m.LayoutComponent),

    children: [
      {
        path: 'users',

        loadComponent: () =>
          import('./features/users/pages/user-management/user-management.component')
            .then(m => m.UserManagementComponent),

        // canActivate: [
        //   permissionGuard('users.view')
        // ]
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

        // canActivate: [
        //   permissionGuard('fichas.view')
        // ]
      },
      
      {
       
        path: 'ficha-nueva',

        loadComponent: () =>
          import('./features/social-records/pages/new-social-record/new-social-record.component')
            .then(m => m.NewSocialRecordComponent),

        // canActivate: [
        //   permissionGuard('fichas.create')
        // ]
        //canDeactivate: [pendingChangesGuard] 
      },

      {

      path: 'fichas/:id',
      loadComponent: () =>
        import('./features/social-records/pages/social-record-detail/social-record-detail.component')
          .then(m => m.SocialRecordDetailComponent)

    },
    
    // RUTA DE EDICIÓN

    {
      path: 'fichas/:id/edit',
      loadComponent: () =>
        import('./features/social-records/pages/social-record-edit/social-record-edit.component')
          .then(m => m.SocialRecordEditComponent),
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
  // CUALQUIER RUTA DESCONOCIDA (Redirige al login de auth)
  // {
  //   path: '**',
  //   redirectTo: 'auth/login',
  // },

];

