import { Injectable, signal } from '@angular/core';

// bug reportado 2026-09-23 (pedido de responsividad): el sidebar tenía ancho fijo (218px)
// sin ningún breakpoint ni forma de ocultarlo, dejando ~157px de ancho útil en mobile.
// Este servicio coordina el estado abierto/cerrado del drawer móvil entre topbar (botón
// hamburguesa), sidebar (el propio drawer) y layout (si hay backdrop).
@Injectable({ providedIn: 'root' })
export class SidebarStateService {
  isOpenOnMobile = signal(false);

  toggle(): void {
    this.isOpenOnMobile.update(open => !open);
  }

  close(): void {
    this.isOpenOnMobile.set(false);
  }
}
