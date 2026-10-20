import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map, of, switchMap, throwError } from 'rxjs';
import { todayLocalIso } from '../../../shared/utils/date.util';
import { isEvaluationExpired } from '../utils/scheme-review.util';
import {
  AdministrationStatus,
  AgendaItem,
  AgendaTimeSlot,
  CreateMedicationSchemeRequest,
  MedicationAdministration,
  MedicationAgenda,
  MedicationCatalogItem,
  MedicationHistoryEntry,
  MedicationScheme,
  PsychiatricEvaluationStatus,
  RecordAdministrationRequest,
} from '../interfaces/medication.interface';

const TIME_PATTERN = /^([01]\d|2[0-3]):[0-5]\d$/;

/** MedicationIntakeStatus del backend (serializado como número). */
const INTAKE_PENDING = 1;
const INTAKE_ADMINISTERED = 2;
const INTAKE_NOT_ADMINISTERED = 3;

interface SchemeDto extends Omit<MedicationScheme, 'active'> {
  isActive: boolean;
}

interface AgendaIntakeDto {
  id: string;
  personId: string;
  personFullName: string;
  medicationName: string;
  dose: string;
  status: number;
  observation: string | null;
  administeredAt: string | null;
  administeredByUserId: string | null;
}

interface AgendaDto {
  date: string;
  groups: { time: string; intakes: AgendaIntakeDto[] }[];
}

/**
 * SCRUM-234 / 238 / 242 / 250: esquemas, agenda diaria e historial de medicación.
 * Usa /api/persons/{id}/medication-schemes, /api/medicamentos/agenda y
 * /api/persons/{id}/medication-history.
 *
 * - La evaluación psiquiátrica vigente la valida el backend al cargar un esquema (devuelve 400
 *   con el motivo); `getEvaluationStatus` solo anticipa el aviso en pantalla.
 * - La agenda la genera el backend al registrar la asistencia de cada persona; cada toma tiene
 *   su propio id (`intakeId`), que es el que se usa para registrar la administración.
 * - El descuento de stock lo hace el backend al marcar una toma como administrada.
 */
@Injectable({ providedIn: 'root' })
export class MedicationService {
  private readonly http = inject(HttpClient);

  getCatalog(): Observable<MedicationCatalogItem[]> {
    return this.http.get<MedicationCatalogItem[]>('/api/medication-schemes/catalog');
  }

  /**
   * `evaluatedAt` (YYYY-MM-DD) es la fecha de la última evaluación psiquiátrica, cuando el
   * perfil la tenga; con fecha, pasados 6 meses vence. Es solo orientativo: el backend decide.
   */
  getEvaluationStatus(_personId: string, isResident: boolean, evaluatedAt: string | null = null): Observable<PsychiatricEvaluationStatus> {
    const expired = !!evaluatedAt && isEvaluationExpired(evaluatedAt, todayLocalIso());
    return of({ valid: isResident && !expired, evaluatedAt, expired });
  }

  getSchemes(personId: string): Observable<MedicationScheme[]> {
    return this.http
      .get<SchemeDto[]>(`/api/persons/${personId}/medication-schemes`)
      .pipe(map((dtos) => dtos.map(toScheme)));
  }

  create(request: CreateMedicationSchemeRequest): Observable<MedicationScheme> {
    if (!request.dose.trim()) {
      return this.fail('La dosis es obligatoria.');
    }
    if (!TIME_PATTERN.test(request.time)) {
      return this.fail('La hora no es válida.');
    }
    const body = { medicationId: request.medicationId, dose: request.dose.trim(), time: request.time };
    return this.http
      .post<SchemeDto>(`/api/persons/${request.personId}/medication-schemes`, body)
      .pipe(map(toScheme));
  }

  /** SCRUM-250: "Marcar como revisado" — el backend registra revisado_en y revisado_por. Solo esquemas vigentes. */
  markReviewed(personId: string, schemeId: string): Observable<MedicationScheme> {
    return this.http
      .patch<void>(`/api/persons/${personId}/medication-schemes/${schemeId}/review`, {})
      .pipe(switchMap(() => this.schemeById(personId, schemeId)));
  }

