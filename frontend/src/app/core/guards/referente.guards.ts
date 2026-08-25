import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';

export const referenteGuard: CanActivateFn = () => {

  const authService = inject(AuthService);
  const router = inject(Router);

  const user = authService.user();

  if (!user) {
    return router.createUrlTree(['/auth/login']);
  }

  if (user.role !== 'Referente') {
    return router.createUrlTree(['/dashboard']);
  }

  return true;
};