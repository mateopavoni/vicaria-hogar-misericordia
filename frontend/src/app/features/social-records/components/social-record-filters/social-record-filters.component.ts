import { Component, inject, output} from '@angular/core';
import {FormBuilder, ReactiveFormsModule} from '@angular/forms';
import { SocialRecordFilters } from '../../interfaces/social-record-filters.interface';

@Component({
  selector: 'app-social-record-filters',
  imports: [ReactiveFormsModule],
  templateUrl: './social-record-filters.component.html',
  styleUrl: './social-record-filters.component.css'
})
export class SocialRecordFiltersComponent {

  private fb = inject(FormBuilder);

  filtersApplied =
    output<SocialRecordFilters>();

   filtersCleared = output<void>();

  form = this.fb.nonNullable.group({

    entryDateFrom: [''],

    entryDateTo: [''],

    withoutObservationsDays: [''],

    hasDni: ['all'],

    hasAddress: ['all']

  });


  applyFilters(): void {

    const value = this.form.getRawValue();

    this.filtersApplied.emit({

      entryDateFrom:
        value.entryDateFrom || null,

      entryDateTo:
        value.entryDateTo || null,

      withoutObservationsDays:
        value.withoutObservationsDays
          ? Number(value.withoutObservationsDays)
          : null,

      hasDni:
        value.hasDni === 'all'
          ? null
          : value.hasDni === 'yes',

      hasAddress:
        value.hasAddress === 'all'
          ? null
          : value.hasAddress === 'yes'

    });

  }


 clearFilters(): void {
  this.form.reset({
    entryDateFrom: '',
    entryDateTo: '',
    withoutObservationsDays: 'all',
    hasDni: 'all',
    hasAddress: 'all'
  });

  this.filtersCleared.emit();
}

}