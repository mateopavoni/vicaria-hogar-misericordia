import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, catchError, map, of, shareReplay } from 'rxjs';
import { extractApiError } from '../../../core/http/api-error';
import {
  CatalogActionResult,
  REPORT_FIELD_KEYS,
  ReportFieldKey,
  ReportFieldValue,
} from '../interfaces/report-fields.interface';

const LABEL_MIN = 2;
const LABEL_MAX = 60;

const normalize = (text: string): string => text.trim().replace(/\s+/g, ' ');
const keyOf = (text: string): string => normalize(text).toLowerCase();

/** CaritasCatalogCategory del backend (serializado como número). */
const CATEGORY: Record<ReportFieldKey, number> = { gender: 0, housing: 1, origin: 2, document: 3, mainReason: 4 };
const KEY_OF_CATEGORY: Record<number, ReportFieldKey> = { 0: 'gender', 1: 'housing', 2: 'origin', 3: 'document', 4: 'mainReason' };

/** CaritasCatalogValueDto del backend (SCRUM-274). */
interface CatalogValueDto {
  id: string;
  category: number;
  name: string;
  isActive: boolean;
}

const emptyCatalogs = (): Record<ReportFieldKey, ReportFieldValue[]> =>
  Object.fromEntries(REPORT_FIELD_KEYS.map((k) => [k, []])) as unknown as Record<ReportFieldKey, ReportFieldValue[]>;

const toValue = (dto: CatalogValueDto): ReportFieldValue => ({ id: dto.id, label: dto.name, active: dto.isActive });

/**
 * SCRUM-275: catálogo de valores de los campos del informe de Cáritas contra la API (SCRUM-274):
 * GET/POST /api/caritas-catalogs, PUT /{id} y PATCH /{id}/status (alta, edición y baja lógica solo
 * Referente). Desactivar un valor lo saca de las opciones nuevas pero las fichas que ya lo tienen
 * lo conservan. `load` se pide una vez y queda en memoria; las acciones actualizan ese estado.
 */
@Injectable({ providedIn: 'root' })
export class ReportFieldCatalogService {
  private readonly http = inject(HttpClient);
  private readonly state = signal<Record<ReportFieldKey, ReportFieldValue[]>>(emptyCatalogs());
  private loaded$: Observable<void> | null = null;

  readonly all = this.state.asReadonly();

  /** Trae todos los valores (activos e inactivos); se comparte entre pantallas. */
  load(): Observable<void> {
    if (!this.loaded$) {
      const params = new HttpParams().set('onlyActive', false);
      this.loaded$ = this.http.get<CatalogValueDto[]>('/api/caritas-catalogs', { params }).pipe(
        map((list) => {
          const next = emptyCatalogs();
          for (const dto of list) {
            const key = KEY_OF_CATEGORY[dto.category];
            if (key) next[key].push(toValue(dto));
          }
          this.state.set(next);
        }),
        catchError(() => {
          this.loaded$ = null;
          return of(undefined);
        }),
        shareReplay(1),
      );
    }
    return this.loaded$;
  }

  values(key: ReportFieldKey): ReportFieldValue[] {
    return this.state()[key];
  }

  /** Solo los activos: las opciones que se ofrecen al cargar una ficha. */
  active(key: ReportFieldKey): ReportFieldValue[] {
    return this.state()[key].filter((v) => v.active);
  }

  /** Texto de un valor guardado (aunque esté desactivado). */
  label(key: ReportFieldKey, id: string | null | undefined): string | null {
    if (!id) return null;
    return this.state()[key].find((v) => v.id === id)?.label ?? null;
  }

  add(key: ReportFieldKey, rawLabel: string): Observable<CatalogActionResult> {
    const error = this.validate(key, rawLabel, null);
    if (error) return of({ ok: false, message: error });

    return this.request(
      this.http.post<CatalogValueDto>('/api/caritas-catalogs', { category: CATEGORY[key], name: normalize(rawLabel) }),
      'No se pudo agregar el valor.',
      (value) => this.update(key, (list) => [...list, value]),
    );
  }

  rename(key: ReportFieldKey, id: string, rawLabel: string): Observable<CatalogActionResult> {
    if (!this.state()[key].some((v) => v.id === id)) return of({ ok: false, message: 'El valor no existe.' });
    const error = this.validate(key, rawLabel, id);
    if (error) return of({ ok: false, message: error });

    return this.request(
      this.http.put<CatalogValueDto>(`/api/caritas-catalogs/${id}`, { name: normalize(rawLabel) }),
      'No se pudo actualizar el valor.',
      (value) => this.update(key, (list) => list.map((v) => (v.id === id ? value : v))),
    );
  }

  setActive(key: ReportFieldKey, id: string, active: boolean): Observable<CatalogActionResult> {
    const found = this.state()[key].find((v) => v.id === id);
    if (!found) return of({ ok: false, message: 'El valor no existe.' });
    if (!active && found.active && this.active(key).length <= 1) {
      return of({ ok: false, message: 'Tiene que quedar al menos un valor activo en cada campo.' });
    }

    // el PATCH responde 204 sin cuerpo
    const updated: CatalogValueDto = { id, category: CATEGORY[key], name: found.label, isActive: active };
    return this.request(
      this.http.patch<void>(`/api/caritas-catalogs/${id}/status`, { isActive: active }).pipe(map(() => updated)),
      'No se pudo cambiar el estado del valor.',
      (value) => this.update(key, (list) => list.map((v) => (v.id === id ? value : v))),
    );
  }

  private request(
    call: Observable<CatalogValueDto>,
    fallback: string,
    apply: (value: ReportFieldValue) => void,
  ): Observable<CatalogActionResult> {
    return call.pipe(
      map((dto): CatalogActionResult => {
        const value = toValue(dto);
        apply(value);
        return { ok: true, value };
      }),
      catchError((err) => of<CatalogActionResult>({ ok: false, message: extractApiError(err, fallback) })),
    );
  }

  private validate(key: ReportFieldKey, rawLabel: string, ignoreId: string | null): string | null {
    const label = normalize(rawLabel);
    if (label.length < LABEL_MIN) return `El valor tiene que tener al menos ${LABEL_MIN} caracteres.`;
    if (label.length > LABEL_MAX) return `El valor no puede superar los ${LABEL_MAX} caracteres.`;
    const dup = this.state()[key].some((v) => v.id !== ignoreId && keyOf(v.label) === keyOf(label));
    return dup ? 'Ya existe ese valor en este campo.' : null;
  }

  private update(key: ReportFieldKey, fn: (list: ReportFieldValue[]) => ReportFieldValue[]): void {
    this.state.update((all) => ({ ...all, [key]: fn(all[key]) }));
  }
}
