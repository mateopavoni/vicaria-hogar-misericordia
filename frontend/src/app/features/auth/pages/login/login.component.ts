import { Component, inject, signal } from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';

import { AuthService } from '../../../../core/auth/auth.service';
import { LoginErrorType } from '../../../../core/auth/auth.interfaces';
import { AuthLayoutComponent } from '../../auth-layout/auth-layout.component';

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

  private passwordPattern = /^(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]).{8,}$/;

  form = this.fb.nonNullable.group({

    email: [ '',[Validators.required, Validators.email]],
    password: ['',[  Validators.required, Validators.minLength(6), Validators.pattern(this.passwordPattern)]],
    remember: [false]
  });


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

    // Validación del formulario.
    if (this.form.invalid) {

      this.form.markAllAsTouched();

      return;
    }

    // Limpiamos errores anteriores.
    this.credentialsError.set(null);

    this.loading.set(true);

    const { email, password} = this.form.getRawValue();

    this.authService.login({ email, password }).subscribe({
       next: (response) => {
         this.loading.set(false);
         this.redirectByRole(
         response.user.role
          );
        },

        error: (err: HttpErrorResponse) => {
          this.loading.set(false);

          const errorType =
            this.authService.mapError(err);
            this.handleError(errorType);

        }

      });

  }


    private handleError(
      type: LoginErrorType
    ): void {

      switch (type) {

        case 'blocked':

          this.router.navigate([
            '/auth/blocked'
          ]);

          break;


        case 'pending':

          this.router.navigate([
            '/auth/pending-approval'
          ]);

          break;


        case 'credentials':

          this.credentialsError.set(
            'Correo o contraseña incorrectos.'
          );

          break;


        default:

          this.credentialsError.set(
            'No pudimos iniciar sesión. Probá de nuevo en unos minutos.'
          );

          break;

      }

    }


  private redirectByRole(
    role: string | null
  ): void {

    switch (role) {

      case 'admin':

        this.router.navigate([
          'auth/dashboard'
        ]);

        break;


      default:

        this.router.navigate([
          'auth/dashboard'
        ]);

        break;

    }

  }

}