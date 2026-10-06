import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';

import { UserManagementComponent } from './user-management.component';

// bug reportado 2026-09-23 (limpieza de test-infra): ngOnInit dispara HTTP reales
// (usuarios activos/inactivos) que, sin HttpClientTesting, intentaban resolver una URL
// relativa contra el entorno de fetch de Vitest/jsdom y fallaban con una excepción no
// controlada después de terminado el test ("should create" igual pasaba, pero quedaba
// ruido de "Unhandled Errors"). Se intercepta con HttpTestingController.
describe('UserManagementComponent', () => {
  let component: UserManagementComponent;
  let fixture: ComponentFixture<UserManagementComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UserManagementComponent],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);

    fixture = TestBed.createComponent(UserManagementComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  afterEach(() => {
    httpMock.match(() => true).forEach(req => req.flush({ items: [], total: 0, totalPages: 0 }));
    httpMock.verify();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
