import { Component, inject, OnInit, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder } from '@angular/forms';
import { ObservationCategory } from '../../interfaces/observation-category.interface';
import { ObservationFilters } from '../../interfaces/observation-filter.interface';

@Component({
  selector: 'app-observation-filters',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './observation-filters.component.html'
})
export class ObservationFiltersComponent implements OnInit {
  private fb = inject(FormBuilder);

  // Inputs desde el componente padre
  categories = input<ObservationCategory[]>([]);
  authors = input<string[]>([]);
  totalCount = input<number>(0);
  filteredCount = input<number>(0);

  // Outputs
  filterChange = output<ObservationFilters>();
  exportReport = output<void>();

  filterForm = this.fb.group({
    categoryId: ['all'],
    author: [''],
    dateFrom: [''],
    dateTo: ['']
  });

  hasActiveFilters = signal(false);

  ngOnInit(): void {
    this.filterForm.valueChanges.subscribe(values => {
      const formattedValues: ObservationFilters = {
        categoryId: values.categoryId ?? 'all',
        author: values.author ?? '',
        dateFrom: values.dateFrom ?? '',
        dateTo: values.dateTo ?? ''
      };

      this.checkActiveFilters(formattedValues);
      this.filterChange.emit(formattedValues);
    });
  }

  private checkActiveFilters(f: ObservationFilters): void {
    const active = f.categoryId !== 'all' || f.author !== '' || f.dateFrom !== '' || f.dateTo !== '';
    this.hasActiveFilters.set(active);
  }

  clearFilters(): void {
    this.filterForm.reset({
      categoryId: 'all',
      author: '',
      dateFrom: '',
      dateTo: ''
    });
  }
}