import { Component, input } from '@angular/core';
import { DatePipe, CommonModule } from '@angular/common';
import { ChangeHistoryItem } from './../../interfaces/change-history.interface';

@Component({
  selector: 'app-change-history',
  standalone: true,
  imports: [CommonModule, DatePipe],
  templateUrl: './change-history.component.html'
})
export class ChangeHistoryComponent {
  // Entradas mediante Signals
  items = input.required<ChangeHistoryItem<any>[]>();
  title = input<string>('Historial de Modificaciones');
}