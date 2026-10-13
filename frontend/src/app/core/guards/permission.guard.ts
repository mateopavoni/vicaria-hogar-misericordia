import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { PermissionService } from '../auth/permission.service';
import { Permission } from '../auth/permissions';

export const permissionGuard = (
  permission: Permission
): CanActivateFn => {

  return () => {

    const permissionService = inject(PermissionService);
    const router = inject(Router);

    const hasPermission =
      permissionService.hasPermission(permission);

    if (hasPermission) {
      return true;
    }

    return router.createUrlTree([
      '/access-denied'
    ]);
  };
};