import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { UiButtonComponent, UiTab, UiTabsComponent } from '../../../../shared/ui';
import { AuthService } from '../../../../core/auth/auth.service';
import { PermissionService } from '../../../../core/auth/permission.service';
import { EventDetailModalComponent } from '../../components/event-detail-modal/event-detail-modal.component';
import { EventFormModalComponent } from '../../components/event-form-modal/event-form-modal.component';
import { CalendarEvent, CreateCalendarEventDto } from '../../interfaces/calendar-event.interface';
import { CalendarEventsService } from '../../services/calendar-events.service';
import {
  addDays,
  addMonths,
  endOfWeek,
  isSameDay,
  isoDate,
  monthGridWeeks,
  startOfWeek,
  weekDays,
} from '../../utils/calendar-date.util';
import { generateRecurringEvents } from '../../utils/recurring-events.util';

type ViewMode = 'week' | 'month';

/**
 * SCRUM-28 (SCRUM-186/SCRUM-187): calendario general del Centro Barrial. Vistas
 * semanal y mensual, actividades recurrentes precargadas (desayuno/almuerzo/
 * merendero), detalle de evento con autor, resaltado del día actual, y de solo
 * lectura para el rol Escucha (sin botón de alta ni de edición).
 */
@Component({
  selector: 'app-calendar',
  imports: [DatePipe, UiTabsComponent, UiButtonComponent, EventFormModalComponent, EventDetailModalComponent],
  templateUrl: './calendar.component.html',
})
export class CalendarComponent implements OnInit {
  private calendarEventsService = inject(CalendarEventsService);
  private permissionService = inject(PermissionService);
  private authService = inject(AuthService);

  viewMode = signal<ViewMode>('week');
  referenceDate = signal<Date>(new Date());

  events = signal<CalendarEvent[]>([]);
  loading = signal(false);
  errorMessage = signal<string | null>(null);

  readonly today = new Date();

  canCreate = computed(() => this.permissionService.hasPermission('calendario.create'));
  canEdit = computed(() => this.permissionService.hasPermission('calendario.edit'));

  viewTabs: UiTab<ViewMode>[] = [
    { id: 'week', label: 'Semana' },
    { id: 'month', label: 'Mes' },
  ];

  rangeStart = computed(() => {
    const ref = this.referenceDate();
    return this.viewMode() === 'week' ? startOfWeek(ref) : startOfWeek(new Date(ref.getFullYear(), ref.getMonth(), 1));
  });

  rangeEnd = computed(() => {
    const ref = this.referenceDate();
    if (this.viewMode() === 'week') {
      return endOfWeek(ref);
    }
    const lastDayOfMonth = new Date(ref.getFullYear(), ref.getMonth() + 1, 0);
    return endOfWeek(lastDayOfMonth);
  });

  rangeLabel = computed(() => {
    const ref = this.referenceDate();
    if (this.viewMode() === 'month') {
      const label = ref.toLocaleDateString('es-AR', { month: 'long', year: 'numeric' });
      return label.charAt(0).toUpperCase() + label.slice(1);
    }

    const start = this.rangeStart();
    const end = this.rangeEnd();
    const fmt = (d: Date) => d.toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit' });
    return `${fmt(start)} – ${fmt(end)}, ${end.getFullYear()}`;
  });

  // eventos reales (del backend, cuando exista) + las actividades recurrentes
  // precargadas, generadas en el frontend para el rango visible (ver AC de SCRUM-15)
  allEvents = computed<CalendarEvent[]>(() => [
    ...this.events(),
    ...generateRecurringEvents(this.rangeStart(), this.rangeEnd()),
  ]);

  weekDays = computed(() => weekDays(this.referenceDate()));
  monthWeeks = computed(() => monthGridWeeks(this.referenceDate()));

  // alta/edición (oculto para Escucha, ver canCreate/canEdit)
  showFormModal = signal(false);
  editingEvent = signal<CalendarEvent | null>(null);
  formInitialDate = signal(isoDate(new Date()));
  saving = signal(false);
  formError = signal<string | null>(null);

