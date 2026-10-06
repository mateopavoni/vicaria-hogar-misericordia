import { Component, inject, OnInit, signal, computed, input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ObservationCategoriesService } from '../../services/observation-categories.service';
import { ObservationsService } from '../../services/observations.service';
import { ObservationCategory } from '../../interfaces/observation-category.interface';
import { Observation } from '../../interfaces/observation.interface';
import { PermissionService } from '../../../../core/auth/permission.service';
import { EmptyFieldBadgeComponent } from '../../../../shared/components/empty-field-badge/empty-field-badge.component';
import { CreateObservationModalComponent } from '../create-observation-modal/create-observation-modal.component';
import { ObservationFilters } from '../../interfaces/observation-filter.interface';
import { ObservationFiltersComponent } from '../observation-filters/observation-filters.component';
import { SuccessModalComponent } from '../../../../shared/components/success-modal/success-modal.component';



@Component({
  selector: 'app-observations',
  standalone: true,
  imports: [DatePipe, EmptyFieldBadgeComponent, CreateObservationModalComponent, ObservationFiltersComponent, SuccessModalComponent],
  templateUrl: './observations.component.html',
  styleUrl: './observations.component.css'
})
export class ObservationsComponent implements OnInit {
  private categoriesService = inject(ObservationCategoriesService);
  private observationsService = inject(ObservationsService);
  public permissionService = inject(PermissionService);

  // Recibe ID de la ficha social y lista inicial de observaciones
  socialRecordId = input.required<string>();
  initialObservations = input<Observation[]>([]);

  // Internal Signals
  observationsList = signal<Observation[]>([]);
  categories = signal<ObservationCategory[]>([]);
  activeTabId = signal<string>('all');
  loading = signal<boolean>(true);
  showCreateModal = signal<boolean>(false);
  // bug reportado 2026-09-23: no había ningún feedback al guardar una observación
  showSuccessModal = signal<boolean>(false);
  exporting = signal<boolean>(false);
  exportError = signal<string | null>(null);


  // Estado del filtro actual recibido del hijo
  activeFilters = signal<ObservationFilters>({
    categoryId: 'all',
    author: '',
    dateFrom: '',
    dateTo: ''
  });


  ngOnInit(): void {
    // Inicializar lista con lo proveniente del input
    this.observationsList.set(this.initialObservations() ?? []);
    this.loadCategories();

  }

  loadCategories(): void {
    this.loading.set(true);
    this.categoriesService.getAll().subscribe({
      next: (data) => {
        this.categories.set(data);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        console.error('Error al cargar categorías:', err);
        this.loading.set(false);
      }
    });
  }

  // Mapea autores únicos para el selector de filtros
  authorsList = computed(() => {
    const list = this.observationsList();
    const authors = list.map(obs => obs.createdBy).filter((author): author is string => Boolean(author));
    return Array.from(new Set(authors));
  });

  // Filtra y ordena cronológicamente de forma descendente
  filteredObservations = computed(() => {
  let list = [...this.observationsList()];
  const f = this.activeFilters();

    // 1. Categoria
    if (f.categoryId && f.categoryId !== 'all') {
      list = list.filter(obs => obs.categoryId === f.categoryId);
    }

    // 2. Autor
    if (f.author && f.author.trim() !== '') {
      list = list.filter(obs => obs.createdBy?.toLowerCase().includes(f.author.toLowerCase()));
    }

    // 3. Fecha Desde
    if (f.dateFrom) {
      const fromDate = new Date(f.dateFrom);
      fromDate.setHours(0, 0, 0, 0);
      list = list.filter(obs => new Date(obs.createdAt) >= fromDate);
    }

    // 4. Fecha Hasta
    if (f.dateTo) {
      const toDate = new Date(f.dateTo);
      toDate.setHours(23, 59, 59, 999);
      list = list.filter(obs => new Date(obs.createdAt) <= toDate);
    }

    // Ordenar de más reciente a más antigua
    return list.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
  });


  onFilterChange(filters: ObservationFilters): void {
    this.activeFilters.set(filters);
  }

   // SCRUM-162: Inserta la nueva observación al tope de la lista sin recargar
    onObservationSaved(newObservation: Observation): void {
    this.observationsList.update(current => [newObservation, ...current]);
    this.showCreateModal.set(false);
    this.showSuccessModal.set(true);
  }

  // bug reportado 2026-09-23: el botón "Exportar" no llamaba al endpoint (ya existente),
  // solo hacía console.log. El filtro por autor no se manda: es por nombre en el front,
  // el backend filtra por id de usuario.
  exportReport(): void {
    const f = this.activeFilters();
    this.exportError.set(null);
    this.exporting.set(true);

    this.observationsService.exportCsv(this.socialRecordId(), {
      categoryId: f.categoryId && f.categoryId !== 'all' ? f.categoryId : null,
      dateFrom: f.dateFrom || null,
      dateTo: f.dateTo || null,
    }).subscribe({
      next: (blob) => {
        this.exporting.set(false);
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `observaciones_${this.socialRecordId()}.csv`;
        link.click();
        URL.revokeObjectURL(url);
      },
      error: () => {
        this.exporting.set(false);
        this.exportError.set('No se pudo exportar el reporte. Intentá nuevamente.');
      }
    });
  }

}