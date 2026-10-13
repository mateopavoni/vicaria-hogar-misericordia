import { CanDeactivateFn } from '@angular/router';

export interface ComponentCanDeactivate {
  canDeactivate: () => boolean;
}

export const pendingChangesGuard: CanDeactivateFn<ComponentCanDeactivate> = (component) => {
  // Si el componente define canDeactivate() y devuelve false (formulario modificado/dirty)
  if (component.canDeactivate && !component.canDeactivate()) {
    return confirm('Tenés cambios sin guardar. ¿Estás seguro de que querés salir de la página?');
  }

  return true;
};