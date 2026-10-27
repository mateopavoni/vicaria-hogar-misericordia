import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { extractApiError } from '../../../../core/http/api-error';
import { UiButtonComponent, UiConfirmDialogComponent, UiTab, UiTabsComponent } from '../../../../shared/ui';
import { CreateCategoryDto, ObservationCategory } from '../../../social-records/interfaces/observation-category.interface';
import { ObservationCategoriesService } from '../../../social-records/services/observation-categories.service';
import { CategoryFormModalComponent } from '../../components/category-form-modal/category-form-modal.component';
import { CategoryFilter, countByStatus, filterCategories } from '../../utils/category-filter.util';

/**
 * SCRUM-180 (front de SCRUM-72): configuración de categorías de observaciones
 * (/dashboard/configuracion/categorias). SMART, solo Referente (permiso categorias.manage).
 * Crear, editar, activar/desactivar y eliminar (solo si no tiene observaciones: el backend
 * lo valida y responde con el motivo). Las desactivadas no se ofrecen al cargar observaciones
 * nuevas pero siguen viéndose en las históricas.
 */
@Component({
  selector: 'app-observation-categories',
  imports: [UiButtonComponent, UiTabsComponent, UiConfirmDialogComponent, CategoryFormModalComponent],
  templateUrl: './observation-categories.component.html',
})
export class ObservationCategoriesComponent implements OnInit {
  private categoriesService = inject(ObservationCategoriesService);

  categories = signal<ObservationCategory[]>([]);
  loading = signal(true);
  filter = signal<CategoryFilter>('all');
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  private successTimeout: ReturnType<typeof setTimeout> | null = null;

  counts = computed(() => countByStatus(this.categories()));
  visible = computed(() => filterCategories(this.categories(), this.filter()));
  tabs = computed<UiTab<CategoryFilter>[]>(() => [
    { id: 'all', label: 'Todas', count: this.counts().all },
    { id: 'active', label: 'Activas', count: this.counts().active },
    { id: 'inactive', label: 'Inactivas', count: this.counts().inactive },
  ]);

  // modal de alta / edición
  showForm = signal(false);
  editing = signal<ObservationCategory | null>(null);
  saving = signal(false);
  formError = signal<string | null>(null);

  // confirmaciones
  pendingDeactivate = signal<ObservationCategory | null>(null);
  pendingDelete = signal<ObservationCategory | null>(null);
  busy = signal(false);

  ngOnInit(): void {
    this.reload();
  }

  private reload(): void {
    this.categoriesService.getAll().subscribe({
      next: (list) => {
        this.categories.set(list);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(extractApiError(err, 'No se pudieron cargar las categorías.'));
      },
    });
  }

  openCreate(): void {
    this.editing.set(null);
    this.formError.set(null);
    this.showForm.set(true);
  }

  openEdit(category: ObservationCategory): void {
    this.editing.set(category);
    this.formError.set(null);
    this.showForm.set(true);
  }

  save(dto: CreateCategoryDto): void {
    if (this.saving()) return;
    const editing = this.editing();
    this.saving.set(true);
    this.formError.set(null);

    const request = editing ? this.categoriesService.update(editing.id, dto) : this.categoriesService.create(dto);
    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.showForm.set(false);
        this.showSuccess(editing ? 'Categoría actualizada.' : `Categoría "${dto.name}" creada.`);
        this.reload();
      },
      error: (err) => {
        this.saving.set(false);
        this.formError.set(extractApiError(err, 'No se pudo guardar la categoría.'));
      },
    });
  }

  // desactivar pide confirmación (deja de ofrecerse); activar es directo
  requestToggle(category: ObservationCategory): void {
    if (category.isActive) {
      this.pendingDeactivate.set(category);
    } else {
      this.setActive(category, true);
    }
  }

  confirmDeactivate(): void {
    const category = this.pendingDeactivate();
    if (category) this.setActive(category, false);
  }

  private setActive(category: ObservationCategory, isActive: boolean): void {
    if (this.busy()) return;
    this.busy.set(true);
    this.errorMessage.set(null);
    this.categoriesService.toggleActive(category.id, isActive).subscribe({
      next: () => {
        this.busy.set(false);
        this.pendingDeactivate.set(null);
        this.showSuccess(isActive ? `"${category.name}" activada.` : `"${category.name}" desactivada.`);
        this.reload();
      },
      error: (err) => {
        this.busy.set(false);
        this.pendingDeactivate.set(null);
        this.errorMessage.set(extractApiError(err, 'No se pudo cambiar el estado de la categoría.'));
      },
    });
  }

  confirmDelete(): void {
    const category = this.pendingDelete();
    if (!category || this.busy()) return;
    this.busy.set(true);
    this.errorMessage.set(null);
    this.categoriesService.delete(category.id).subscribe({
      next: () => {
        this.busy.set(false);
        this.pendingDelete.set(null);
        this.showSuccess(`Categoría "${category.name}" eliminada.`);
        this.reload();
      },
      error: (err) => {
        this.busy.set(false);
        this.pendingDelete.set(null);
        // 409 si tiene observaciones: el backend explica que hay que desactivarla
        this.errorMessage.set(extractApiError(err, 'No se pudo eliminar la categoría.'));
      },
    });
  }

  private showSuccess(message: string): void {
    this.successMessage.set(message);
    if (this.successTimeout) clearTimeout(this.successTimeout);
    this.successTimeout = setTimeout(() => this.successMessage.set(null), 4000);
  }
}
