import { HttpInterceptorFn } from '@angular/common/http';

// agrega el token guardado en el login a todos los pedidos que van a nuestra propia API
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = localStorage.getItem('token');

  if (!token || !req.url.startsWith('/api')) {
    return next(req);
  }

  const authReq = req.clone({
    setHeaders: { Authorization: `Bearer ${token}` },
  });

  return next(authReq);
};
