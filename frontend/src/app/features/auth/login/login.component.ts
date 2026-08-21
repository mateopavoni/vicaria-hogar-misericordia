import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../../core/auth/auth.service';
import { LoginErrorTipo } from '../../../core/auth/auth.model';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);

  // pantalla completa que reemplaza el formulario (bloqueada / pendiente)
  pantalla = signal<'form' | 'bloqueada' | 'pendiente'>('form');
  loading = signal(false);
  mostrarPassword = signal(false);
  errorCredenciales = signal<string | null>(null);

  form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required]
  });

  toggleMostrarPassword(): void {
    this.mostrarPassword.update((v) => !v);
  }

  submit(): void {
    if (this.form.invalid || this.loading()) {
      this.form.markAllAsTouched();
      return;
    }

    const { email, password } = this.form.getRawValue();
    this.errorCredenciales.set(null);
    this.loading.set(true);

    this.authService.login({ email, password }).subscribe({
      next: () => {
        this.loading.set(false);
        // TODO: navegar al panel según rol cuando exista (lo arma el menú por rol).
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.manejarError(this.authService.mapearError(err));
      }
    });
  }

  private manejarError(tipo: LoginErrorTipo): void {
    if (tipo === 'bloqueada' || tipo === 'pendiente') {
      this.pantalla.set(tipo);
      return;
    }
    if (tipo === 'credenciales') {
      this.errorCredenciales.set('Correo o contraseña incorrectos.');
      return;
    }
    this.errorCredenciales.set('No pudimos iniciar sesión. Probá de nuevo en unos minutos.');
  }

  volverAlInicio(): void {
    this.pantalla.set('form');
    this.errorCredenciales.set(null);
  }
}
