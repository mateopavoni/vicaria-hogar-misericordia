import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { IdleTimeoutService } from './core/auth/idle-timeout.service';


@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class App {
  protected readonly title = signal('vicaria-prueba');

  // solo con inyectarlo queda activo el control de inactividad
  private readonly idleTimeout = inject(IdleTimeoutService);
}
