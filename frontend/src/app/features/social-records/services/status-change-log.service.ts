import { Injectable, signal } from '@angular/core';
import { ChangeHistoryItem } from '../interfaces/change-history.interface';
import { PersonStatus } from '../interfaces/social-record.interface';

export const STATUS_LABELS: Record<PersonStatus, string> = {
  [PersonStatus.Active]: 'Activo',
  [PersonStatus.Inactive]: 'Inactivo',
};

/**
 * SCRUM-264: registro de cambios MANUALES de estado hechos desde esta sesión (fecha, usuario y motivo).
 * El backend todavía no devuelve historial de estados ni guarda el motivo, así que se guarda en memoria
 * y se mezcla con `statusHistory` de la ficha cuando el backend lo exponga. Se pierde al recargar.
 */
@Injectable({ providedIn: 'root' })
export class StatusChangeLogService {
  private readonly entries = signal<Record<string, ChangeHistoryItem[]>>({});

  readonly all = this.entries.asReadonly();

  forPerson(personId: string): ChangeHistoryItem[] {
    return this.entries()[personId] ?? [];
  }

  record(personId: string, previous: PersonStatus, next: PersonStatus, modifiedBy: string, reason: string, now = new Date()): ChangeHistoryItem {
    const item: ChangeHistoryItem = {
      id: `local-${personId}-${now.getTime()}`,
      fieldName: 'Estado',
      previousValue: STATUS_LABELS[previous],
      newValue: STATUS_LABELS[next],
      modifiedBy,
      modifiedAt: now.toISOString(),
      reason: reason.trim(),
    };
    this.entries.update((all) => ({ ...all, [personId]: [item, ...(all[personId] ?? [])] }));
    return item;
  }
}

/** Une historial del backend + cambios locales, sin duplicados, del más nuevo al más viejo. */
export function mergeStatusHistory(remote: ChangeHistoryItem[] | undefined, local: ChangeHistoryItem[]): ChangeHistoryItem[] {
  const seen = new Set<string>();
  const out: ChangeHistoryItem[] = [];
  for (const item of [...(remote ?? []), ...local]) {
    const key = `${item.modifiedAt}|${item.newValue}`;
    if (seen.has(key)) continue;
    seen.add(key);
    out.push(item);
  }
  return out.sort((a, b) => b.modifiedAt.localeCompare(a.modifiedAt));
}
