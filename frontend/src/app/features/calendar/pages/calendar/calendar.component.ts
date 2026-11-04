import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { forkJoin, map, Observable } from 'rxjs';
import { UiButtonComponent, UiConfirmDialogComponent, UiTab, UiTabsComponent } from '../../../../shared/ui';
import { AuthService } from '../../../../core/auth/auth.service';
import { PermissionService } from '../../../../core/auth/permission.service';
import { EventDetailModalComponent } from '../../components/event-detail-modal/event-detail-modal.component';
import { EventFormModalComponent } from '../../components/event-form-modal/event-form-modal.component';
import { CalendarEvent, CalendarScope, EventFormValue } from '../../interfaces/calendar-event.interface';
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
import { isBeforeToday } from '../../../../shared/utils/date.util';
import { CasaConvivenciaVisitsCalendarComponent } from '../../../casa-convivencia-visits/pages/casa-convivencia-visits-calendar/casa-convivencia-visits-calendar.component';

type ViewMode = 'week' | 'month';
// 'combined' muestra el general y el personal juntos; 'visitas' es el calendario
// de visitas de la Casa de Convivencia, que se muestra como una pestaña más.
type CalendarTab = CalendarScope | 'combined' | 'visitas';

/**
 * Calendario general del Centro Barrial y calendario personal, en vista semanal o mensual.
 * El calendario personal es solo para el rol Referente; los demás roles ven el general.
 * La pestaña de visitas monta CasaConvivenciaVisitsCalendarComponent, que tiene su propia lógica.
 */
@Component({
  selector: 'app-calendar',
  imports: [
    DatePipe,
    UiTabsComponent,
    UiButtonComponent,
    UiConfirmDialogComponent,
    EventFormModalComponent,
    EventDetailModalComponent,
    CasaConvivenciaVisitsCalendarComponent,
  ],
  templateUrl: './calendar.component.html',
})
export class CalendarComponent implements OnInit, OnDestroy {
  private calendarEventsService = inject(CalendarEventsService);
  private permissionService = inject(PermissionService);
  private authService = inject(AuthService);

  viewMode = signal<ViewMode>('week');
  referenceDate = signal<Date>(new Date());

  events = signal<CalendarEvent[]>([]);
  loading = signal(false);
  errorMessage = signal<string | null>(null);

  readonly today = new Date();

  canCreateGeneral = computed(() => this.permissionService.hasPermission('calendario.create'));

  // permiso general de edición; canManageEvent() agrega la regla por evento
  private hasEditPermission = computed(() => this.permissionService.hasPermission('calendario.edit'));
  private isReferente = computed(() => this.authService.user()?.role === 'Referente');

  // el calendario personal es exclusivo de Referente
  hasPersonalCalendar = computed(() => this.isReferente());

  // la pestaña de visitas la ven quienes tienen el permiso visitas.view (todos menos Escucha)
  hasVisitsCalendar = computed(() => this.permissionService.hasPermission('visitas.view'));

  viewTabs: UiTab<ViewMode>[] = [
    { id: 'week', label: 'Semana' },
    { id: 'month', label: 'Mes' },
  ];

  // pestañas disponibles según el rol; si solo queda "General" no se muestra el selector
  calendarTab = signal<CalendarTab>('general');
  scopeTabs = computed<UiTab<CalendarTab>[]>(() => {
    const tabs: UiTab<CalendarTab>[] = [{ id: 'general', label: 'General' }];
    if (this.hasPersonalCalendar()) {
      tabs.push({ id: 'personal', label: 'Mi calendario' }, { id: 'combined', label: 'Combinado' });
    }
    if (this.hasVisitsCalendar()) {
      tabs.push({ id: 'visitas', label: 'Casa de Convivencia' });
    }
    return tabs;
  });
  hasExtraTabs = computed(() => this.hasPersonalCalendar() || this.hasVisitsCalendar());

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

  // solo se crean eventos desde "General" o "Mi calendario"; en "Combinado" no se sabe
  // a qué calendario iría y la pestaña de visitas tiene su propio botón
  canCreateHere = computed(() => {
    switch (this.calendarTab()) {
      case 'general':
        return this.canCreateGeneral();
      case 'personal':
        return this.hasPersonalCalendar();
      default:
        return false;
    }
  });

  weekDays = computed(() => weekDays(this.referenceDate()));
  monthWeeks = computed(() => monthGridWeeks(this.referenceDate()));

  // formulario de alta y edición
  showFormModal = signal(false);
  editingEvent = signal<CalendarEvent | null>(null);
  formInitialDate = signal(isoDate(new Date()));
  saving = signal(false);
  formError = signal<string | null>(null);

  // detalle del evento
  viewingEvent = signal<CalendarEvent | null>(null);

  // eliminación, siempre con confirmación previa
  deletingEvent = signal<CalendarEvent | null>(null);
  deleting = signal(false);

  // mensaje de éxito que se oculta solo a los pocos segundos
  successMessage = signal<string | null>(null);
  private successTimeout: ReturnType<typeof setTimeout> | null = null;

  ngOnInit(): void {
    this.loadEvents();
  }

  ngOnDestroy(): void {
    if (this.successTimeout) {
      clearTimeout(this.successTimeout);
    }
  }

  private showSuccess(message: string): void {
    this.successMessage.set(message);
    if (this.successTimeout) {
      clearTimeout(this.successTimeout);
    }
    this.successTimeout = setTimeout(() => this.successMessage.set(null), 4000);
  }

