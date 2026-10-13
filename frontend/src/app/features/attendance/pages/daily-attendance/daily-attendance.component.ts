import { Component, DestroyRef, OnInit, computed, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { catchError, debounceTime, distinctUntilChanged, of, switchMap, tap } from 'rxjs';
import { PermissionService } from '../../../../core/auth/permission.service';
import { extractApiError } from '../../../../core/http/api-error';
import { UiButtonComponent, UiFormFieldComponent, UiInputDirective } from '../../../../shared/ui';
import { isBeforeToday, todayLocalIso } from '../../../../shared/utils/date.util';
import { SocialRecordListItem } from '../../../social-records/interfaces/social-record.interface';
import { SocialRecordsService } from '../../../social-records/services/social-records.service';
import { AttendanceJustificationModalComponent } from '../../components/justification-modal/justification-modal.component';
import {
  ATTENDANCE_ACTIVITIES,
  AttendanceActivity,
  AttendancePerson,
} from '../../interfaces/attendance.interface';
import { AttendanceService } from '../../services/attendance.service';

/**
 * SCRUM-218: pantalla de registro de asistencia diaria. Selector de actividad y fecha,
 * buscador de personas con ficha para marcar presentes, contador de personas anónimas,
 * error inline ante duplicado y modal de justificación para días cerrados.
 */
@Component({
  selector: 'app-daily-attendance',
  imports: [
    ReactiveFormsModule,
    DatePipe,
    UiButtonComponent,
    UiFormFieldComponent,
    UiInputDirective,
    AttendanceJustificationModalComponent,
  ],
  templateUrl: './daily-attendance.component.html',
})
export class DailyAttendanceComponent implements OnInit {
  private attendanceService = inject(AttendanceService);
  private socialRecordsService = inject(SocialRecordsService);
  private permissionService = inject(PermissionService);
  private destroyRef = inject(DestroyRef);

  readonly activities = ATTENDANCE_ACTIVITIES;
  readonly today = todayLocalIso();

  activity = signal<AttendanceActivity>('hogar-de-dia');
  date = signal(todayLocalIso());

  present = signal<AttendancePerson[]>([]);
  anonymousCount = signal(0);

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

  // día abierto: quien puede cargar; día cerrado: solo Referente (con justificación)
  canModify = computed(() => (this.isClosed() ? this.canEditClosed() : this.canCreate()));

  totalPresent = computed(() => this.present().length + this.anonymousCount());

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
          this.results.set(response.items.map((item) => this.toPerson(item)));
        }
      });
  }

  ngOnInit(): void {
    this.load();
  }

  private toPerson(item: SocialRecordListItem): AttendancePerson {
    return {
      personId: item.personId,
      fullName: [item.firstName, item.lastName].filter(Boolean).join(' '),
      dni: item.dni,
    };
  }

  activityLabel(): string {
    return this.activities.find((a) => a.id === this.activity())?.label ?? '';
  }

  get dateAsDate(): Date {
    const [y, m, d] = this.date().split('-').map(Number);
    return new Date(y, m - 1, d);
  }

  isMarked(person: AttendancePerson): boolean {
    return this.present().some((p) => p.personId === person.personId);
  }

  onActivityChange(value: string): void {
    this.activity.set(value as AttendanceActivity);
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

  private load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.duplicateError.set(null);

    this.attendanceService.get(this.activity(), this.date()).subscribe({
      next: (attendance) => {
        this.present.set(attendance?.persons ?? []);
        this.anonymousCount.set(attendance?.anonymousCount ?? 0);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.errorMessage.set('No se pudo cargar la asistencia.');
      },
    });
  }

  markPresent(person: AttendancePerson): void {
    if (this.isMarked(person)) {
      this.duplicateError.set(
        `${person.fullName} ya está registrada en ${this.activityLabel()} el ${this.dateAsDate.toLocaleDateString('es-AR')}.`,
      );
      return;
    }
    this.duplicateError.set(null);
    this.present.update((list) => [...list, person]);
  }

  unmark(person: AttendancePerson): void {
    this.present.update((list) => list.filter((p) => p.personId !== person.personId));
  }

  changeAnonymous(delta: number): void {
    this.setAnonymous(this.anonymousCount() + delta);
  }

  setAnonymous(value: number): void {
    const n = Number.isFinite(value) ? Math.floor(value) : 0;
    this.anonymousCount.set(Math.max(0, Math.min(n, 999)));
  }

  requestSave(): void {
    if (!this.canModify()) {
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
    this.saving.set(true);
    this.errorMessage.set(null);
    this.justificationError.set(null);

    this.attendanceService
      .save({
        activity: this.activity(),
        date: this.date(),
        persons: this.present(),
        anonymousCount: this.anonymousCount(),
        justification,
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.showJustificationModal.set(false);
          this.showSuccess('Asistencia guardada correctamente.');
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
