import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import {
  UiButtonComponent,
  UiCellDirective,
  UiColumn,
  UiConfirmDialogComponent,
  UiDataTableComponent,
  UiTab,
  UiTabsComponent,
} from '../../../../../shared/ui';
import { CategoryFormModalComponent } from '../../components/category-form-modal/category-form-modal.component';
import { CreateCategoryDto, ObservationCategory } from '../../interfaces/observation-category.interface';
import { ObservationCategoriesService } from '../../services/observation-categories.service';

type CategoryTab = 'active' | 'inactive';

/**
 * SCRUM-181: configuración de categorías de observaciones (solo Referente).
 * Crear / editar / activar / desactivar categorías compartidas por todo el equipo.
 * No hay botón de eliminar acá a propósito: el backend solo permite borrar una
 * categoría sin observaciones asociadas, y desactivar cubre el caso de uso real
 * (una categoría con historial nunca se puede borrar, solo desactivar).
 */
@Component({
  selector: 'app-category-settings',
  imports: [
    DatePipe,
    UiTabsComponent,
    UiDataTableComponent,
    UiCellDirective,
    UiButtonComponent,
    UiConfirmDialogComponent,
    CategoryFormModalComponent,
  ],
  templateUrl: './category-settings.component.html',
})
export class CategorySettingsComponent implements OnInit {
  private categoriesService = inject(ObservationCategoriesService);

  categories = signal<ObservationCategory[]>([]);
  loading = signal(false);
  errorMessage = signal<string | null>(null);

  activeTab = signal<CategoryTab>('active');

  activeCount = computed(() => this.categories().filter(c => c.isActive).length);
  inactiveCount = computed(() => this.categories().filter(c => !c.isActive).length);

  tabs = computed<UiTab<CategoryTab>[]>(() => [
    { id: 'active', label: 'Activas', count: this.activeCount() },
    { id: 'inactive', label: 'Inactivas', count: this.inactiveCount(), tone: 'neutral' },
  ]);

  // las inactivas siguen existiendo (y visibles acá, y en observaciones históricas que
  // ya las usaban) pero no deben ofrecerse al cargar una observación nueva (ver AC)
  visibleCategories = computed(() =>
    this.categories().filter(c => (this.activeTab() === 'active' ? c.isActive : !c.isActive)),
  );

  columns: UiColumn[] = [
    { key: 'name', header: 'Nombre' },
    { key: 'description', header: 'Descripción' },
    { key: 'createdAt', header: 'Creada' },
    { key: 'actions', header: 'Acciones', align: 'center' },
  ];

  // modal de alta/edición: null = cerrado; category=null dentro del modal = alta
  showFormModal = signal(false);
  editingCategory = signal<ObservationCategory | null>(null);
  saving = signal(false);
  formError = signal<string | null>(null);

  // confirmación antes de desactivar (reemplaza al confirm() nativo del navegador)
  pendingDeactivate = signal<ObservationCategory | null>(null);
  togglingStatus = signal(false);

  ngOnInit(): void {
    this.loadCategories();
  }

  changeTab(tab: CategoryTab): void {
    this.activeTab.set(tab);
  }

  loadCategories(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    // trae activas e inactivas juntas (onlyActive=false): esta pantalla necesita
    // mostrar ambos grupos, a diferencia del selector de "nueva observación" que
    // solo pide las activas (getActive()).
    this.categoriesService.getAll().subscribe({
      next: (categories) => {
        this.categories.set(categories);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.errorMessage.set('No se pudieron cargar las categorías.');
      },
    });
  }

  openCreateModal(): void {
    this.editingCategory.set(null);
    this.formError.set(null);
    this.showFormModal.set(true);
  }

  openEditModal(category: ObservationCategory): void {
    this.editingCategory.set(category);
    this.formError.set(null);
    this.showFormModal.set(true);
  }

  closeFormModal(): void {
    this.showFormModal.set(false);
  }

  saveCategory(dto: CreateCategoryDto): void {
    this.saving.set(true);
    this.formError.set(null);

    const editing = this.editingCategory();
    const request$ = editing
      ? this.categoriesService.update(editing.id, dto)
      : this.categoriesService.create(dto);

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.showFormModal.set(false);
        this.loadCategories();
      },
      error: (err) => {
        this.saving.set(false);
        this.formError.set(
          err?.error?.message || 'No se pudo guardar la categoría.'
        );
      },
    });
  }

  // activar no es una acción destructiva: se hace directo, sin confirmación
  activate(category: ObservationCategory): void {
    this.errorMessage.set(null);
    this.categoriesService.toggleActive(category.id, true).subscribe({
      next: () => this.loadCategories(),
      error: (err) => {
        this.errorMessage.set(err?.error?.message || 'No se pudo activar la categoría.');
      },
    });
  }

  // desactivar sí se confirma: deja de ofrecerse en observaciones nuevas
  requestDeactivate(category: ObservationCategory): void {
    this.pendingDeactivate.set(category);
  }

  cancelDeactivate(): void {
    this.pendingDeactivate.set(null);
  }

  confirmDeactivate(): void {
    const category = this.pendingDeactivate();
    if (!category) {
      return;
    }

    this.togglingStatus.set(true);
    this.categoriesService.toggleActive(category.id, false).subscribe({
      next: () => {
        this.togglingStatus.set(false);
        this.pendingDeactivate.set(null);
        this.loadCategories();
      },
      error: (err) => {
        this.togglingStatus.set(false);
        this.pendingDeactivate.set(null);
        this.errorMessage.set(err?.error?.message || 'No se pudo desactivar la categoría.');
      },
    });
  }
}
