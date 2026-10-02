import { Component, computed, effect, inject, input, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { UiButtonComponent, UiFormFieldComponent, UiInputDirective, UiModalComponent } from '../../../../shared/ui';
import { todayLocalIso } from '../../../../shared/utils/date.util';
import { CalendarEvent, EventFormValue, RecurrenceFrequency } from '../../interfaces/calendar-event.interface';

/**
 * SCRUM-186/SCRUM-187/SCRUM-191: alta y edición de un evento del calendario,
 * incluyendo si es recurrente (diario/semanal/mensual, AC de SCRUM-16). No se usa para
 * las actividades recurrentes precargadas (no se pueden editar, ver AC de SCRUM-15).
 * Oculto para el rol Escucha (el padre decide si renderiza el botón que abre esto).
 * SCRUM-197 (AC): "los eventos personales siguen el mismo formulario de carga que los
 * generales" — por eso este componente no sabe nada de `scope`: lo decide
 * CalendarComponent (según la pestaña activa, o conservando el del evento original al
 * editar), no un campo del formulario.
 */
@Component({
  selector: 'app-event-form-modal',
  imports: [ReactiveFormsModule, UiModalComponent, UiButtonComponent, UiFormFieldComponent, UiInputDirective],
  templateUrl: './event-form-modal.component.html',
})
export class EventFormModalComponent {
  private fb = inject(FormBuilder);

  // null = alta de un evento nuevo; con valor = edición de ese evento
  event = input<CalendarEvent | null>(null);

  // fecha sugerida para la alta (ej. el día de la celda en la que se hizo clic)
  initialDate = input<string>(todayLocalIso());

  saving = input(false);
  errorMessage = input<string | null>(null);

  saved = output<EventFormValue>();
  closed = output<void>();

  title = computed(() => (this.event() ? 'Editar evento' : 'Nuevo evento'));

  submitted = false;

  readonly recurrenceOptions: { value: RecurrenceFrequency; label: string }[] = [
    { value: 'none', label: 'No se repite' },
    { value: 'daily', label: 'Todos los días' },
    { value: 'weekly', label: 'Todas las semanas' },
    { value: 'monthly', label: 'Todos los meses' },
  ];

  form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(150)]],
    date: [todayLocalIso(), [Validators.required]],
    startTime: ['09:00', [Validators.required]],
    endTime: ['10:00', [Validators.required]],
    description: ['', [Validators.maxLength(500)]],
    recurrence: ['none' as RecurrenceFrequency],
  });

  constructor() {
    effect(() => {
      const event = this.event();
      if (event) {
        const [startDate, startTime] = event.start.split('T');
        const endTime = event.end.split('T')[1]?.slice(0, 5) ?? '';
        this.form.patchValue({
          title: event.title,
          date: startDate,
          startTime: startTime?.slice(0, 5) ?? '',
          endTime,
          description: event.description ?? '',
          recurrence: event.recurrence ?? 'none',
        });
        return;
      }

      this.form.patchValue({ date: this.initialDate() });
    });
  }

  get titleError(): string | null {
    const control = this.form.controls.title;
    if (!this.submitted || !control.errors) {
      return null;
    }
    if (control.errors['required']) {
      return 'El título es obligatorio.';
    }
    if (control.errors['maxlength']) {
      return 'El título no puede superar los 150 caracteres.';
    }
    return null;
  }

  // SCRUM-16 (AC): "si se intenta guardar un evento sin título o sin fecha, el sistema
  // muestra un error de validación claro" — faltaba este getter, el control ya era
  // required pero no se mostraba ningún mensaje.
  get dateError(): string | null {
    const control = this.form.controls.date;
    if (!this.submitted || !control.errors) {
      return null;
    }
    return control.errors['required'] ? 'La fecha es obligatoria.' : null;
  }

  get timeRangeError(): string | null {
    if (!this.submitted) {
      return null;
    }
    const { startTime, endTime } = this.form.getRawValue();
    if (startTime && endTime && endTime <= startTime) {
      return 'El horario de fin tiene que ser posterior al de inicio.';
    }
    return null;
  }

  get descriptionError(): string | null {
    const control = this.form.controls.description;
    if (!this.submitted || !control.errors) {
      return null;
    }
    return control.errors['maxlength'] ? 'La descripción no puede superar los 500 caracteres.' : null;
  }

  onClose(): void {
    this.closed.emit();
  }

  submit(): void {
    this.submitted = true;

    if (this.form.invalid || this.timeRangeError) {
      this.form.markAllAsTouched();
      return;
    }

    const { title, date, startTime, endTime, description, recurrence } = this.form.getRawValue();

    this.saved.emit({
      title: title.trim(),
      description: description.trim() || null,
      start: `${date}T${startTime}:00`,
      end: `${date}T${endTime}:00`,
      recurrence,
    });
  }
}
