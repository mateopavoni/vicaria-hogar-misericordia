import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CreateSocialRecordRequest, CreateSocialRecordResponse ,SocialRecordsResponse} from '../interfaces/social-record.interface';


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

   getAll( page: number = 1,search: string = ''): Observable<SocialRecordsResponse> {

    let params = new HttpParams() .set('page', page);
    if (search.trim()) 
      {params = params.set( 'search', search.trim() );}
    return this.http.get<SocialRecordsResponse>(
      this.apiUrl,
      { params }
    );
  }
  // search(query: string): Observable<SocialRecordSearchResult[]> {
  // return this.http.get<SocialRecordSearchResult[]>(`${this.apiUrl}/search`, { params: {  q: query } } );}

}

