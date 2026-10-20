import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { SocialRecordFilters} from '../interfaces/social-record-filters.interface';

const EMPTY_FILTERS: SocialRecordFilters = {
  entryDateFrom: null,
  entryDateTo: null,
  withoutObservationsDays: null,
  hasDni: null,
  hasAddress: null
};


@Injectable({
  providedIn: 'root'
})
export class SocialRecordFiltersService {

  private filtersSubject =
    new BehaviorSubject<SocialRecordFilters>(
      EMPTY_FILTERS
    );

  filters$ =
    this.filtersSubject.asObservable();


  get filters(): SocialRecordFilters {
    return this.filtersSubject.value;
  }


  setFilters(
    filters: SocialRecordFilters
  ): void {

    this.filtersSubject.next(filters);

  }


  clear(): void {

    this.filtersSubject.next({
      ...EMPTY_FILTERS
    });

  }

}