import { Component, inject, OnInit, signal, computed, input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ObservationCategoriesService } from '../../services/observation-categories.service';
import { ObservationCategory } from '../../interfaces/observation-category.interface';
import { Observation } from '../../interfaces/observation.interface';
import { PermissionService } from '../../../../core/auth/permission.service';
import { EmptyFieldBadgeComponent } from '../../../../shared/components/empty-field-badge/empty-field-badge.component';
import { CreateObservationModalComponent } from '../create-observation-modal/create-observation-modal.component';

@Component({
  selector: 'app-observations',
  standalone: true,
  imports: [DatePipe, EmptyFieldBadgeComponent, CreateObservationModalComponent],
  templateUrl: './observations.component.html',
  styleUrl: './observations.component.css'
})
export class ObservationsComponent implements OnInit {
  private categoriesService = inject(ObservationCategoriesService);
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

  // SCRUM-162: Inserta la nueva observación al tope de la lista sin recargar
  onObservationSaved(newObservation: Observation): void {
    this.observationsList.update(current => [newObservation, ...current]);
    this.showCreateModal.set(false);
  }

  // Filtrado reactivo por tab
  filteredObservations = computed(() => {
    const list = this.observationsList();
    const currentTab = this.activeTabId();

    if (currentTab === 'all') {
      return list;
    }

    return list.filter((obs) => obs.categoryId === currentTab);
  });

  selectTab(tabId: string): void {
    this.activeTabId.set(tabId);
  }

  getCategoryCount(categoryId: string): number {
    return this.observationsList().filter((obs) => obs.categoryId === categoryId).length;
  }
}