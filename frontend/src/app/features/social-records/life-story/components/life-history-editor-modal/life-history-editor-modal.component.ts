import { Component, input, output, signal, effect } from '@angular/core';
import { UiButtonComponent, UiFormFieldComponent, UiInputDirective, UiModalComponent } from '../../../../../shared/ui';

@Component({
  selector: 'app-life-history-editor-modal',
  imports: [UiModalComponent, UiButtonComponent, UiFormFieldComponent, UiInputDirective],
  templateUrl: './life-history-editor-modal.component.html',
})
export class LifeHistoryEditorModalComponent {
  stageTitle = input.required<string>();
  initialText = input<string>('');

  save = output<string>();
  cancel = output<void>();

  textSignal = signal<string>('');

  constructor() {
    effect(() => {
      this.textSignal.set(this.initialText() ?? '');
    });
  }

  onInput(event: Event): void {
    const textarea = event.target as HTMLTextAreaElement;
    this.textSignal.set(textarea.value);
  }

  onSave(): void {
    this.save.emit(this.textSignal().trim());
  }
}
