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
export class SocialRecordListComponent implements OnInit, OnDestroy {
  private socialRecordsService = inject(SocialRecordsService);
  private destroy$ = new Subject<void>();

  records = signal<SocialRecordListItem[]>([]);
  loading = signal(false);
  errorMessage = signal<string | null>(null);

  currentPage = signal(1);
  totalPages = signal(1);
  searchTerm = signal(''); // Guarda el término de búsqueda activo

  private searchSubject = new Subject<string>();

  ngOnInit(): void {
    // 1. Carga inicial
    this.loadRecords();

    // 2. Escucha la escritura continua en el input
    this.searchSubject
      .pipe(
        debounceTime(300), // Espera 300ms a que el usuario deje de tipear
        distinctUntilChanged(), // Solo busca si el texto cambió respecto a la última búsqueda
        takeUntil(this.destroy$)
      )
      .subscribe((search) => {
        this.executeSearch(search);
      });
  }

  /**
   * Carga los registros usando la página y el término de búsqueda almacenados
   */
  loadRecords(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.socialRecordsService
      .getAll(this.currentPage(), this.searchTerm())
      .subscribe({
        next: (response) => {
          const sortedRecords = [...response.items].sort((a, b) => {
            const nameA = `${a.lastName ?? ''} ${a.firstName}`.trim();
            const nameB = `${b.lastName ?? ''} ${b.firstName}`.trim();
            return nameA.localeCompare(nameB, 'es', { sensitivity: 'base' });
          });

          this.records.set(sortedRecords);
          this.totalPages.set(response.totalPages);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.errorMessage.set('No se pudieron cargar las fichas.');
        }
      });
  }

  /**
   * Se ejecuta en el HTML en el evento (input)="onSearch(searchInput.value)"
   */
  onSearch(value: string): void {
    this.searchSubject.next(value);
  }

  /**
   * Se ejecuta al presionar Enter o hacer clic en el botón Buscar
   */
  searchNow(value: string): void {
    // Si el término es distinto al guardado actualmente, ejecuta la búsqueda de inmediato
    if (value !== this.searchTerm()) {
      this.executeSearch(value);
    }
  }

  /**
   * Aplica el término de búsqueda, resetea a la página 1 y carga datos
   */
  private executeSearch(value: string): void {
    this.searchTerm.set(value);
    this.currentPage.set(1);
    this.loadRecords();
  }

  /**
   * Cambia de página preservando el término de búsqueda actual
   */
  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages()) {
      return;
    }

    this.currentPage.set(page);
    this.loadRecords();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}