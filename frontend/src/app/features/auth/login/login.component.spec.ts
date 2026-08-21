import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { LoginComponent } from './login.component';

describe('LoginComponent', () => {
  let fixture: ComponentFixture<LoginComponent>;
  let component: LoginComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [provideHttpClient()]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('el formulario es inválido si falta email o password', () => {
    component.form.setValue({ email: '', password: '' });
    expect(component.form.invalid).toBe(true);
  });

  it('el formulario es válido con email y password correctos', () => {
    component.form.setValue({ email: 'test@correo.com', password: '123456' });
    expect(component.form.valid).toBe(true);
  });

  it('toggleMostrarPassword alterna el estado', () => {
    expect(component.mostrarPassword()).toBe(false);
    component.toggleMostrarPassword();
    expect(component.mostrarPassword()).toBe(true);
  });
});
