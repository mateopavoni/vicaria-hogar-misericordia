import { Routes } from '@angular/router';
import { pendingChangesGuard } from '../../core/guards/pending-changes.guard';

export const socialRecordsRoutes: Routes = [
  // SCRUM-6 (listado)
  {
    path: '',
    loadComponent: () =>
      import('./pages/social-record-list/social-record-list.component')
        .then(m => m.SocialRecordListComponent),
  },

  // Redirect por compatibilidad (URL vieja del botón "Nueva ficha")
  {
    path: 'crear-legacy',
    redirectTo: 'crear',
    pathMatch: 'full',
  },

  {
    path: 'crear',
    loadComponent: () =>
      import('./pages/new-social-record/new-social-record.component')
        .then(m => m.NewSocialRecordComponent),
  },

  {
    path: ':id',
    loadComponent: () =>
      import('./pages/social-record-detail/social-record-detail.component')
        .then(m => m.SocialRecordDetailComponent),
  },

  {
    path: ':id/edit',
    loadComponent: () =>
      import('./pages/social-record-edit/social-record-edit.component')
        .then(m => m.SocialRecordEditComponent),
    canDeactivate: [pendingChangesGuard],
  },
];
