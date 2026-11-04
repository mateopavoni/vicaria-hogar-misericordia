import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RejectUserModalComponent } from './reject-user-modal.component';

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
