import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import {
  UiButtonComponent,
  UiCellDirective,
  UiColumn,
  UiConfirmDialogComponent,
  UiDataTableComponent,
  UiTab,
  UiTabsComponent,
} from '../../../../shared/ui';
import { CollaboratorFormModalComponent } from '../../components/collaborator-form-modal/collaborator-form-modal.component';
import { CollaboratorDetailModalComponent } from '../../components/collaborator-detail-modal/collaborator-detail-modal.component';
import { CollaboratorFiltersComponent } from '../../components/collaborator-filters/collaborator-filters.component';
import { CollaboratorFilters } from '../../interfaces/collaborator-filters.interface';
import { Collaborator, CollaboratorType, CreateCollaboratorDto } from '../../interfaces/collaborator.interface';
import { CollaboratorsService } from '../../services/collaborators.service';

type CollaboratorTab = 'active' | 'inactive';

/**
 * SCRUM-29 (SCRUM-201/SCRUM-202/SCRUM-206/SCRUM-207): listado y gestión de
 * colaboradores (voluntarios y empleados). Alta/edición con solo el nombre
 * obligatorio, baja y reactivación con confirmación (no se elimina del historial),
 * búsqueda por nombre/apellido/área (insensible a mayúsculas y acentos, con
 * debounce) y filtro por tipo, estado vacío con alta rápida precargada, y ficha
 * completa de solo lectura al hacer clic en un resultado.
 */
@Component({
  selector: 'app-collaborator-management',
  imports: [
    DatePipe,
    UiTabsComponent,
    UiDataTableComponent,
    UiCellDirective,
    UiButtonComponent,
    UiConfirmDialogComponent,
    CollaboratorFormModalComponent,
    CollaboratorDetailModalComponent,
    CollaboratorFiltersComponent,
  ],
  templateUrl: './collaborator-management.component.html',
})
export class CollaboratorManagementComponent implements OnInit, OnDestroy {
  private collaboratorsService = inject(CollaboratorsService);
  private destroy$ = new Subject<void>();
  private searchSubject = new Subject<string>();

  collaborators = signal<Collaborator[]>([]);
  loading = signal(false);
  errorMessage = signal<string | null>(null);

  activeTab = signal<CollaboratorTab>('active');

  // SCRUM-206: término de búsqueda aplicado (ya debounceado) y filtro por tipo
  // (mismo patrón que fichas: searchTerm/filters separados de lo que el usuario
  // está tipeando, para no recalcular en cada letra sin el debounce)
  searchTerm = signal('');
  filters = signal<CollaboratorFilters>({ type: null });

  readonly CollaboratorType = CollaboratorType;

  activeCount = computed(() => this.collaborators().filter(c => c.isActive).length);
  inactiveCount = computed(() => this.collaborators().filter(c => !c.isActive).length);

  tabs = computed<UiTab<CollaboratorTab>[]>(() => [
    { id: 'active', label: 'Activos', count: this.activeCount() },
    { id: 'inactive', label: 'Inactivos', count: this.inactiveCount(), tone: 'neutral' },
  ]);

  // SCRUM-19: busca por nombre, apellido o área desde un único campo, insensible a
  // mayúsculas y acentos; además filtra por tipo y por la tab activa/inactiva.
  visibleCollaborators = computed(() => {
    const term = this.normalize(this.searchTerm().trim());
    const type = this.filters().type;

    return this.collaborators().filter((c) => {
      const matchesTab = this.activeTab() === 'active' ? c.isActive : !c.isActive;
      if (!matchesTab) return false;

      if (type !== null && c.type !== type) return false;

      if (term.length > 0) {
        const haystack = this.normalize(`${c.firstName} ${c.lastName ?? ''} ${c.workArea ?? ''}`);
        if (!haystack.includes(term)) return false;
      }

      return true;
    });
  });

  // SCRUM-207: si hay una búsqueda activa y no encontró nada, se muestra el estado
  // vacío con la opción de registrar en vez de la tabla (que tiene su propio mensaje
  // genérico para cuando simplemente no hay colaboradores activos/inactivos).
  showEmptySearchState = computed(
    () => this.searchTerm().trim().length > 0 && !this.loading() && this.visibleCollaborators().length === 0,
  );

  columns: UiColumn[] = [
    { key: 'name', header: 'Nombre' },
    { key: 'contact', header: 'Contacto' },
    { key: 'type', header: 'Tipo' },
    { key: 'workArea', header: 'Área' },
    { key: 'status', header: 'Estado' },
    { key: 'createdAt', header: 'Alta' },
    { key: 'actions', header: 'Acciones', align: 'center' },
  ];

