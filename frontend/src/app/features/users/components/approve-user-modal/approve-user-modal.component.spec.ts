import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ApproveUserModalComponent } from './approve-user-modal.component';

// bug reportado 2026-09-23: este spec estaba comentado por completo (con un nombre de
// clase viejo, "ApproveUserComponent") y Vitest lo contaba como archivo fallido
// ("No test suite found").
describe('ApproveUserModalComponent', () => {
  let component: ApproveUserModalComponent;
  let fixture: ComponentFixture<ApproveUserModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ApproveUserModalComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ApproveUserModalComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
