import { Component, DestroyRef, OnInit, computed, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { catchError, debounceTime, distinctUntilChanged, forkJoin, of, switchMap, tap } from 'rxjs';
import { PermissionService } from '../../../../core/auth/permission.service';
import { extractApiError } from '../../../../core/http/api-error';
import { UiButtonComponent, UiFormFieldComponent, UiInputDirective, UiTab, UiTabsComponent } from '../../../../shared/ui';
import { isBeforeToday, todayLocalIso } from '../../../../shared/utils/date.util';
import { PersonType, SocialRecordListItem } from '../../../social-records/interfaces/social-record.interface';
import { SocialRecordsService } from '../../../social-records/services/social-records.service';
import { AttendanceJustificationModalComponent } from '../../components/justification-modal/justification-modal.component';
import {
  AREA_LABELS,
  AttendanceActivity,
  AttendanceArea,
  AttendancePerson,
  CASA_CONVIVENCIA_ID,
  HOGAR_DE_DIA_ID,
} from '../../interfaces/attendance.interface';
import { AttendanceActivitiesService } from '../../services/attendance-activities.service';
import { AttendanceService } from '../../services/attendance.service';

/** Una persona marcada y las actividades del día en las que estuvo (SCRUM-230). */
interface AttendanceRow {
  person: AttendancePerson;
  activityIds: AttendanceActivity[];
}

/**
 * SCRUM-218 + SCRUM-230: registro de asistencia diaria. Dos áreas separadas (Hogar de día /
 * Casa de Convivencia diaria). En Hogar de día se eligen una o más actividades y una grilla
 * persona × actividad permite marcar a la misma persona en varias actividades del mismo día.
 * Buscador de personas con ficha, contador de personas sin ficha por actividad, error inline
 * ante duplicado y modal de justificación para días cerrados.
 */
@Component({
  selector: 'app-daily-attendance',
  imports: [
    ReactiveFormsModule,
    DatePipe,
    RouterLink,
    UiButtonComponent,
    UiFormFieldComponent,
    UiInputDirective,
    UiTabsComponent,
    AttendanceJustificationModalComponent,
  ],
  templateUrl: './daily-attendance.component.html',
})
export class DailyAttendanceComponent implements OnInit {
  private attendanceService = inject(AttendanceService);
  private activitiesService = inject(AttendanceActivitiesService);
  private socialRecordsService = inject(SocialRecordsService);
  private permissionService = inject(PermissionService);
  private destroyRef = inject(DestroyRef);

  readonly today = todayLocalIso();

  readonly areaTabs: UiTab<AttendanceArea>[] = [
    { id: 'hogar-de-dia', label: AREA_LABELS['hogar-de-dia'] },
    { id: 'casa-convivencia', label: AREA_LABELS['casa-convivencia'] },
  ];

  area = signal<AttendanceArea>('hogar-de-dia');
  date = signal(todayLocalIso());

  // actividades del Hogar de día elegidas para registrar (selección múltiple)
  selectedIds = signal<AttendanceActivity[]>([HOGAR_DE_DIA_ID]);

  rows = signal<AttendanceRow[]>([]);
  anonymous = signal<Record<AttendanceActivity, number>>({});
  // actividades que ya tenían un registro guardado en la fecha cargada
  private existing = new Set<AttendanceActivity>();

  loading = signal(false);
  saving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  private successTimeout: ReturnType<typeof setTimeout> | null = null;

  // buscador de personas con ficha
  searchControl = new FormControl('', { nonNullable: true });
  results = signal<AttendancePerson[]>([]);
  searching = signal(false);
  searchError = signal<string | null>(null);
  // error inline ante duplicado (AC): misma persona, misma actividad y fecha
  duplicateError = signal<string | null>(null);

  showJustificationModal = signal(false);
  justificationError = signal<string | null>(null);

  // un día es "cerrado" cuando su fecha es anterior a hoy
  isClosed = computed(() => isBeforeToday(this.date()));

  canCreate = computed(() => this.permissionService.hasPermission('asistencia.create'));
  canEditClosed = computed(() => this.permissionService.hasPermission('asistencia.edit'));
  canConfigure = computed(() => this.permissionService.hasPermission('asistencia.config'));

  // día abierto: quien puede cargar; día cerrado: solo Referente (con justificación)
  canModify = computed(() => (this.isClosed() ? this.canEditClosed() : this.canCreate()));

  // actividades que se pueden elegir en el área Hogar de día (solo activas)
  hogarOptions = computed(() => this.activitiesService.active().filter((a) => a.area === 'hogar-de-dia'));

  // columnas de la grilla: la actividad fija de la Casa, o las elegidas del Hogar de día
  columns = computed(() => {
    const all = this.activitiesService.all();
    if (this.area() === 'casa-convivencia') {
      return all.filter((a) => a.id === CASA_CONVIVENCIA_ID);
    }
    return all.filter((a) => a.area === 'hogar-de-dia' && this.selectedIds().includes(a.id));
  });

  columnIds = computed(() => this.columns().map((c) => c.id));

  constructor() {
    // [disabled] en un input con [formControl] no deshabilita el control: se hace por código
    effect(() => {
      if (this.canModify()) {
        this.searchControl.enable({ emitEvent: false });
      } else {
        this.searchControl.disable({ emitEvent: false });
      }
    });

    this.searchControl.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        tap(() => this.duplicateError.set(null)),
        switchMap((term) => {
          const search = term.trim();
          if (search.length < 2) {
            this.results.set([]);
            this.searching.set(false);
            this.searchError.set(null);
            return of(null);
          }
          this.searching.set(true);
          this.searchError.set(null);
          return this.socialRecordsService.getAll(1, search).pipe(
            catchError(() => {
              this.searchError.set('No se pudo buscar personas.');
              return of(null);
            }),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((response) => {
        this.searching.set(false);
        if (response) {
          // la Casa de Convivencia registra solo a sus residentes
          const onlyResidents = this.area() === 'casa-convivencia';
          this.results.set(
            response.items.filter((item) => !onlyResidents || item.personType === PersonType.Resident).map((item) => this.toPerson(item)),
          );
        }
      });
  }

  ngOnInit(): void {
    // las recurrentes del calendario se suman solas al catálogo; si el calendario falla se sigue con el catálogo actual
    this.activitiesService.syncFromCalendar().subscribe({ error: () => undefined });
    this.load();
  }

  private toPerson(item: SocialRecordListItem): AttendancePerson {
    return {
      personId: item.personId,
      fullName: [item.firstName, item.lastName].filter(Boolean).join(' '),
      dni: item.dni,
    };
  }

  get dateAsDate(): Date {
    const [y, m, d] = this.date().split('-').map(Number);
    return new Date(y, m - 1, d);
  }

  areaLabel(): string {
    return AREA_LABELS[this.area()];
  }

  isSelected(id: AttendanceActivity): boolean {
    return this.selectedIds().includes(id);
  }

  isChecked(row: AttendanceRow, id: AttendanceActivity): boolean {
    return row.activityIds.includes(id);
  }

  isMarked(person: AttendancePerson): boolean {
    const cols = this.columnIds();
    const row = this.rows().find((r) => r.person.personId === person.personId);
    return !!row && cols.length > 0 && cols.every((id) => row.activityIds.includes(id));
  }

  countFor(id: AttendanceActivity): number {
    return this.rows().filter((r) => r.activityIds.includes(id)).length + (this.anonymous()[id] ?? 0);
  }

  totalPresent = computed(() => {
    // personas distintas con alguna actividad visible + sin ficha de todas las columnas
    const cols = this.columnIds();
    const withFile = this.rows().filter((r) => cols.some((id) => r.activityIds.includes(id))).length;
    const without = cols.reduce((sum, id) => sum + (this.anonymous()[id] ?? 0), 0);
    return withFile + without;
  });

  onAreaChange(area: AttendanceArea): void {
    if (area === this.area()) {
      return;
    }
    this.area.set(area);
    this.searchControl.setValue('');
    this.results.set([]);
    this.load();
  }

  onDateChange(value: string): void {
    // el input permite borrar la fecha; se ignora hasta que haya una válida
    if (!value || value > this.today) {
      return;
    }
    this.date.set(value);
    this.load();
  }

  toggleActivity(id: AttendanceActivity): void {
    const current = this.selectedIds();
    if (current.includes(id)) {
      // tiene que quedar al menos una actividad elegida
      if (current.length > 1) {
        this.selectedIds.set(current.filter((x) => x !== id));
      }
    } else {
      this.selectedIds.set([...current, id]);
    }
    this.duplicateError.set(null);
  }

  private load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.duplicateError.set(null);

    // se cargan todas las actividades del área (también las desactivadas), para no perder
    // lo ya registrado en ellas
    const defs = this.activitiesService.all().filter((a) => a.area === this.area());

    forkJoin(defs.map((a) => this.attendanceService.get(a.id, this.date()))).subscribe({
      next: (records) => {
        const byPerson = new Map<string, AttendanceRow>();
        const anonymous: Record<AttendanceActivity, number> = {};
        this.existing = new Set();

        records.forEach((record, i) => {
          if (!record) {
            return;
          }
          this.existing.add(defs[i].id);
          anonymous[defs[i].id] = record.anonymousCount;
          for (const person of record.persons) {
            const row = byPerson.get(person.personId) ?? { person, activityIds: [] };
            row.activityIds.push(defs[i].id);
            byPerson.set(person.personId, row);
          }
        });

        this.rows.set([...byPerson.values()]);
        this.anonymous.set(anonymous);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.errorMessage.set('No se pudo cargar la asistencia.');
      },
    });
  }

  markPresent(person: AttendancePerson): void {
    const cols = this.columnIds();
    if (cols.length === 0) {
      return;
    }

    const existing = this.rows().find((r) => r.person.personId === person.personId);
    if (!existing) {
      this.duplicateError.set(null);
      this.rows.update((list) => [...list, { person, activityIds: [...cols] }]);
      return;
    }

    const missing = cols.filter((id) => !existing.activityIds.includes(id));
    if (missing.length === 0) {
      this.duplicateError.set(
        `${person.fullName} ya está registrada en ${this.columns().map((c) => c.name).join(', ')} el ${this.dateAsDate.toLocaleDateString('es-AR')}.`,
      );
      return;
    }

    // ya estaba en otra actividad del día: se le suman las que faltaban
    this.duplicateError.set(null);
    this.rows.update((list) =>
      list.map((r) => (r.person.personId === person.personId ? { ...r, activityIds: [...r.activityIds, ...missing] } : r)),
    );
  }

  toggleCell(row: AttendanceRow, id: AttendanceActivity): void {
    this.duplicateError.set(null);
    this.rows.update((list) =>
      list.map((r) => {
        if (r.person.personId !== row.person.personId) {
          return r;
        }
        return {
          ...r,
          activityIds: r.activityIds.includes(id) ? r.activityIds.filter((x) => x !== id) : [...r.activityIds, id],
        };
      }),
    );
  }

  unmark(row: AttendanceRow): void {
    const cols = this.columnIds();
    // se quita de las actividades visibles; las no visibles (otras actividades del día) quedan como estaban
    this.rows.update((list) =>
      list
        .map((r) =>
          r.person.personId === row.person.personId ? { ...r, activityIds: r.activityIds.filter((id) => !cols.includes(id)) } : r,
        )
        .filter((r) => r.activityIds.length > 0),
    );
  }

  setAnonymous(id: AttendanceActivity, value: number): void {
    const n = Number.isFinite(value) ? Math.floor(value) : 0;
    this.anonymous.update((map) => ({ ...map, [id]: Math.max(0, Math.min(n, 999)) }));
  }

  changeAnonymous(id: AttendanceActivity, delta: number): void {
    this.setAnonymous(id, (this.anonymous()[id] ?? 0) + delta);
  }

  requestSave(): void {
    if (!this.canModify() || this.columnIds().length === 0) {
      return;
    }
    if (this.isClosed()) {
      this.justificationError.set(null);
      this.showJustificationModal.set(true);
      return;
    }
    this.save(null);
  }

  closeJustificationModal(): void {
    this.showJustificationModal.set(false);
  }

  save(justification: string | null): void {
    // un registro por actividad visible; las vacías que nunca se habían guardado se omiten
    const requests = this.columnIds()
      .map((id) => ({
        activity: id,
        date: this.date(),
        persons: this.rows()
          .filter((r) => r.activityIds.includes(id))
          .map((r) => r.person),
        anonymousCount: this.anonymous()[id] ?? 0,
        justification,
      }))
      .filter((r) => r.persons.length > 0 || r.anonymousCount > 0 || this.existing.has(r.activity));

    if (requests.length === 0) {
      this.errorMessage.set('No hay nada para guardar: marcá al menos una persona o un conteo sin ficha.');
      return;
    }

    this.saving.set(true);
    this.errorMessage.set(null);
    this.justificationError.set(null);

    forkJoin(requests.map((request) => this.attendanceService.save(request))).subscribe({
      next: () => {
        requests.forEach((r) => this.existing.add(r.activity));
        this.saving.set(false);
        this.showJustificationModal.set(false);
        this.showSuccess(
          requests.length > 1 ? `Asistencia guardada en ${requests.length} actividades.` : 'Asistencia guardada correctamente.',
        );
      },
      error: (err) => {
        this.saving.set(false);
        const message = extractApiError(err, 'No se pudo guardar la asistencia.');
        if (this.showJustificationModal()) {
          this.justificationError.set(message);
        } else {
          this.errorMessage.set(message);
        }
      },
    });
  }

  private showSuccess(message: string): void {
    this.successMessage.set(message);
    if (this.successTimeout) {
      clearTimeout(this.successTimeout);
    }
    this.successTimeout = setTimeout(() => this.successMessage.set(null), 4000);
  }
}
