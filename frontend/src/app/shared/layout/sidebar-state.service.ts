import { Injectable, signal } from '@angular/core';

// estado abierto/cerrado del menú lateral en mobile, compartido por topbar, sidebar y layout
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
