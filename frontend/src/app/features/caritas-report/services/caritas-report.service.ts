import { Injectable } from '@angular/core';
import { Observable, delay, of, throwError } from 'rxjs';
import { CaritasReport, ReportPeriodType } from '../interfaces/caritas-report.interface';
import { periodRange } from '../utils/caritas-report.util';

/**
 * SCRUM-271: informe estadístico para Cáritas.
 *
 * El backend todavía no lo calcula (y la ficha no guarda género, de dónde es la persona ni
 * motivo de asistencia). Por eso `generate()` devuelve números DE EJEMPLO, repetibles para un
 * mismo período, marcados con `isSample: true`. Para conectarlo, reemplazar el cuerpo por
 * `GET /api/reports/caritas?type=...&to=...` devolviendo el mismo `CaritasReport` con
 * `isSample: false`.
 */
@Injectable({ providedIn: 'root' })
export class CaritasReportService {
  generate(type: ReportPeriodType, to: string): Observable<CaritasReport> {
    if (type !== 'trimestral' && type !== 'semestral') {
      return throwError(() => ({ error: { message: 'Elegí un período trimestral o semestral.' } }));
    }
    if (!/^\d{4}-\d{2}-\d{2}$/.test(to)) {
      return throwError(() => ({ error: { message: 'La fecha de corte no es válida.' } }));
    }
    return of(CaritasReportService.sample(type, to)).pipe(delay(300));
  }

  /** Reparte `total` según proporciones, sin perder ni sumar personas. */
  static split(total: number, weights: number[]): number[] {
    const sum = weights.reduce((a, b) => a + b, 0);
    const parts = weights.map((w) => Math.floor((total * w) / sum));
    let rest = total - parts.reduce((a, b) => a + b, 0);
    for (let i = 0; rest > 0; i = (i + 1) % parts.length, rest--) parts[i]++;
    return parts;
  }

  static sample(type: ReportPeriodType, to: string): CaritasReport {
    const { from } = periodRange(type, to);
    // semilla simple por fecha de corte para que el mismo período dé siempre lo mismo
    const seed = Number(to.replace(/-/g, '')) % 17;
    const persons = (type === 'trimestral' ? 64 : 112) + seed;
    const [male, female, other] = CaritasReportService.split(persons, [58, 38, 4]);
    const [street, housed] = CaritasReportService.split(persons, [45, 55]);
    const [withDni, withoutDni] = CaritasReportService.split(persons, [72, 28]);
    const geo = CaritasReportService.split(persons, [46, 22, 14, 10, 8]);
    const [alcohol, drugs, violence, multiple, unspecified] = CaritasReportService.split(persons, [22, 18, 12, 30, 18]);
    const attendances = persons * (type === 'trimestral' ? 7.4 : 12.8);

    return {
      periodType: type,
      from,
      to,
      isSample: true,
      totals: {
        attendedPersons: persons,
        averageAttendancesPerPerson: Math.round((attendances / persons) * 10) / 10,
        newRegistrations: Math.round(persons * 0.22),
      },
      gender: { male, female, other },
      housing: { street, housed },
      dni: { withDni, withoutDni },
      geography: ['Barrio del Centro Barrial', 'Resto de la ciudad', 'Otra localidad de la provincia', 'Otra provincia', 'Otro país / no informado']
        .map((label, i) => ({ label, count: geo[i] })),
      reasons: { alcohol, drugs, violence, multiple, unspecified },
    };
  }
}
