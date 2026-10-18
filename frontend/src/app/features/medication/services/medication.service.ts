import { Injectable, inject } from '@angular/core';
import { Observable, delay, map, of, throwError } from 'rxjs';
import { todayLocalIso } from '../../../shared/utils/date.util';
import { isEvaluationExpired } from '../utils/scheme-review.util';
import { MedicationStockService } from '../../medication-stock/services/medication-stock.service';
import { AttendanceService } from '../../attendance/services/attendance.service';
import {
  AgendaItem,
  AgendaTimeSlot,
  CreateMedicationSchemeRequest,
  MedicationAdministration,
  MedicationAgenda,
  MedicationHistoryEntry,
  RecordAdministrationRequest,
  MedicationActor,
  MedicationCatalogItem,
  MedicationScheme,
  PsychiatricEvaluationStatus,
} from '../interfaces/medication.interface';

const TIME_PATTERN = /^([01]\d|2[0-3]):[0-5]\d$/;

/**
 * SCRUM-234: esquema de medicación de una persona.
 *
 * OJO — backend pendiente (EP-11, SCRUM-59): hoy no existen endpoints de catálogo de
 * medicamentos, stock de farmacia, evaluaciones psiquiátricas ni esquemas. Hasta entonces
 * este servicio vive en memoria (se pierde al recargar) y aplica las reglas del AC con la
 * misma forma de error ({ error: { message } }) que lee extractApiError. Al llegar los
 * endpoints reales solo hay que cambiar el cuerpo de estos métodos por llamadas HttpClient.
 *
 * - Catálogo y stock: vienen de MedicationStockService (SCRUM-246), con datos de prueba genéricos
 *   (no son medicamentos reales); uno de ellos arranca sin stock.
 * - Evaluación vigente: el backend exige una evaluación vigente para que una persona sea
 *   Residente (SCRUM-134), así que acá se asume vigente para residentes y no vigente para el resto.
 */
@Injectable({ providedIn: 'root' })
export class MedicationService {
  private stockService = inject(MedicationStockService);

  private attendanceService = inject(AttendanceService);

  private readonly schemes: MedicationScheme[] = [];
  private readonly administrations = new Map<string, MedicationAdministration>();
  private nextId = 1;

  getCatalog(): Observable<MedicationCatalogItem[]> {
    const catalog: MedicationCatalogItem[] = this.stockService
      .snapshotForCatalog()
      .map((m) => ({ id: m.id, name: m.name, pharmacyStock: m.quantity }));
    return of(catalog).pipe(delay(100));
  }

  /**
   * `evaluatedAt` (YYYY-MM-DD) es la fecha de la última evaluación psiquiátrica, cuando el
   * perfil la tenga. Hoy el frontend no la recibe del backend, así que suele venir null:
   * en ese caso solo cuenta si la persona es Residente. Con fecha, pasados 6 meses vence.
   */
  getEvaluationStatus(_personId: string, isResident: boolean, evaluatedAt: string | null = null): Observable<PsychiatricEvaluationStatus> {
    const expired = !!evaluatedAt && isEvaluationExpired(evaluatedAt, todayLocalIso());
    return of({ valid: isResident && !expired, evaluatedAt, expired }).pipe(delay(100));
  }

  getSchemes(personId: string): Observable<MedicationScheme[]> {
    const own = this.schemes
      .filter((s) => s.personId === personId)
      .sort((a, b) => b.loadedAt.localeCompare(a.loadedAt));
    return of(own.map((s) => ({ ...s, pendingStock: this.isPending(s) }))).pipe(delay(100));
  }

