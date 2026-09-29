import { CanDeactivateFn } from '@angular/router';
import { Observable } from 'rxjs';

/**
 * canDeactivate() ahora puede devolver un Observable<boolean> además de un
 * boolean: cuando hay cambios sin guardar, el componente ya no puede resolver
 * la pregunta en el momento (como con el confirm() nativo, bloqueante) porque
 * la respuesta depende de que la persona interactúe con un modal propio
 * (ui-confirm-dialog). El componente abre el modal y devuelve un Observable
 * que emite true/false recién cuando confirma o cancela.
 */
export interface ComponentCanDeactivate {
  canDeactivate: () => boolean | Observable<boolean>;
}

export const pendingChangesGuard: CanDeactivateFn<ComponentCanDeactivate> = (component) => {
  return component.canDeactivate ? component.canDeactivate() : true;
};
