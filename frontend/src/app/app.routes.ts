import { Routes } from '@angular/router';

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
        // canActivate: [referenteGuard],
        loadComponent: () =>
          import(
            './features/users/pages/user-management/user-management.component'
          ).then(m => m.UserManagementComponent)
      },
    ],
  },

  // CUALQUIER RUTA DESCONOCIDA (Redirige al login de auth)
  {
    path: '**',
    redirectTo: 'auth/login',
  },

];