  isToday(date: Date): boolean {
    return isSameDay(date, this.today);
  }

  isCurrentMonth(date: Date): boolean {
    return date.getMonth() === this.referenceDate().getMonth();
  }

  // no se ofrece crear eventos en días pasados (el formulario también lo valida)
  isPastDay(date: Date): boolean {
    return isBeforeToday(isoDate(date));
  }

  eventsForDay(date: Date): CalendarEvent[] {
    return this.events()
      .filter((e) => isSameDay(new Date(e.start), date))
      .sort((a, b) => a.start.localeCompare(b.start));
  }

  eventTime(event: CalendarEvent): string {
    return new Date(event.start).toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' });
  }

  private isAuthor(event: CalendarEvent): boolean {
    const currentUserId = this.authService.user()?.id;
    return !!currentUserId && currentUserId === event.authorId;
  }

  // un evento general lo edita su autor o un Referente; uno personal, solo su autor.
  // Las actividades precargadas del sistema no se editan.
  canManageEvent(event: CalendarEvent): boolean {
    if (event.isRecurring) {
      return false;
    }
    if (event.scope === 'personal') {
      return this.isAuthor(event);
    }
    return this.hasEditPermission() && (this.isReferente() || this.isAuthor(event));
  }

  // el autor puede pasar un evento personal al calendario general si tiene permiso de crear eventos generales
  canConvertToGeneral(event: CalendarEvent): boolean {
    return !event.isRecurring && event.scope === 'personal' && this.isAuthor(event) && this.canCreateGeneral();
  }

  // nombre corto del día en español (DatePipe lo mostraría en inglés)
  weekdayLabel(date: Date): string {
    const label = date.toLocaleDateString('es-AR', { weekday: 'short' });
    return label.replace('.', '').charAt(0).toUpperCase() + label.replace('.', '').slice(1);
  }

  changeView(mode: ViewMode): void {
    this.viewMode.set(mode);
    this.loadEvents();
  }

  changeScope(tab: CalendarTab): void {
    this.calendarTab.set(tab);
    // la pestaña de visitas carga sus propios datos
    if (tab !== 'visitas') {
      this.loadEvents();
    }
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
    const tab = this.calendarTab();
    // la pestaña de visitas carga sus propios datos
    if (tab === 'visitas') {
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);

    const desde = isoDate(this.rangeStart());
    const hasta = isoDate(this.rangeEnd());

    // en la vista combinada se piden los dos calendarios en paralelo y se juntan
    const request$: Observable<CalendarEvent[]> =
      tab === 'combined'
        ? forkJoin([
            this.calendarEventsService.getByRange(desde, hasta, 'general'),
            this.calendarEventsService.getByRange(desde, hasta, 'personal'),
          ]).pipe(map(([general, personal]) => [...general, ...personal]))
        : this.calendarEventsService.getByRange(desde, hasta, tab);

    request$.subscribe({
      next: (events) => {
        this.events.set(events.map((event) => this.withAuthorName(event)));
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.errorMessage.set('No se pudieron cargar los eventos del calendario.');
      },
    });
  }

  // los eventos personales llegan sin nombre de autor porque siempre son del usuario logueado
  private withAuthorName(event: CalendarEvent): CalendarEvent {
    if (event.authorName) {
      return event;
    }
    const user = this.authService.user();
    return { ...event, authorName: user ? `${user.name} ${user.lastname}`.trim() : '' };
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

  saveEvent(formValue: EventFormValue): void {
    this.saving.set(true);
    this.formError.set(null);

    const editing = this.editingEvent();
    // al editar se mantiene el calendario del evento; al crear lo define la pestaña activa
    const scope = editing ? editing.scope : this.calendarTab() === 'personal' ? 'personal' : 'general';
    const dto = { ...formValue, scope };

    const request$ = editing
      ? this.calendarEventsService.update(editing.id, dto)
      : this.calendarEventsService.create(dto);

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.showFormModal.set(false);
        this.showSuccess(editing ? 'Evento actualizado correctamente.' : 'Evento creado correctamente.');
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

  // Confirmación previa antes de eliminar un evento.
  requestDelete(event: CalendarEvent): void {
    this.viewingEvent.set(null);
    this.errorMessage.set(null);
    this.deletingEvent.set(event);
  }

  cancelDelete(): void {
    this.deletingEvent.set(null);
  }

  confirmDelete(): void {
    const event = this.deletingEvent();
    if (!event) {
      return;
    }

    this.deleting.set(true);

    this.calendarEventsService.delete(event.id, event.scope).subscribe({
      next: () => {
        this.deleting.set(false);
        this.deletingEvent.set(null);
        this.showSuccess('Evento eliminado correctamente.');
        this.loadEvents();
      },
      error: (err) => {
        this.deleting.set(false);
        this.deletingEvent.set(null);
        this.errorMessage.set(err?.error?.message || 'No se pudo eliminar el evento.');
      },
    });
  }

  // pasa un evento personal al calendario general
  requestConvert(event: CalendarEvent): void {
    this.viewingEvent.set(null);
    this.errorMessage.set(null);

    this.calendarEventsService.convertToGeneral(event.id).subscribe({
      next: () => {
        this.showSuccess('Evento convertido a general: ahora lo ve todo el equipo.');
        this.loadEvents();
      },
      error: (err) => {
        this.errorMessage.set(err?.error?.message || 'No se pudo convertir el evento a general.');
      },
    });
  }
}
