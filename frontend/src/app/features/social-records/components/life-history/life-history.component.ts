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

  startEditing(stage: LifeHistoryStage): void {
    this.editingStage.set(stage);
  }

  closeEditor(): void {
    this.editingStage.set(null);
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

  get currentEditingText(): string {
    const stage = this.editingStage();
    if (!stage) return '';
    return this.history()?.[stage]?.text ?? '';
  }
}