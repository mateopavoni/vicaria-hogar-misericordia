import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { LifeHistory, LifeHistoryStage } from '../interfaces/life-history.interface';

// forma real de la respuesta del backend (LifeStoryResponseDto): nombres de etapa distintos
// a los del frontend (beforeHogar/inHogar/afterHogar vs beforeHome/duringHome/afterHome)
interface LifeStorySectionResponse {
  content: string | null;
  isCompleted: boolean;
  updatedByUserId: string | null;
  updatedByName: string | null;
  updatedAt: string | null;
}

interface LifeStoryResponse {
  id: string;
  personId: string;
  beforeHogar: LifeStorySectionResponse;
  inHogar: LifeStorySectionResponse;
  afterHogar: LifeStorySectionResponse;
}

// mapeo etapa frontend -> segmento de ruta que espera el backend
const STAGE_TO_BACKEND_PATH: Record<LifeHistoryStage, string> = {
  beforeHome: 'before-hogar',
  duringHome: 'in-hogar',
  afterHome: 'after-hogar',
};

function toSection(section: LifeStorySectionResponse) {
  return {
    text: section.content,
    lastEditedBy: section.updatedByName,
    lastEditedAt: section.updatedAt,
  };
}

function toLifeHistory(response: LifeStoryResponse): LifeHistory {
  return {
    beforeHome: toSection(response.beforeHogar),
    duringHome: toSection(response.inHogar),
    afterHome: toSection(response.afterHogar),
  };
}

@Injectable({
  providedIn: 'root'
})
export class LifeStoryService {
  private http = inject(HttpClient);

  getByPersonId(personId: string): Observable<LifeHistory> {
    return this.http
      .get<LifeStoryResponse>(`/api/persons/${personId}/life-story`)
      .pipe(map(toLifeHistory));
  }

  updateStage(personId: string, stage: LifeHistoryStage, content: string): Observable<LifeHistory> {
    const path = STAGE_TO_BACKEND_PATH[stage];
    return this.http
      .put<LifeStoryResponse>(`/api/persons/${personId}/life-story/${path}`, { content })
      .pipe(map(toLifeHistory));
  }
}
