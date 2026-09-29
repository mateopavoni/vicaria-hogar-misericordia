import { Routes } from '@angular/router';

export const usersRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/user-management/user-management.component')
        .then(m => m.UserManagementComponent),
  },
];
