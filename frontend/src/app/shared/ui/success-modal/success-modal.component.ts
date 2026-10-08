import { Component, EventEmitter, Output, input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-success-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './success-modal.component.html'
})
export class SuccessModalComponent {
  title = input<string>('¡Operación Exitosa!');
  message = input<string>('La acción se completó correctamente.');
  buttonText = input<string>('Aceptar');

  @Output() close = new EventEmitter<void>();
}