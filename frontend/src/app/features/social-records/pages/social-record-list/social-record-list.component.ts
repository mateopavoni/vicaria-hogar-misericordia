import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { SocialRecordListItem} from '../../interfaces/social-record.interface';
import { SocialRecordsService } from '../../services/social-records.service';

@Component({
  selector: 'app-social-record-list',
  imports: [DatePipe, RouterLink],
  templateUrl: './social-record-list.component.html',
  styleUrl: './social-record-list.component.css'
})
export class SocialRecordListComponent {

  private socialRecordsService = inject(SocialRecordsService);

  records = signal<SocialRecordListItem[]>([]);

  loading = signal(false);

  errorMessage = signal<string | null>(null);

  currentPage = signal(1);

  totalPages = signal(1);


  ngOnInit(): void {
    this.loadRecords();
  }


  loadRecords(): void {

    this.loading.set(true);
    this.errorMessage.set(null);

    this.socialRecordsService.getAll(this.currentPage()) .subscribe({ next: (response) => {

          const sortedRecords = [...response.items].sort(
            (a, b) => {

              const nameA =
                `${a.lastName ?? ''} ${a.firstName}`.trim();

              const nameB =
                `${b.lastName ?? ''} ${b.firstName}`.trim();

              return nameA.localeCompare(
                nameB,
                'es',
                { sensitivity: 'base' }
              );
            }
          );

          this.records.set(sortedRecords);

          this.totalPages.set(response.totalPages);

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


  goToPage(page: number): void {

    if (
      page < 1 ||
      page > this.totalPages()
    ) {
      return;
    }

    this.currentPage.set(page);

    this.loadRecords();
  }

}