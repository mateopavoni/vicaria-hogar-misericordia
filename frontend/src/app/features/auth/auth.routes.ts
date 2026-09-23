import { Routes } from '@angular/router';
import { authGuard } from '../../core/auth/auth.guard';

export const authRoutes: Routes = [

  {
    
    path: '',
    loadComponent: () =>
      import('./auth-layout/auth-layout.component')
        .then(m => m.AuthLayoutComponent),

    children: [

      {
        path: '',
        redirectTo: 'login',
        pathMatch: 'full',
      },

      {
        path: 'login',
        loadComponent: () =>
          import('./pages/login/login.component')
            .then(m => m.LoginComponent),
      },

      {
        path: 'register',
        loadComponent: () =>
          import('./pages/register/register.component')
            .then(m => m.RegisterComponent),
      },

      {
        path: 'pending-approval',
        loadComponent: () =>
          import('./pages/pending-approval/pending-approval.component')
            .then(m => m.PendingApprovalComponent),
      },

    ],
  },

];