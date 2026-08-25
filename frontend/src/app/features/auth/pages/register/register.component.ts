import { Component, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/auth/auth.service';


export const matchPasswordValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const password = control.get('password')?.value;
  const confirmPassword = control.get('confirmPassword')?.value;
  return password === confirmPassword ? null : { passwordMismatch: true };
};

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})

export class RegisterComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);

  loading = signal(false);
  showPassword = signal(false);
  errorMessage = signal<string | null>(null);

  private passwordPattern = /^(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]).{8,}$/;

  registerForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    lastname: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.pattern(this.passwordPattern)]],
  });

  togglePasswordVisibility(): void {

    this.showPassword.update(
      value => !value
    );

  }
  submit(): void {
     ///Evita enviar el formulario
    // si ya se está procesando una petición.
    if (this.loading()) {
      return;
    }

    // Validación del formulario.
    if (this.registerForm.invalid) {

      this.registerForm.markAllAsTouched();
    ///  this.errorMessage.set('Por favor completa todos los campos obligatorios correctamente.');

      return;
    }

    // Limpiamos errores anteriores.
    this.errorMessage.set(null);

    // SI EL FORMULARIO ES VÁLIDO:
    this.loading.set(true);

    const { name, lastname, email, password } = this.registerForm.getRawValue();
  
    this.authService.register({ name, lastname, email, password }).subscribe({
      next: () => {
        this.loading.set(false);
        this.router.navigate(['/auth/pending-approval']);
      },

       error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err?.error?.message || 'Ocurrió un error al procesar el registro.');
      }

      });

  }

     
    
  }
