import { ComponentFixture, TestBed } from '@angular/core/testing';

import {  NotificationMenuComponent } from './notification-menu.component';

describe('NotificationMenuComponent', () => {
  let component: NotificationMenuComponent;
  let fixture: ComponentFixture<NotificationMenuComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NotificationMenuComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(NotificationMenuComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
