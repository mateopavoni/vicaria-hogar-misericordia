import { Injectable, signal } from '@angular/core';
import { PersonReportData } from '../interfaces/report-fields.interface';
import { cleanReportData } from '../utils/report-fields.util';

/**
 * SCRUM-275: valores de los campos del informe de cada persona (clave: personId).
 *
 * OJO — backend pendiente: la ficha no guarda estos campos. Vive en memoria (se pierde al
 * recargar). Para conectarlo: leer/escribir estos campos en el detalle de la ficha y cambiar
 * `get`/`save` por el detalle y el PUT de la ficha.
 */
@Injectable({ providedIn: 'root' })
export class PersonReportDataService {
  private readonly state = signal<Record<string, PersonReportData>>({});

  /** Se lee en un computed/template: reacciona cuando se guarda. */
  get(personId: string): PersonReportData {
    return this.state()[personId] ?? {};
  }

  save(personId: string, data: PersonReportData): PersonReportData {
    const clean = cleanReportData(data);
    this.state.update((all) => ({ ...all, [personId]: clean }));
    return clean;
  }
}
