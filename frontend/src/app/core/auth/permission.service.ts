import { Injectable, inject } from '@angular/core';
import { AuthService } from './auth.service';
import {Permission,ROLE_PERMISSIONS} from './permissions';

@Injectable({
  providedIn: 'root'
})
export class PermissionService {

  private authService = inject(AuthService);


  hasPermission(permission: Permission): boolean {

    const user = this.authService.user();

    if (!user?.role) {
      return false;
    }

    return ROLE_PERMISSIONS[user.role]
      ?.includes(permission) ?? false;
  }


  hasAnyPermission(
    permissions: Permission[]
  ): boolean {

    return permissions.some(
      permission => this.hasPermission(permission)
    );
  }


  hasAllPermissions(
    permissions: Permission[]
  ): boolean {

    return permissions.every(
      permission => this.hasPermission(permission)
    );
  }


  hasRole(role: string): boolean {

    return this.authService.user()?.role === role;
  }

}