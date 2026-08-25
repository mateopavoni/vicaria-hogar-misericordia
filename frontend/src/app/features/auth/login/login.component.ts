import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../../core/auth/auth.service';
import { LoginErrorType } from '../../../core/auth/auth.model';

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

  screen = signal<'form' | 'blocked' | 'pending'>('form');
  loading = signal(false);
  showPassword = signal(false);
  credentialsError = signal<string | null>(null);

  form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required]
  });

  togglePasswordVisibility(): void {
    this.showPassword.update((v) => !v);
  }

  submit(): void {
    if (this.form.invalid || this.loading()) {
      this.form.markAllAsTouched();
      return;
    }

    const { email, password } = this.form.getRawValue();
    this.credentialsError.set(null);
    this.loading.set(true);

    this.authService.login({ email, password }).subscribe({
      next: () => {
        this.loading.set(false);
        // TODO: navegar según rol
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.handleError(this.authService.mapError(err));
      }
    });
  }

  private handleError(type: LoginErrorType): void {
    if (type === 'blocked' || type === 'pending') {
      this.screen.set(type);
      return;
    }
    if (type === 'credentials') {
      this.credentialsError.set('Correo o contraseña incorrectos.');
      return;
    }
    this.credentialsError.set('No pudimos iniciar sesión. Probá de nuevo en unos minutos.');
  }

  goBackToStart(): void {
    this.screen.set('form');
    this.credentialsError.set(null);
  }
}
