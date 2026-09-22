import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CreateSocialRecordRequest, CreateSocialRecordResponse ,SocialRecordDetail,SocialRecordsResponse} from '../interfaces/social-record.interface';
import { SocialRecordFilters } from '../interfaces/social-record-filters.interface';
import { RegisterExitRequest } from '../interfaces/exit-stay.interface';

@Injectable({
  providedIn: 'root'
})
export class SocialRecordsService {

  private http = inject(HttpClient);

  // private readonly apiUrl = '/api/social-records';
  // URL  backend:
  private readonly apiUrl = 'http://localhost:5000/api/social-records';

  create(dto: CreateSocialRecordRequest): Observable<CreateSocialRecordResponse> {
    return this.http.post<CreateSocialRecordResponse>(this.apiUrl, dto);
  }

  getAll( page: number = 1,search: string = '',filters?: SocialRecordFilters): Observable<SocialRecordsResponse> {

    let params = new HttpParams() .set('page', page);
    if (search.trim()) 
      {params = params.set( 'search', search.trim() );}
    if (filters) {
      // Agregar parámetros de filtro
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
    return this.http.get<SocialRecordsResponse>(
      this.apiUrl,
      { params }
    );
  }
  // search(query: string): Observable<SocialRecordSearchResult[]> {
  // return this.http.get<SocialRecordSearchResult[]>(`${this.apiUrl}/search`, { params: {  q: query } } );}

  getById(id: string): Observable<SocialRecordDetail> {
    return this.http.get<SocialRecordDetail>(
      `${this.apiUrl}/${id}`
    );
   
  }

  update(id: string, data: CreateSocialRecordRequest): Observable<SocialRecordDetail> {
  // Ajustá la URL según el endpoint de tu backend (p. ej. PUT o PATCH)
  return this.http.put<SocialRecordDetail>(`${this.apiUrl}/social-records/${id}`, data);
 }


 /**
 * Registra el ingreso de una persona a la Casona (cambia personType a Residente y crea estadía)
 */
registerEntry(recordId: string, entryDate?: string): Observable<SocialRecordDetail> {
  const payload = {
    entryDate: entryDate || new Date().toISOString().substring(0, 10)
  };

  return this.http.post<SocialRecordDetail>(
    `${this.apiUrl}/${recordId}/stays/entry`,
    payload
  );
}

 /**
   * Registra el egreso de una estadía en la casona
   */
  registerExit(recordId: string, payload: RegisterExitRequest): Observable<SocialRecordDetail> {
    return this.http.post<SocialRecordDetail>(
      `${this.apiUrl}/${recordId}/stays/exit`,
      payload
    );
  }
}



