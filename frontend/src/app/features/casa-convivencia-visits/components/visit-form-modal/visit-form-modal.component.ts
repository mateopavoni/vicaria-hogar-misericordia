import { Component, computed, effect, inject, input, output } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { UiButtonComponent, UiFormFieldComponent, UiInputDirective, UiModalComponent } from '../../../../shared/ui';
import { isBeforeToday, todayLocalIso } from '../../../../shared/utils/date.util';
import { CreateVisitDto, Resident, Visit } from '../../interfaces/visit.interface';
import { findConflictingVisits } from '../../utils/visit-conflict.util';

/**
 * SCRUM-214: alta y edición de una visita. Reactive form con residente (solo
 * residentes, ver `residents` input), visitante, fecha, hora y duración estimada.
 * SCRUM-74 (AC): "el sistema alerta si hay dos visitas al mismo residente en el mismo
 * horario" — alerta no bloqueante, se recalcula en vivo contra `existingVisits`.
 */
@Component({
  selector: 'app-visit-form-modal',
  imports: [ReactiveFormsModule, UiModalComponent, UiButtonComponent, UiFormFieldComponent, UiInputDirective],
  templateUrl: './visit-form-modal.component.html',
})
export class VisitFormModalComponent {
  private fb = inject(FormBuilder);

  // null = alta de una visita nueva; con valor = edición de esa visita
  visit = input<Visit | null>(null);

  residents = input<Resident[]>([]);
  residentsLoading = input(false);

  // visitas ya cargadas en el calendario (del rango visible), para chequear
  // conflictos de horario en vivo — ver findConflictingVisits
  existingVisits = input<Visit[]>([]);

  // fecha sugerida para la alta (ej. el día de la celda en la que se hizo clic)
  initialDate = input<string>(todayLocalIso());

  saving = input(false);
  errorMessage = input<string | null>(null);

  saved = output<CreateVisitDto>();
  closed = output<void>();

  title = computed(() => (this.visit() ? 'Editar visita' : 'Nueva visita'));

  submitted = false;

  // input[type=date] min, mismo criterio que event-form-modal: no se puede agendar
  // una visita en una fecha anterior a hoy.
  readonly minDate = todayLocalIso();

  form = this.fb.nonNullable.group({
    residentId: ['', [Validators.required]],
    visitorName: ['', [Validators.required, Validators.maxLength(150)]],
    date: [todayLocalIso(), [Validators.required]],
    time: ['10:00', [Validators.required]],
    durationMinutes: [30, [Validators.required, Validators.min(5), Validators.max(480)]],
  });

  constructor() {
    effect(() => {
      const visit = this.visit();
      if (visit) {
        const [date, time] = visit.start.split('T');
        this.form.patchValue({
          residentId: visit.residentId,
          visitorName: visit.visitorName,
          date,
          time: time?.slice(0, 5) ?? '',
          durationMinutes: visit.durationMinutes,
        });
        return;
      }

      this.form.patchValue({ date: this.initialDate() });
    });
  }

  get residentError(): string | null {
    const control = this.form.controls.residentId;
    if (!this.submitted || !control.errors) {
      return null;
    }
    return control.errors['required'] ? 'Elegí a qué residente visitan.' : null;
  }

  get visitorError(): string | null {
    const control = this.form.controls.visitorName;
    if (!this.submitted || !control.errors) {
      return null;
    }
    if (control.errors['required']) {
      return 'El nombre del visitante es obligatorio.';
    }
    if (control.errors['maxlength']) {
      return 'El nombre del visitante no puede superar los 150 caracteres.';
    }
    return null;
  }

  get dateError(): string | null {
    const control = this.form.controls.date;
    if (!this.submitted) {
      return null;
    }
    if (control.errors?.['required']) {
      return 'La fecha es obligatoria.';
    }
    return this.isPastDateInvalid() ? 'La fecha no puede ser anterior a hoy.' : null;
  }

  // Misma regla y mismo criterio de "permitir mantener una fecha ya pasada al editar"
  // que event-form-modal.component.ts — ver comentario ahí. Acá además una visita
  // pasada es el caso normal al marcarla realizada/cancelada después de que ocurrió,
  // así que no tendría sentido bloquear la edición solo porque su fecha ya pasó.
  private isPastDateInvalid(): boolean {
    const date = this.form.controls.date.value;
    if (!isBeforeToday(date)) {
      return false;
    }
    const originalDate = this.visit()?.start.split('T')[0];
    return date !== originalDate;
  }

  get durationError(): string | null {
    const control = this.form.controls.durationMinutes;
    if (!this.submitted || !control.errors) {
      return null;
    }
    if (control.errors['required']) {
      return 'La duración estimada es obligatoria.';
    }
    if (control.errors['min'] || control.errors['max']) {
      return 'La duración tiene que estar entre 5 y 480 minutos.';
    }
    return null;
  }

  // reactive forms no es signals: para que la alerta de conflicto se recalcule en
  // vivo mientras se completa el formulario, se convierte valueChanges a signal
  // (si no, un computed() acá nunca se volvería a disparar al tipear).
  private formValue = toSignal(this.form.valueChanges, { initialValue: this.form.getRawValue() });

  // alerta en vivo (AC de SCRUM-74), no bloquea el guardado
  conflicts = computed(() => {
    const { residentId, date, time, durationMinutes } = this.formValue();
    if (!residentId || !date || !time || !durationMinutes) {
      return [];
    }
    return findConflictingVisits(
      { id: this.visit()?.id, residentId, start: `${date}T${time}:00`, durationMinutes },
      this.existingVisits(),
    );
  });

  conflictTime(visit: Visit): string {
    return new Date(visit.start).toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' });
  }

  onClose(): void {
    this.closed.emit();
  }

  submit(): void {
    this.submitted = true;

    if (this.form.invalid || this.isPastDateInvalid()) {
      this.form.markAllAsTouched();
      return;
    }

    const { residentId, visitorName, date, time, durationMinutes } = this.form.getRawValue();

    this.saved.emit({
      residentId,
      visitorName: visitorName.trim(),
      start: `${date}T${time}:00`,
      durationMinutes,
    });
  }
}
