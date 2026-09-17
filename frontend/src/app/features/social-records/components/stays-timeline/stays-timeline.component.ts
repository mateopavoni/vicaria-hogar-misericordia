import { Component, input } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { Stay } from '../../interfaces/stay.interface';

@Component({
  selector: 'app-stays-timeline',
  standalone: true,
  imports: [CommonModule, DatePipe],
  templateUrl: './stays-timeline.component.html'
})
export class StaysTimelineComponent {
  stays = input.required<Stay[]>();

  // Calcula la duración entre dos fechas o hasta hoy si sigue activo
  calculateDuration(entryDateStr: string, exitDateStr?: string | null): string {
    const entry = new Date(entryDateStr);
    const exit = exitDateStr ? new Date(exitDateStr) : new Date();

    const diffTime = Math.abs(exit.getTime() - entry.getTime());
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

    if (diffDays < 30) {
      return `${diffDays} ${diffDays === 1 ? 'día' : 'días'}`;
    }

    const months = Math.floor(diffDays / 30);
    const remainingDays = diffDays % 30;

    return remainingDays > 0
      ? `${months} m ${remainingDays} d`
      : `${months} ${months === 1 ? 'mes' : 'meses'}`;
  }
}