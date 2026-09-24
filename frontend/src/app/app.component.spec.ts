import { TestBed } from '@angular/core/testing';
import { App } from './app.component';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  // bug reportado 2026-09-23: este test seguía comprobando el <h1> de bienvenida del
  // scaffold inicial de Angular CLI ("Hello, vicaria-prueba"); el template real de
  // App ya es solo un <router-outlet/>, sin ningún <h1> — el test fallaba siempre.
  it('should render the router outlet', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('router-outlet')).toBeTruthy();
  });
});