  /** Pasa un esquema vigente a histórico. El backend no permite volver a activarlo. */
  deactivate(personId: string, schemeId: string): Observable<MedicationScheme> {
    return this.http
      .patch<void>(`/api/persons/${personId}/medication-schemes/${schemeId}/deactivate`, {})
      .pipe(switchMap(() => this.schemeById(personId, schemeId)));
  }

  // ───────── SCRUM-238: agenda diaria ─────────

  /**
   * Agenda del día agrupada por hora. El backend la arma con las personas que asistieron ese día
   * y tienen esquemas vigentes con stock.
   */
  getAgenda(date: string): Observable<MedicationAgenda> {
    const params = new HttpParams().set('fecha', date);
    return this.http.get<AgendaDto>('/api/medicamentos/agenda', { params }).pipe(
      map((dto) => {
        const slots: AgendaTimeSlot[] = dto.groups.map((group) => ({
          time: group.time,
          items: group.intakes.map((intake) => toAgendaItem(intake, group.time)),
        }));
        return { date, slots, pendingStockCount: 0 };
      }),
    );
  }

  /** Marca una toma como administrada / no administrada. Solo del día actual; se puede corregir. */
  recordAdministration(request: RecordAdministrationRequest): Observable<MedicationAdministration> {
    if (request.date !== todayLocalIso()) {
      return this.fail('Solo se puede registrar la administración del día actual.');
    }
    if (request.status !== 'administered' && request.status !== 'not-administered') {
      return this.fail('El estado de la toma no es válido.');
    }
    const observation = request.observation?.trim() || null;
    if (observation && observation.length > 500) {
      return this.fail('La observación no puede superar los 500 caracteres.');
    }

    const body = {
      status: request.status === 'administered' ? INTAKE_ADMINISTERED : INTAKE_NOT_ADMINISTERED,
      observation,
    };
    return this.http.patch<void>(`/api/medicamentos/agenda/${request.intakeId}`, body).pipe(
      map(() => ({
        intakeId: request.intakeId,
        date: request.date,
        status: request.status,
        observation,
        recordedAt: new Date().toISOString(),
        recordedByUserId: '',
        recordedByName: '',
      })),
    );
  }

  // ───────── SCRUM-242: historial de tomas de una persona ─────────

  /** Historial de una persona, de más reciente a más antiguo (el backend ya lo ordena). */
  getPersonHistory(personId: string): Observable<MedicationHistoryEntry[]> {
    return this.http.get<MedicationHistoryEntry[]>(`/api/persons/${personId}/medication-history`);
  }

  private schemeById(personId: string, schemeId: string): Observable<MedicationScheme> {
    return this.getSchemes(personId).pipe(
      switchMap((schemes) => {
        const scheme = schemes.find((s) => s.id === schemeId);
        return scheme ? of(scheme) : this.fail('El esquema no existe.');
      }),
    );
  }

  private fail(message: string): Observable<never> {
    return throwError(() => ({ error: { message } }));
  }
}

function toScheme(dto: SchemeDto): MedicationScheme {
  const { isActive, ...rest } = dto;
  return { ...rest, active: isActive };
}

function toAgendaItem(dto: AgendaIntakeDto, time: string): AgendaItem {
  const status: AdministrationStatus | null =
    dto.status === INTAKE_ADMINISTERED ? 'administered' : dto.status === INTAKE_NOT_ADMINISTERED ? 'not-administered' : null;
  return {
    intakeId: dto.id,
    personId: dto.personId,
    personName: dto.personFullName,
    medicationName: dto.medicationName,
    dose: dto.dose,
    time,
    administration:
      status && dto.status !== INTAKE_PENDING
        ? {
            intakeId: dto.id,
            date: (dto.administeredAt ?? '').slice(0, 10),
            status,
            observation: dto.observation,
            recordedAt: dto.administeredAt ?? '',
            recordedByUserId: dto.administeredByUserId ?? '',
            recordedByName: '',
          }
        : null,
  };
}