  // modal de alta/edición: null = cerrado; collaborator=null dentro del modal = alta
  showFormModal = signal(false);
  editingCollaborator = signal<Collaborator | null>(null);
  createPrefillName = signal<string | null>(null);
  saving = signal(false);
  formError = signal<string | null>(null);

  // ficha de solo lectura (SCRUM-207)
  viewingCollaborator = signal<Collaborator | null>(null);

  // confirmación antes de dar de baja o reactivar
  pendingToggle = signal<Collaborator | null>(null);
  togglingStatus = signal(false);

  ngOnInit(): void {
    this.loadCollaborators();

    this.searchSubject
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe((value) => this.searchTerm.set(value));
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  changeTab(tab: CollaboratorTab): void {
    this.activeTab.set(tab);
  }

  // (input): dispara el debounce. (keydown.enter)/botón "Buscar": busca ya, igual que en fichas.
  onSearchInput(value: string): void {
    this.searchSubject.next(value);
  }

  searchNow(value: string): void {
    if (value !== this.searchTerm()) {
      this.searchTerm.set(value);
    }
  }

  hasActiveFilters(): boolean {
    return this.filters().type !== null;
  }

  applyFilters(filters: CollaboratorFilters): void {
    this.filters.set(filters);
  }

  removeTypeFilter(): void {
    this.filters.set({ type: null });
  }

  clearFilters(): void {
    this.filters.set({ type: null });
  }

  loadCollaborators(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.collaboratorsService.getAll().subscribe({
      next: (collaborators) => {
        this.collaborators.set(collaborators);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.errorMessage.set('No se pudieron cargar los colaboradores.');
      },
    });
  }

  typeLabel(type: CollaboratorType | null | undefined): string {
    if (type === CollaboratorType.Volunteer) return 'Voluntario';
    if (type === CollaboratorType.Employee) return 'Empleado';
    return '—';
  }

  openCreateModal(): void {
    this.editingCollaborator.set(null);
    this.createPrefillName.set(null);
    this.formError.set(null);
    this.showFormModal.set(true);
  }

  // SCRUM-207: desde el estado vacío de la búsqueda, precarga el nombre tipeado
  openCreateModalFromSearch(): void {
    this.editingCollaborator.set(null);
    this.createPrefillName.set(this.searchTerm().trim());
    this.formError.set(null);
    this.showFormModal.set(true);
  }

  openEditModal(collaborator: Collaborator): void {
    this.editingCollaborator.set(collaborator);
    this.createPrefillName.set(null);
    this.formError.set(null);
    this.showFormModal.set(true);
  }

  closeFormModal(): void {
    this.showFormModal.set(false);
  }

  saveCollaborator(dto: CreateCollaboratorDto): void {
    this.saving.set(true);
    this.formError.set(null);

    const editing = this.editingCollaborator();
    const request$ = editing
      ? this.collaboratorsService.update(editing.id, dto)
      : this.collaboratorsService.create(dto);

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.showFormModal.set(false);
        this.loadCollaborators();
      },
      error: (err) => {
        this.saving.set(false);
        this.formError.set(
          err?.error?.message || 'No se pudo guardar el colaborador.'
        );
      },
    });
  }

  // SCRUM-207: ficha completa de solo lectura al hacer clic en un resultado
  openDetail(collaborator: Collaborator): void {
    this.viewingCollaborator.set(collaborator);
  }

  closeDetail(): void {
    this.viewingCollaborator.set(null);
  }

  editFromDetail(collaborator: Collaborator): void {
    this.viewingCollaborator.set(null);
    this.openEditModal(collaborator);
  }

  // tanto dar de baja como reactivar se confirman antes (SCRUM-202): es un cambio de
  // estado visible para todo el equipo, mejor evitar un click accidental
  requestToggle(collaborator: Collaborator): void {
    this.pendingToggle.set(collaborator);
  }

  cancelToggle(): void {
    this.pendingToggle.set(null);
  }

  confirmToggle(): void {
    const collaborator = this.pendingToggle();
    if (!collaborator) {
      return;
    }

    this.togglingStatus.set(true);
    this.collaboratorsService.toggleActive(collaborator.id, !collaborator.isActive).subscribe({
      next: () => {
        this.togglingStatus.set(false);
        this.pendingToggle.set(null);
        this.loadCollaborators();
      },
      error: (err) => {
        this.togglingStatus.set(false);
        this.pendingToggle.set(null);
        this.errorMessage.set(err?.error?.message || 'No se pudo actualizar el estado del colaborador.');
      },
    });
  }

  // SCRUM-19: "la búsqueda es insensible a mayúsculas y acentos"
  private normalize(value: string): string {
    return value
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .toLowerCase();
  }
}
