import { Component, inject, signal } from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';

import { AuthService } from '../../../../core/auth/auth.service';
import { LoginErrorType, LoginRequest } from '../../../../core/auth/auth.interfaces';
import { AuthLayoutComponent } from '../../auth-layout/auth-layout.component';
import { UserRole } from '../../../../core/auth/userRole';

@Component({
  selector: 'app-login',
  imports: [
    RouterLink,
    ReactiveFormsModule,
   // AuthLayoutComponent
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css',
})
  export class LoginComponent {

    private fb = inject(FormBuilder);
    private authService = inject(AuthService);
    private router = inject(Router);

    loading = signal(false);
    showPassword = signal(false);
    credentialsError = signal<string | null>(null);
    // solo mostramos errores de validación después de intentar enviar el formulario
    submitted = signal(false);

    private passwordPattern = /^(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]).{8,}$/;
    // guarda el timer para poder cancelarlo si se muestra otro error antes de que pase el tiempo
    private credentialsErrorTimeout?: ReturnType<typeof setTimeout>;

    form = this.fb.nonNullable.group({

      email: [ '',[Validators.required, Validators.email]],
      password: ['',[  Validators.required, Validators.minLength(6), Validators.pattern(this.passwordPattern)]],
      remember: [false]
    });

    constructor() {
      // si la vez pasada tildó "recordar usuario", precargamos el email guardado
      const savedEmail = localStorage.getItem('rememberedEmail');
      if (savedEmail) {
        this.form.patchValue({ email: savedEmail, remember: true });
      }
    }


    togglePasswordVisibility(): void {

      this.showPassword.update(
        value => !value
      );

    }


    submit(): void {

      // Evita enviar el formulario // si ya se está procesando una petición.
      if (this.loading()) {
        return;
      }

      this.submitted.set(true);

      // Validación del formulario.
      if (this.form.invalid) {

        this.form.markAllAsTouched();

        return;
      }

      // Limpiamos errores anteriores.
      clearTimeout(this.credentialsErrorTimeout);
      this.credentialsError.set(null);

      this.loading.set(true);

      const { email, password, remember} = this.form.getRawValue();

      this.authService.login({ email, password }).subscribe({
        next: (response) => {
          this.loading.set(false);

          // guardamos o borramos el email recordado según el checkbox
          if (remember) {
            localStorage.setItem('rememberedEmail', email);
          } else {
            localStorage.removeItem('rememberedEmail');
          }

          this.redirectByRole(
          response.user.role
            );
          },

          error: (err: HttpErrorResponse) => {
            this.loading.set(false);

            const errorType =
              this.authService.mapError(err);
              this.handleError(errorType);

            // credenciales incorrectas: limpiamos la password (y el email si no se pidió recordar)
            if (errorType === 'credentials') {
              this.form.patchValue({ password: '' });
              if (!remember) {
                this.form.patchValue({ email: '' });
              }
              this.submitted.set(false);
            }

          }

        });

    }


      private handleError(type: LoginErrorType): void {
    switch (type) {
      case 'blocked':
        this.router.navigate(['/auth/blocked']);
        break;

      case 'pending':
        
        this.router.navigate(['/auth/pending-approval']);
        break;

      case 'credentials':
        this.setCredentialsError('Correo o contraseña incorrectos.');
        break;

      default:
        this.setCredentialsError('No pudimos iniciar sesión. Probá de nuevo en unos minutos.');
        break;
    }
  }

  // muestra el error y lo borra solo a los 5 segundos, para que no quede pegado para siempre
  private setCredentialsError(message: string): void {
    clearTimeout(this.credentialsErrorTimeout);

    this.credentialsError.set(message);

    this.credentialsErrorTimeout = setTimeout(() => {
      this.credentialsError.set(null);
    }, 5000);
  }

    private redirectByRole(role: UserRole | null): void {

      switch (role) {

        case 'Referente':

          this.router.navigate([
            '/dashboard/users'
          ]);
          break;

        case 'DirectoraDeCasona':

          this.router.navigate([
            '/dashboard'
          ]);

          break;

        case 'Escucha':

          this.router.navigate([
            '/dashboard'
          ]);
          
          break;


        default:

          this.router.navigate([
            '/dashboard'
          ]);

          break;

      }

    }
  ;

    

  }