  create(
    request: CreateMedicationSchemeRequest,
    isResident: boolean,
    actor: MedicationActor,
    evaluatedAt: string | null = null,
  ): Observable<MedicationScheme> {
    // 1) evaluación psiquiátrica vigente (y no vencida: más de 6 meses), si no se bloquea la carga
    if (evaluatedAt && isEvaluationExpired(evaluatedAt, todayLocalIso())) {
      return this.fail('No se puede cargar medicación: la evaluación psiquiátrica venció (pasaron más de 6 meses).');
    }
    if (!isResident) {
      return this.fail('No se puede cargar medicación: la persona no tiene una evaluación psiquiátrica vigente.');
    }

    // 2) solo medicamentos que existan en el catálogo
    const medication = this.stockService.find(request.medicationId);
    if (!medication) {
      return this.fail('El medicamento no existe en el catálogo.');
    }

    const dose = request.dose.trim();
    if (!dose) {
      return this.fail('La dosis es obligatoria.');
    }
    if (!TIME_PATTERN.test(request.time)) {
      return this.fail('La hora no es válida.');
    }

    // 3) sin stock: se permite, pero queda marcado como pendiente de stock
    const scheme: MedicationScheme = {
      id: `scheme-${this.nextId++}`,
      personId: request.personId,
      medicationId: medication.id,
      medicationName: medication.name,
      dose,
      time: request.time,
      loadedAt: new Date().toISOString(),
      loadedByUserId: actor.id,
      loadedByName: actor.name,
      reviewedAt: null,
      reviewedByUserId: null,
      reviewedByName: null,
      active: true,
      pendingStock: medication.quantity <= 0,
    };
    this.schemes.push(scheme);
    return of({ ...scheme }).pipe(delay(150));
  }

  /** SCRUM-250: "Marcar como revisado" — registra revisado_en y revisado_por. Solo esquemas vigentes. */
  markReviewed(schemeId: string, actor: MedicationActor): Observable<MedicationScheme> {
    const scheme = this.schemes.find((s) => s.id === schemeId);
    if (!scheme) {
      return this.fail('El esquema no existe.');
    }
    if (!scheme.active) {
      return this.fail('Solo se puede revisar un esquema vigente.');
    }
    scheme.reviewedAt = new Date().toISOString();
    scheme.reviewedByUserId = actor.id;
    scheme.reviewedByName = actor.name;
    return of({ ...scheme }).pipe(delay(100));
  }

  /** Marca un esquema como vigente (true) o histórico (false). */
  setActive(schemeId: string, active: boolean): Observable<MedicationScheme> {
    const scheme = this.schemes.find((s) => s.id === schemeId);
    if (!scheme) {
      return this.fail('El esquema no existe.');
    }
    scheme.active = active;
    return of({ ...scheme }).pipe(delay(100));
  }

  // ───────── SCRUM-238: agenda diaria ─────────

  /**
   * Agenda del día agrupada por hora. Se arma sola con la asistencia registrada ese día
   * (cualquier actividad): solo entran las personas que asistieron y tienen medicación.
   * - Esquemas pendientes de stock no entran (se informan aparte en pendingStockCount).
   * - Un esquema solo cuenta desde el día en que se cargó.
   * - Si el esquema hoy es histórico solo figura en días donde ya tiene una toma registrada,
   *   así el historial de días anteriores no cambia al dar de baja un medicamento.
   */
  getAgenda(date: string): Observable<MedicationAgenda> {
    return this.attendanceService.getAll().pipe(
      map((days) => {
        const present = new Map<string, string>();
        for (const day of days) {
          if (day.date !== date) continue;
          for (const person of day.persons) {
            present.set(person.personId, person.fullName);
          }
        }

        const items: AgendaItem[] = [];
        let pendingStockCount = 0;
        for (const scheme of this.schemes) {
          const personName = present.get(scheme.personId);
          if (personName === undefined) continue;
          if (scheme.loadedAt.slice(0, 10) > date) continue;

          const administration = this.administrations.get(this.adminKey(scheme.id, date)) ?? null;
          if (!scheme.active && !administration) continue;
          if (this.isPending(scheme) && !administration) {
            pendingStockCount++;
            continue;
          }

          items.push({
            schemeId: scheme.id,
            personId: scheme.personId,
            personName,
            medicationName: scheme.medicationName,
            dose: scheme.dose,
            time: scheme.time,
            administration: administration ? { ...administration } : null,
          });
        }

        const byTime = new Map<string, AgendaItem[]>();
        for (const item of items) {
          byTime.set(item.time, [...(byTime.get(item.time) ?? []), item]);
        }
        const slots: AgendaTimeSlot[] = [...byTime.entries()]
          .sort(([a], [b]) => a.localeCompare(b))
          .map(([time, group]) => ({
            time,
            items: group.sort((a, b) => a.personName.localeCompare(b.personName, 'es')),
          }));

        return { date, slots, pendingStockCount };
      }),
      delay(100),
    );
  }

