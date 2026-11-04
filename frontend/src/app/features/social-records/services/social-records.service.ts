import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CreateSocialRecordRequest, CreateSocialRecordResponse, PersonStatus, PersonType, SocialRecordDetail, SocialRecordsResponse } from '../interfaces/social-record.interface';
import { SocialRecordFilters } from '../interfaces/social-record-filters.interface';
import { CasaConvivenciaStayExitRequest } from '../interfaces/exit-stay.interface';

@Injectable({
  providedIn: 'root'
})
export class SocialRecordsService {

  private http = inject(HttpClient);

  // ruta relativa: el proxy de dev (proxy.conf.json) y el interceptor de auth la resuelven
  private readonly apiUrl = '/api/social-records';

  create(dto: CreateSocialRecordRequest): Observable<CreateSocialRecordResponse> {
    return this.http.post<CreateSocialRecordResponse>(this.apiUrl, dto);
  }

  // listado paginado con búsqueda y filtros avanzados
  getAll(page: number = 1, search: string = '', filters?: SocialRecordFilters): Observable<SocialRecordsResponse> {
    let params = new HttpParams().set('page', page);
    if (search.trim()) {
      params = params.set('search', search.trim());
    }
    if (filters) {
      if (filters.entryDateFrom) {
        params = params.set('entryDateFrom', filters.entryDateFrom);
      }
      if (filters.entryDateTo) {
        params = params.set('entryDateTo', filters.entryDateTo);
      }
      if (filters.withoutObservationsDays !== null) {
        params = params.set('withoutObservationsDays', filters.withoutObservationsDays);
      }
      if (filters.hasDni !== null) {
        params = params.set('hasDni', filters.hasDni);
      }
      if (filters.hasAddress !== null) {
        params = params.set('hasAddress', filters.hasAddress);
      }
    }
    return this.http.get<SocialRecordsResponse>(`${this.apiUrl}/list`, { params });
  }

  getById(id: string): Observable<SocialRecordDetail> {
    return this.http.get<SocialRecordDetail>(`${this.apiUrl}/${id}`);
  }

  // el backend responde 204 sin cuerpo; para ver la ficha actualizada hay que pedirla con getById
  update(id: string, data: CreateSocialRecordRequest): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, data);
  }

  // cambia el tipo de persona (ambulatorio/residente); el backend crea la estadía en la Casa de Convivencia
  // automáticamente al pasar a Residente, no hace falta un endpoint de "entry" aparte
  updatePersonType(personId: string, personType: PersonType): Observable<void> {
    return this.http.put<void>(`/api/persons/${personId}/type`, { personType });
  }

  // registra el egreso de una estadía en la Casa de Convivencia: la ruta real vive en CasaConvivenciaStayController,
  // no en social-records, y necesita el id de la estadía activa (no el de la ficha)
  registerExit(stayId: string, payload: CasaConvivenciaStayExitRequest): Observable<void> {
    return this.http.put<void>(`/api/casa-convivencia-stays/${stayId}/egreso`, payload);
  }

  // Cambio manual de estado Activo/Inactivo con motivo obligatorio. El backend
  // lo guarda en el historial de estados y, al pasar a Inactivo, notifica a los Referentes
  updateStatus(personId: string, status: PersonStatus, reason: string): Observable<void> {
    return this.http.patch<void>(`/api/persons/${personId}/status`, { status, reason });
  }
}



