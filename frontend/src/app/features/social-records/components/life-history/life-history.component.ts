import { Component, input, output, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { LifeHistory, LifeHistoryStage } from './../../interfaces/life-history.interface';
import { LifeHistoryEditorModalComponent } from './../life-history-editor-modal/life-history-editor-modal.component';
import { EmptyFieldBadgeComponent } from '../../../../shared/components/empty-field-badge/empty-field-badge.component';
@Component({
  selector: 'app-life-history',
  standalone: true,
  imports: [DatePipe, LifeHistoryEditorModalComponent, EmptyFieldBadgeComponent],
  templateUrl: './life-history.component.html'
})
export class LifeHistoryComponent {
  history = input<LifeHistory | null>(null);

  editingStage = signal<LifeHistoryStage | null>(null);

  historySaved = output<{
    stage: LifeHistoryStage;
    text: string;
    lastEditedBy: string;
    lastEditedAt: string;
  }>();

  stages: { id: LifeHistoryStage; title: string; colorClass: string; textClass: string }[] = [
    { id: 'beforeHome', title: 'Antes del hogar', colorClass: 'bg-emerald-400', textClass: 'text-emerald-600' },
    { id: 'duringHome', title: 'En el hogar', colorClass: 'bg-orange-400', textClass: 'text-orange-500' },
    { id: 'afterHome', title: 'Después del hogar', colorClass: 'bg-purple-500', textClass: 'text-purple-600' }
  ];

  // por etapa: si se está mostrando el historial completo de entradas anteriores
  expandedStage = signal<LifeHistoryStage | null>(null);

  // bug reportado 2026-09-23: "Editar" guardaba siempre sobre la misma entrada en vez de
  // crear una nueva. Ahora el modal siempre arranca en blanco: cada guardado es una entrada
  // nueva en el historial, no una edición de la anterior.
  startNewEntry(stage: LifeHistoryStage): void {
    this.editingStage.set(stage);
  }

  closeEditor(): void {
    this.editingStage.set(null);
  }

  toggleHistory(stage: LifeHistoryStage): void {
    this.expandedStage.update(current => (current === stage ? null : stage));
  }

  handleSave(text: string): void {
    const stage = this.editingStage();
    if (!stage) return;

    this.historySaved.emit({
      stage,
      text,
      lastEditedBy: 'Usuario actual',
      lastEditedAt: new Date().toISOString()
    });

    this.closeEditor();
  }

  get currentEditingTitle(): string {
    const stage = this.editingStage();
    return this.stages.find(s => s.id === stage)?.title ?? '';
  }
}