  /** Marca una toma como administrada / no administrada. Solo del día actual; se puede corregir. */
  recordAdministration(request: RecordAdministrationRequest, actor: MedicationActor): Observable<MedicationAdministration> {
    if (request.date !== todayLocalIso()) {
      return this.fail('Solo se puede registrar la administración del día actual.');
    }
    if (request.status !== 'administered' && request.status !== 'not-administered') {
      return this.fail('El estado de la toma no es válido.');
    }
    const scheme = this.schemes.find((s) => s.id === request.schemeId);
    if (!scheme) {
      return this.fail('El esquema no existe.');
    }
    const observation = request.observation?.trim() || null;
    if (observation && observation.length > 500) {
      return this.fail('La observación no puede superar los 500 caracteres.');
    }

    const administration: MedicationAdministration = {
      schemeId: scheme.id,
      date: request.date,
      status: request.status,
      observation,
      recordedAt: new Date().toISOString(),
      recordedByUserId: actor.id,
      recordedByName: actor.name,
    };
    const key = this.adminKey(scheme.id, request.date);
    const previous = this.administrations.get(key);
    this.administrations.set(key, administration);

    // SCRUM-246: marcar administrada descuenta una unidad del stock; si después se corrige, se devuelve
    const stockActor = { id: actor.id, name: actor.name };
    if (request.status === 'administered' && previous?.status !== 'administered') {
      this.stockService.consume(scheme.medicationId, key, request.date, stockActor);
    } else if (request.status !== 'administered' && previous?.status === 'administered') {
      this.stockService.restore(scheme.medicationId, key, request.date, stockActor);
    }
    return of({ ...administration }).pipe(delay(100));
  }

  // ───────── SCRUM-242: historial de tomas de una persona ─────────

  /**
   * Historial de una persona, de más reciente a más antiguo. Por cada día en que asistió
   * (o tiene tomas registradas) lista las tomas programadas de sus esquemas y su estado:
   * administrada, no administrada o sin registrar. Mismas reglas que la agenda diaria.
   */
  getPersonHistory(personId: string): Observable<MedicationHistoryEntry[]> {
    return this.attendanceService.getAll().pipe(
      map((days) => {
        const schemes = this.schemes.filter((s) => s.personId === personId);
        const dates = new Set<string>();
        for (const day of days) {
          if (day.persons.some((p) => p.personId === personId)) dates.add(day.date);
        }
        for (const scheme of schemes) {
          for (const adm of this.administrations.values()) {
            if (adm.schemeId === scheme.id) dates.add(adm.date);
          }
        }

        const entries: MedicationHistoryEntry[] = [];
        for (const date of dates) {
          for (const scheme of schemes) {
            if (scheme.loadedAt.slice(0, 10) > date) continue;
            const adm = this.administrations.get(this.adminKey(scheme.id, date)) ?? null;
            if (!scheme.active && !adm) continue;
            if (this.isPending(scheme) && !adm) continue;
            entries.push({
              date,
              time: scheme.time,
              schemeId: scheme.id,
              medicationId: scheme.medicationId,
              medicationName: scheme.medicationName,
              dose: scheme.dose,
              status: adm ? adm.status : 'unregistered',
              observation: adm?.observation ?? null,
              recordedByName: adm?.recordedByName ?? null,
              recordedAt: adm?.recordedAt ?? null,
            });
          }
        }
        return entries.sort((a, b) => b.date.localeCompare(a.date) || a.time.localeCompare(b.time));
      }),
      delay(100),
    );
  }

  /** Pendiente de stock = se cargó sin stock y todavía no hay (un ingreso lo libera solo). */
  private isPending(scheme: MedicationScheme): boolean {
    return scheme.pendingStock && this.stockService.quantityOf(scheme.medicationId) <= 0;
  }

  private adminKey(schemeId: string, date: string): string {
    return `${schemeId}|${date}`;
  }

  private fail(message: string): Observable<never> {
    return throwError(() => ({ error: { message } }));
  }
}
