import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CreateSocialRecordRequest, CreateSocialRecordResponse, PersonProfileStatus, PersonType, SocialRecordDetail, SocialRecordsResponse } from '../interfaces/social-record.interface';
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

  // listado paginado con búsqueda y filtros avanzados (SCRUM-6, SCRUM-21)
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

  update(id: string, data: CreateSocialRecordRequest): Observable<SocialRecordDetail> {
    return this.http.put<SocialRecordDetail>(`${this.apiUrl}/${id}`, data);
  }

  // cambia el tipo de persona (ambulatorio/residente); el backend crea la estadía en la Casa de Convivencia
  // automáticamente al pasar a Residente (SCRUM-134), no hace falta un endpoint de "entry" aparte
  updatePersonType(personId: string, personType: PersonType): Observable<void> {
    return this.http.put<void>(`/api/persons/${personId}/type`, { personType });
  }

  // registra el egreso de una estadía en la Casa de Convivencia: la ruta real vive en CasaConvivenciaStayController,
  // no en social-records, y necesita el id de la estadía activa (no el de la ficha)
  registerExit(stayId: string, payload: CasaConvivenciaStayExitRequest): Observable<void> {
    return this.http.put<void>(`/api/casa-convivencia-stays/${stayId}/egreso`, payload);
  }

  // marca manualmente a una persona ambulatoria como activa/inactiva (SCRUM-78/156)
  updateProfileStatus(personId: string, status: PersonProfileStatus): Observable<void> {
    return this.http.put<void>(`/api/persons/${personId}/status`, { status });
  }
}



