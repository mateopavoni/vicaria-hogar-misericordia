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
// 'combined' = SCRUM-17 (AC): "vista combinada que muestre ambos calendarios al mismo
// tiempo con colores diferenciados". 'visitas' = SCRUM-74: el calendario de la Casa de
// Convivencia, integrado como una pestaña más acá (ver comentario de la clase) en vez
// de una pantalla aparte con link propio en el sidebar.
type CalendarTab = CalendarScope | 'combined' | 'visitas';

/**
 * SCRUM-28 (SCRUM-186/SCRUM-187) + SCRUM-16 (SCRUM-191/SCRUM-192) + SCRUM-17
 * (SCRUM-196/SCRUM-197): calendario general del Centro Barrial + calendario personal.
 * Vistas semanal y mensual, actividades recurrentes precargadas (desayuno/almuerzo/
 * merendero, solo en general/combinado), alta/edición/baja de eventos propios (con
 * opción de repetirlos diario/semanal/mensual), conversión de un evento personal en
 * general, detalle con autor, resaltado del día actual, confirmación visible al
 * guardar/eliminar. El calendario PERSONAL (AC de SCRUM-17: "como referente del
 * hogar, quiero tener mi propio calendario personal") es exclusivo del rol
 * Referente — el selector General/Mi calendario/Combinado ni se muestra para los
 * demás roles, que solo ven el calendario general (Escucha, de solo lectura, ver AC
 * de SCRUM-15).
 *
 * SCRUM-74/SCRUM-213/SCRUM-214: el calendario de visitas de la Casa de Convivencia
 * (CasaConvivenciaVisitsCalendarComponent) se integra acá como pestaña "Visitas Casa de Convivencia" —
 * exclusiva de quien tenga el permiso visitas.view (Referente/DirectoraDeCasona/
 * CoordinadorDeCasaConvivencia) — en vez de tener ruta y link de sidebar propios. Es
 * un calendario con datos, permisos y vistas (día/semana/mes) totalmente distintos al
 * general/personal, así que no comparte lógica interna con este componente: solo se
 * monta su página completa dentro de esta pestaña (ver template).
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

  // permiso de rol crudo (Referente/DirectoraDeCasona hoy). SCRUM-16 (AC) agrega una
  // regla MÁS estricta por evento encima de esto — ver canManageEvent().
  private hasEditPermission = computed(() => this.permissionService.hasPermission('calendario.edit'));
  private isReferente = computed(() => this.authService.user()?.role === 'Referente');

  // SCRUM-17 (AC): el calendario personal es exclusivo de Referente (ver comentario de
  // la clase). Expuesto público porque el template lo usa para mostrar/ocultar el
  // selector de pestañas General/Mi calendario/Combinado.
  hasPersonalCalendar = computed(() => this.isReferente());

  // SCRUM-74 (AC): la pestaña "Visitas Casa de Convivencia" se muestra con el mismo permiso que
  // guardaba la ruta propia que tenía antes (visitas.view) — Referente,
  // DirectoraDeCasona y CoordinadorDeCasaConvivencia; Escucha no la ve.
  hasVisitsCalendar = computed(() => this.permissionService.hasPermission('visitas.view'));

  viewTabs: UiTab<ViewMode>[] = [
    { id: 'week', label: 'Semana' },
    { id: 'month', label: 'Mes' },
  ];

  // SCRUM-196 (AC): alternar entre "Calendario general", "Mi calendario" y una vista
  // combinada, más "Visitas Casa de Convivencia" (SCRUM-74) para quien tenga ese permiso. La fila
  // de pestañas entera solo se muestra si hay al menos una pestaña extra por mostrar
  // (ver hasExtraTabs() y calendar.component.html) — quien no tiene ninguno de los dos
  // permisos solo ve el calendario general, sin selector.
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

  // eventos reales (del backend, cuando exista) + las actividades recurrentes
  // precargadas, generadas en el frontend para el rango visible (ver AC de SCRUM-15).
  // Las recurrentes son siempre institucionales/generales — no aparecen en "Mi
  // calendario" (scope 'personal').
  allEvents = computed<CalendarEvent[]>(() => {
    // las actividades precargadas (desayuno, almuerzo...) ya vienen del backend (plantillas del seed)
    const recurring: CalendarEvent[] = [];
    return [...this.events(), ...recurring];
  });

  // SCRUM-17 (AC): mismo formulario que el general, pero solo se puede dar de alta
  // desde "General" (si hay permiso) o "Mi calendario" (si hay calendario personal).
  // Desde "Combinado" no se da de alta: no hay forma no ambigua de saber a qué
  // calendario pertenecería el evento nuevo. Desde "Visitas Casa de Convivencia" tampoco: esa
  // pestaña tiene su propio botón "Nueva visita", lo gestiona CasaConvivenciaVisitsCalendarComponent.
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

  // alta (ver canCreateHere, según la pestaña activa); edición/baja/conversión se
  // evalúan por evento, ver canManageEvent()/canConvertToGeneral()
  showFormModal = signal(false);
  editingEvent = signal<CalendarEvent | null>(null);
  formInitialDate = signal(isoDate(new Date()));
  saving = signal(false);
  formError = signal<string | null>(null);

  // detalle del evento (SCRUM-187)
  viewingEvent = signal<CalendarEvent | null>(null);

  // eliminación (SCRUM-16/SCRUM-192): confirmación previa obligatoria. Un error acá
  // se muestra en el mismo banner de errorMessage que usa la carga del calendario.
  deletingEvent = signal<CalendarEvent | null>(null);
  deleting = signal(false);

  // SCRUM-192 (AC): mensaje de confirmación visible al crear/editar/eliminar, sin
  // recargar la página — se limpia solo a los pocos segundos.
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

  // Regla pedida: ningún evento de calendario puede quedar con fecha anterior a hoy —
  // acá se usa para no ofrecer "crear evento" al clickear un día ya pasado en la
  // grilla (el bloqueo real está en el formulario, ver event-form-modal; esto es solo
  // para no abrir el modal con una fecha que de entrada ya va a rechazar).
  isPastDay(date: Date): boolean {
    return isBeforeToday(isoDate(date));
  }

  eventsForDay(date: Date): CalendarEvent[] {
    return this.allEvents()
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

  // SCRUM-16 (AC): "el autor del evento puede editarlo o eliminarlo; cualquier
  // Referente también puede hacerlo". Las actividades recurrentes precargadas
  // (isRecurring) nunca son editables, las creó el sistema, no un usuario.
  // SCRUM-17 (AC): el calendario personal es privado — ahí la regla es más estricta,
  // solo el autor lo administra (ni siquiera otro Referente puede tocar el evento
  // personal de otra persona, porque ni siquiera debería poder verlo).
  canManageEvent(event: CalendarEvent): boolean {
    if (event.isRecurring) {
      return false;
    }
    if (event.scope === 'personal') {
      return this.isAuthor(event);
    }
    return this.hasEditPermission() && (this.isReferente() || this.isAuthor(event));
  }

  // SCRUM-17/SCRUM-197 (AC): "el usuario puede convertir un evento personal en
  // general si lo decide posteriormente" — solo el autor, y solo si además podría
  // haber creado un evento general directamente (mismo permiso que "Nuevo evento" en
  // la pestaña General).
  canConvertToGeneral(event: CalendarEvent): boolean {
    return !event.isRecurring && event.scope === 'personal' && this.isAuthor(event) && this.canCreateGeneral();
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

  changeScope(tab: CalendarTab): void {
    this.calendarTab.set(tab);
    // "Visitas Casa de Convivencia" no usa events/loadEvents de este componente — tiene su propia
    // carga de datos adentro de CasaConvivenciaVisitsCalendarComponent (ver template).
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
    // "Visitas Casa de Convivencia" no tiene CalendarEvent/scope propios — los carga
    // CasaConvivenciaVisitsCalendarComponent. No debería llegar acá (changeScope/ngOnInit lo
    // evitan), pero se corta explícito para que el tipo de tab quede acotado a
    // CalendarScope|'combined' debajo, igual que antes de agregar esa pestaña.
    if (tab === 'visitas') {
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);

    const desde = isoDate(this.rangeStart());
    const hasta = isoDate(this.rangeEnd());

    // SCRUM-196 (AC): vista combinada = ambos calendarios a la vez. Se piden los dos
    // rangos en paralelo y se combinan acá — el backend sigue filtrando 'personal' por
    // el usuario autenticado (ver comentario en el service), el frontend no filtra nada.
    const request$: Observable<CalendarEvent[]> =
      tab === 'combined'
        ? forkJoin([
            this.calendarEventsService.getByRange(desde, hasta, 'general'),
            this.calendarEventsService.getByRange(desde, hasta, 'personal'),
          ]).pipe(map(([general, personal]) => [...general, ...personal]))
        : this.calendarEventsService.getByRange(desde, hasta, tab);

    request$.subscribe({
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
  // alguna respuesta llega sin authorName/authorId, se completa con el usuario
  // logueado para que nunca se vea vacío ni con un valor genérico. LIMITACIÓN: esto
  // hace que todo evento sin authorId "parezca" propio de quien esté mirando el
  // calendario en ese momento — no reemplaza probar la regla de autorización del AC
  // de SCRUM-16 contra un backend real.
  private withAuthorFallback(event: CalendarEvent): CalendarEvent {
    if (event.authorName && event.authorId && event.scope) {
      return event;
    }
    const user = this.authService.user();
    const fallbackName = user ? `${user.name} ${user.lastname}`.trim() : 'Usuario desconocido';
    return {
      ...event,
      authorName: event.authorName || fallbackName,
      authorId: event.authorId || user?.id || '',
      scope: event.scope || 'general',
    };
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
    // scope: al editar se conserva el del evento original (no es un campo del
    // formulario, ver EventFormValue); al crear, lo decide la pestaña activa. Si por
    // algún motivo se llegara a disparar desde "Combinado" (el botón está oculto ahí,
    // ver canCreateHere), se cae a 'general' como default seguro.
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

  // SCRUM-16/SCRUM-192 (AC): confirmación previa antes de eliminar un evento.
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

    this.calendarEventsService.delete(event.id).subscribe({
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

  // SCRUM-17/SCRUM-197 (AC): convertir un evento personal en general. Sin
  // confirmación previa — el AC solo la pide para eliminar, no para esto.
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
