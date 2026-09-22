import { Component, inject, OnInit, signal, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ObservationCategoriesService } from '../../services/observation-categories.service';
import { ObservationsService } from '../../services/observations.service';
import { ObservationCategory } from '../../interfaces/observation-category.interface';
import { Observation } from '../../interfaces/observation.interface';

@Component({
  selector: 'app-create-observation-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './create-observation-modal.component.html'
})
export class CreateObservationModalComponent implements OnInit {
  private fb = inject(FormBuilder);
  private categoriesService = inject(ObservationCategoriesService);
  private observationsService = inject(ObservationsService);

  // Inputs & Outputs
  socialRecordId = input.required<string>();
  observationSaved = output<Observation>();
  cancel = output<void>();

  // Signals para manejar estado
  activeCategories = signal<ObservationCategory[]>([]);
  submitting = signal<boolean>(false);
  errorMessage = signal<string | null>(null);

  // Formulario reactivo
  observationForm = this.fb.group({
    categoryId: [null as string | null],
    content: ['', [Validators.required, Validators.minLength(3)]]
  });

  ngOnInit(): void {
    this.loadActiveCategories();
  }

  loadActiveCategories(): void {
    this.categoriesService.getActive().subscribe({
      next: (categories) => this.activeCategories.set(categories),
      error: (err: unknown) => console.error('Error al cargar categorías activas:', err)
    });
  }

  save(): void {
    if (observationFormInvalid(this.observationForm)) return;

    this.submitting.set(true);
    this.errorMessage.set(null);

    const dto = {
      socialRecordId: this.socialRecordId(),
      categoryId: this.observationForm.value.categoryId ?? null,
      content: this.observationForm.value.content!.trim()
    };

    this.observationsService.create(this.socialRecordId(), dto).subscribe({
      next: (newObservation) => {
        this.submitting.set(false);
        this.observationSaved.emit(newObservation);
      },
      error: (err: unknown) => {
        this.submitting.set(false);
        this.errorMessage.set('No se pudo guardar la observación. Intenta nuevamente.');
        console.error('Error al guardar observación:', err);
      }
    });
  }
}

function observationFormInvalid(form: any): boolean {
  if (form.invalid) {
    form.markAllAsTouched();
    return true;
  }
  return false;
}