  // detalle del evento (SCRUM-187)
  viewingEvent = signal<CalendarEvent | null>(null);

  ngOnInit(): void {
    this.loadEvents();
  }

  isToday(date: Date): boolean {
    return isSameDay(date, this.today);
  }

  isCurrentMonth(date: Date): boolean {
    return date.getMonth() === this.referenceDate().getMonth();
  }

  eventsForDay(date: Date): CalendarEvent[] {
    return this.allEvents()
      .filter((e) => isSameDay(new Date(e.start), date))
      .sort((a, b) => a.start.localeCompare(b.start));
  }

  eventTime(event: CalendarEvent): string {
    return new Date(event.start).toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' });
  }

  // DatePipe (date: 'EEE') depende del LOCALE_ID de Angular, que en este proyecto no
  // está registrado en es-AR, así que mostraba los días en inglés (Mon, Tue...). Se
  // usa toLocaleDateString directamente, igual que el resto de los textos de fecha
  // de este componente.
  weekdayLabel(date: Date): string {
    const label = date.toLocaleDateString('es-AR', { weekday: 'short' });
    return label.replace('.', '').charAt(0).toUpperCase() + label.replace('.', '').slice(1);
  }

  changeView(mode: ViewMode): void {
    this.viewMode.set(mode);
    this.loadEvents();
  }

  goToday(): void {
    this.referenceDate.set(new Date());
    this.loadEvents();
  }

  goPrev(): void {
    this.referenceDate.set(
      this.viewMode() === 'week' ? addDays(this.referenceDate(), -7) : addMonths(this.referenceDate(), -1),
    );
    this.loadEvents();
  }

  goNext(): void {
    this.referenceDate.set(
      this.viewMode() === 'week' ? addDays(this.referenceDate(), 7) : addMonths(this.referenceDate(), 1),
    );
    this.loadEvents();
  }

  loadEvents(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.calendarEventsService.getByRange(isoDate(this.rangeStart()), isoDate(this.rangeEnd())).subscribe({
      next: (events) => {
        this.events.set(events.map((event) => this.withAuthorFallback(event)));
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.errorMessage.set('No se pudieron cargar los eventos del calendario.');
      },
    });
  }

  // el autor lo debería completar el backend a partir del usuario autenticado (ver
  // comentario en calendar-event.interface.ts). Mientras ese backend no exista, o si
  // alguna respuesta llega sin authorName, se completa con el usuario logueado para
  // que nunca se vea vacío ni con un valor genérico.
  private withAuthorFallback(event: CalendarEvent): CalendarEvent {
    if (event.authorName) {
      return event;
    }
    const user = this.authService.user();
    const fallbackName = user ? `${user.name} ${user.lastname}`.trim() : 'Usuario desconocido';
    return { ...event, authorName: fallbackName };
  }

  openCreateModal(date: Date = new Date()): void {
    this.editingEvent.set(null);
    this.formInitialDate.set(isoDate(date));
    this.formError.set(null);
    this.showFormModal.set(true);
  }

  openEditModal(event: CalendarEvent): void {
    this.editingEvent.set(event);
    this.formError.set(null);
    this.showFormModal.set(true);
  }

  closeFormModal(): void {
    this.showFormModal.set(false);
  }

  saveEvent(dto: CreateCalendarEventDto): void {
    this.saving.set(true);
    this.formError.set(null);

    const editing = this.editingEvent();
    const request$ = editing
      ? this.calendarEventsService.update(editing.id, dto)
      : this.calendarEventsService.create(dto);

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.showFormModal.set(false);
        this.loadEvents();
      },
      error: (err) => {
        this.saving.set(false);
        this.formError.set(err?.error?.message || 'No se pudo guardar el evento.');
      },
    });
  }

  openDetail(event: CalendarEvent): void {
    this.viewingEvent.set(event);
  }

  closeDetail(): void {
    this.viewingEvent.set(null);
  }

  editFromDetail(event: CalendarEvent): void {
    this.viewingEvent.set(null);
    this.openEditModal(event);
  }
}
