import { Injectable, signal } from '@angular/core';
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

function slugify(text: string): string {
  return (
    normalize(text)
      .toLowerCase()
      .normalize('NFD')
      .replace(/[̀-ͯ]/g, '')
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '') || 'valor'
  );
}

const seed = (labels: string[]): ReportFieldValue[] =>
  labels.map((label) => ({ id: slugify(label), label, active: true }));

/** Valores iniciales: salen del criterio de aceptación; el resto lo ajusta el Referente. */
export const DEFAULT_CATALOGS: Record<ReportFieldKey, ReportFieldValue[]> = {
  gender: seed(['Masculino', 'Femenino', 'Otro']),
  housing: seed(['En situación de calle', 'Con domicilio', 'Transitorio']),
  origin: seed(['Misma ciudad', 'Otra localidad de la provincia', 'Otra provincia', 'Otro país']),
  document: seed(['Con DNI', 'Sin DNI', 'En trámite']),
  mainReason: seed(['Consumo de alcohol', 'Consumo de drogas', 'Violencia', 'Múltiple', 'No especificado']),
};

/**
 * SCRUM-275: catálogo de valores de los campos del informe de Cáritas, configurable por el Referente
 * (agregar, editar, desactivar). Desactivar un valor lo saca de las opciones nuevas pero las fichas
 * que ya lo tienen lo conservan.
 *
 * OJO — backend pendiente: no existe endpoint de catálogos. Igual que AttendanceActivitiesService,
 * vive en memoria (se pierde al recargar) hasta que exista.
 */
@Injectable({ providedIn: 'root' })
export class ReportFieldCatalogService {
  private readonly state = signal<Record<ReportFieldKey, ReportFieldValue[]>>(
    Object.fromEntries(REPORT_FIELD_KEYS.map((k) => [k, DEFAULT_CATALOGS[k].map((v) => ({ ...v }))])) as Record<ReportFieldKey, ReportFieldValue[]>,
  );

  readonly all = this.state.asReadonly();

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

  add(key: ReportFieldKey, rawLabel: string): CatalogActionResult {
    const error = this.validate(key, rawLabel, null);
    if (error) return { ok: false, message: error };

    const label = normalize(rawLabel);
    const base = slugify(label);
    let id = base;
    let n = 2;
    while (this.state()[key].some((v) => v.id === id)) {
      id = `${base}-${n++}`;
    }
    const value: ReportFieldValue = { id, label, active: true };
    this.update(key, (list) => [...list, value]);
    return { ok: true, value };
  }

  rename(key: ReportFieldKey, id: string, rawLabel: string): CatalogActionResult {
    const found = this.state()[key].find((v) => v.id === id);
    if (!found) return { ok: false, message: 'El valor no existe.' };
    const error = this.validate(key, rawLabel, id);
    if (error) return { ok: false, message: error };

    const value = { ...found, label: normalize(rawLabel) };
    this.update(key, (list) => list.map((v) => (v.id === id ? value : v)));
    return { ok: true, value };
  }

  setActive(key: ReportFieldKey, id: string, active: boolean): CatalogActionResult {
    const found = this.state()[key].find((v) => v.id === id);
    if (!found) return { ok: false, message: 'El valor no existe.' };
    if (!active && found.active && this.active(key).length <= 1) {
      return { ok: false, message: 'Tiene que quedar al menos un valor activo en cada campo.' };
    }
    const value = { ...found, active };
    this.update(key, (list) => list.map((v) => (v.id === id ? value : v)));
    return { ok: true, value };
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
