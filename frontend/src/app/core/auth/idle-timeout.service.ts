import { Injectable, effect, inject } from '@angular/core';

import { AuthService } from './auth.service';

// minutos sin actividad (mouse, teclado, scroll, touch) antes de cerrar la sesión
export const IDLE_TIMEOUT_MS = 30 * 60 * 1000;
export const SESSION_NOTICE_KEY = 'sessionNotice';
const LAST_ACTIVITY_KEY = 'lastActivityAt';
const ACTIVITY_EVENTS = ['mousemove', 'mousedown', 'keydown', 'scroll', 'touchstart', 'click'];

// SCRUM-96: expiración por inactividad real. La última actividad se guarda en
// localStorage para que varias pestañas abiertas compartan el mismo reloj.
@Injectable({ providedIn: 'root' })
export class IdleTimeoutService {
  private authService = inject(AuthService);
  private checker?: ReturnType<typeof setInterval>;
  private lastWrite = 0;
  private readonly onActivity = () => this.touch();

  constructor() {
    // arranca al iniciar sesión y se detiene al cerrarla
    effect(() => {
      if (this.authService.user()) {
        this.start();
      } else {
        this.stop();
      }
    });
  }

  private start(): void {
    if (this.checker) {
      return;
    }
    this.touch(true);
    ACTIVITY_EVENTS.forEach(e => window.addEventListener(e, this.onActivity, { passive: true }));
    this.checker = setInterval(() => this.check(), 30_000);
  }

  private stop(): void {
    if (!this.checker) {
      return;
    }
    clearInterval(this.checker);
    this.checker = undefined;
    ACTIVITY_EVENTS.forEach(e => window.removeEventListener(e, this.onActivity));
  }

  // escribe como máximo una vez por segundo para no castigar el storage con mousemove
  private touch(force = false): void {
    const now = Date.now();
    if (!force && now - this.lastWrite < 1000) {
      return;
    }
    this.lastWrite = now;
    try {
      localStorage.setItem(LAST_ACTIVITY_KEY, String(now));
    } catch { /* storage no disponible: el timeout no aplica */ }
  }

  private check(): void {
    const last = Number(localStorage.getItem(LAST_ACTIVITY_KEY) ?? Date.now());
    if (Date.now() - last >= IDLE_TIMEOUT_MS) {
      sessionStorage.setItem(SESSION_NOTICE_KEY, 'Tu sesión se cerró por inactividad. Iniciá sesión de nuevo.');
      this.authService.logout();
    }
  }
}
