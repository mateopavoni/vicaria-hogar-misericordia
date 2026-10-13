import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

// no hay envío de mails en el sistema: la recuperación de acceso se resuelve con un Referente
@Component({
  selector: 'app-forgot-password',
  imports: [RouterLink],
  template: `
    <div class="rounded-2xl border border-[#E3E8F5] bg-white p-10 text-center shadow-[0_8px_30px_rgba(79,115,232,0.08)]">
      <h2 class="text-[25px] font-bold text-slate-950">¿Olvidaste tu contraseña?</h2>

      <p class="mt-4 text-[15px] leading-6 text-slate-500">
        Por ahora el sistema no envía correos de recuperación.
        <br />
        Pedile a un Referente de tu equipo que te ayude
        <br />
        a recuperar el acceso a tu cuenta.
      </p>

      <a
        routerLink="/auth/login"
        class="mt-6 inline-flex items-center gap-1.5 text-[14px] font-semibold text-[#4167D9] hover:text-[#3155C5]"
      >
        Volver al inicio de sesión
      </a>
    </div>
  `,
})
export class ForgotPasswordComponent {}
