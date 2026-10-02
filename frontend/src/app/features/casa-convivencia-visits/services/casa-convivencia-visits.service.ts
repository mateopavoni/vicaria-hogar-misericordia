import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CreateVisitDto, Resident, UpdateVisitDto, Visit, VisitStatus } from '../interfaces/visit.interface';

@Injectable({
  providedIn: 'root',
})
export class CasaConvivenciaVisitsService {
  private http = inject(HttpClient);
  private apiUrl = '/api/casa-convivencia-visitas';

  // rango visible (día, semana o mes); fechas en formato YYYY-MM-DD
  getByRange(desde: string, hasta: string): Observable<Visit[]> {
    return this.http.get<Visit[]>(this.apiUrl, {
      params: new HttpParams().set('desde', desde).set('hasta', hasta),
    });
  }

  // SCRUM-214 (AC): "selector de residente (solo residentes)" — lista liviana, no
  // paginada, de personas con personType Resident (contrato asumido, ver interface).
  getResidents(): Observable<Resident[]> {
    return this.http.get<Resident[]>(`${this.apiUrl}/residentes`);
  }

  create(dto: CreateVisitDto): Observable<Visit> {
    return this.http.post<Visit>(this.apiUrl, dto);
  }

  update(id: string, dto: UpdateVisitDto): Observable<Visit> {
    return this.http.put<Visit>(`${this.apiUrl}/${id}`, dto);
  }

  // SCRUM-74 (AC): "se puede marcar una visita como realizada, cancelada o
  // pendiente"; "las visitas canceladas quedan registradas con motivo opcional".
  updateStatus(id: string, status: VisitStatus, cancellationReason?: string | null): Observable<Visit> {
    return this.http.patch<Visit>(`${this.apiUrl}/${id}/estado`, { status, cancellationReason: cancellationReason ?? null });
  }
}
