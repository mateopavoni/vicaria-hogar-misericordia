import { Component, inject, signal, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { SocialRecordListItem} from '../../interfaces/social-record.interface';
import { SocialRecordsService } from '../../services/social-records.service';
import {debounceTime,distinctUntilChanged,Subject,takeUntil} from 'rxjs';
import { OnDestroy } from '@angular/core';
@Component({
  selector: 'app-social-record-list',
  imports: [DatePipe, RouterLink],
  templateUrl: './social-record-list.component.html',
  styleUrl: './social-record-list.component.css'
})
export class SocialRecordListComponent
  implements OnInit, OnDestroy {

  private socialRecordsService =
    inject(SocialRecordsService);


  private destroy$ = new Subject<void>();


  records = signal<SocialRecordListItem[]>([]);

  loading = signal(false);

  searchTerm = signal('');

  errorMessage = signal<string | null>(null);

  currentPage = signal(1);

  totalPages = signal(1);


  // Texto que envía el buscador
  private searchSubject =
    new Subject<string>();


  ngOnInit(): void {

    this.loadRecords();


    this.searchSubject.pipe(
        debounceTime(300),
        distinctUntilChanged(),
        takeUntil(this.destroy$)
      )
      .subscribe(search => {

      this.searchTerm.set(search);

      this.currentPage.set(1);

      this.loadRecords(search);

    });

  }


  
  //  Carga el listado
  
  loadRecords(search: string = ''): void {

    this.loading.set(true);

    this.errorMessage.set(null);


    this.socialRecordsService.getAll( this.currentPage(), search ).subscribe({

        next: (response) => {

          const sortedRecords =
            [...response.items].sort(
              (a, b) => {

                const nameA =
                  `${a.lastName ?? ''} ${a.firstName}`.trim();

                const nameB =
                  `${b.lastName ?? ''} ${b.firstName}`.trim();

                return nameA.localeCompare(
                  nameB,
                  'es',
                  {
                    sensitivity: 'base'
                  }
                );

              }
            );


          this.records.set(
            sortedRecords
          );

          this.totalPages.set(
            response.totalPages
          );

          this.loading.set(false);

        },

        error: () => {

          this.loading.set(false);

          this.errorMessage.set(
            'No se pudieron cargar las fichas.'
          );

        }

      });

  }


 
  // Se ejecuta mientras escribimos
  
  onSearch(value: string): void {

  this.searchSubject.next(
    value.trim()
  );

}
    // Búsqueda al presionar Enter
  searchNow(value: string): void {

  const search = value.trim();

  this.searchTerm.set(search);

  this.currentPage.set(1);

  this.loadRecords(search);

}


  goToPage(page: number): void {

  if (
    page < 1 ||
    page > this.totalPages()
  ) {
    return;
  }

  this.currentPage.set(page);

  this.loadRecords(
    this.searchTerm()
  );

}


  ngOnDestroy(): void {

    this.destroy$.next();
    this.destroy$.complete();

  }

}