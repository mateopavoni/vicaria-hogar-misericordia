import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RejectUserModalComponent } from './reject-user-modal.component';

// bug reportado 2026-09-23: este spec estaba comentado por completo (con nombres de
// clase viejos, "RejectUserComponent"/"RejectUserRequest") y Vitest lo contaba como
// archivo fallido ("No test suite found").
describe('RejectUserModalComponent', () => {
  let component: RejectUserModalComponent;
  let fixture: ComponentFixture<RejectUserModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RejectUserModalComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(RejectUserModalComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
