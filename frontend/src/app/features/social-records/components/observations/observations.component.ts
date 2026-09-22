import { Component, inject, OnInit, signal, computed, input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ObservationCategoriesService } from '../../services/observation-categories.service';
import { ObservationCategory } from '../../interfaces/observation-category.interface';
import { Observation } from '../../interfaces/observation.interface';
import { PermissionService } from '../../../../core/auth/permission.service';
import { EmptyFieldBadgeComponent } from '../../../../shared/components/empty-field-badge/empty-field-badge.component';

@Component({
  selector: 'app-observations',
  standalone: true,
  imports: [DatePipe, EmptyFieldBadgeComponent],
  templateUrl: './observations.component.html',
  styleUrl: './observations.component.css'
})
export class ObservationsComponent implements OnInit {
  private categoriesService = inject(ObservationCategoriesService);
  public permissionService = inject(PermissionService);

  // Recibe las observaciones de la ficha social desde el componente padre
  observations = input<Observation[]>([]);

  // Signals para gestionar las categorías y la pestaña activa
  categories = signal<ObservationCategory[]>([]);
  activeTabId = signal<string>('all');
  loading = signal<boolean>(true);

  ngOnInit(): void {
    this.loadCategories();
  }

  loadCategories(): void {
    this.loading.set(true);
    // Cargamos todas las categorías para que las observaciones históricas de categorías inactivas sigan viéndose
    this.categoriesService.getAll().subscribe({
      next: (data) => {
        this.categories.set(data);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        console.error('Error al cargar categorías de observaciones:', err);
        this.loading.set(false);
      }
    });
  }

  // Filtrado reactivo de observaciones según la solapa activa
  filteredObservations = computed(() => {
    const list = this.observations() ?? [];
    const currentTab = this.activeTabId();

    if (currentTab === 'all') {
      return list;
    }

    return list.filter((obs) => obs.categoryId === currentTab);
  });

  selectTab(tabId: string): void {
    this.activeTabId.set(tabId);
  }

  // Devuelve la cantidad de observaciones que pertenecen a una categoría específica
  getCategoryCount(categoryId: string): number {
    const list = this.observations() ?? [];
    return list.filter((obs) => obs.categoryId === categoryId).length;
  }
}