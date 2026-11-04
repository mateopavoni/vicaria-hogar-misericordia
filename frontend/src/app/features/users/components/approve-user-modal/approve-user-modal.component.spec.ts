import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ApproveUserModalComponent } from './approve-user-modal.component';

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
