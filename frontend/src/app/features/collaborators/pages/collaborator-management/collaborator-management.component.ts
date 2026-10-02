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
} from '../../../../shared/ui';
import { CollaboratorFormModalComponent } from '../../components/collaborator-form-modal/collaborator-form-modal.component';
import { Collaborator, CollaboratorType, CreateCollaboratorDto } from '../../interfaces/collaborator.interface';
import { CollaboratorsService } from '../../services/collaborators.service';

type CollaboratorTab = 'active' | 'inactive';

/**
 * SCRUM-29 (SCRUM-201/SCRUM-202): listado y gestión de colaboradores (voluntarios y
 * empleados). Alta/edición con solo el nombre obligatorio, baja y reactivación con
 * confirmación (no se elimina del historial), y badge + fecha de alta + quién registró
 * cada colaborador en el listado.
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
  ],
  templateUrl: './collaborator-management.component.html',
})
export class CollaboratorManagementComponent implements OnInit {
  private collaboratorsService = inject(CollaboratorsService);

  collaborators = signal<Collaborator[]>([]);
  loading = signal(false);
  errorMessage = signal<string | null>(null);

  activeTab = signal<CollaboratorTab>('active');

  activeCount = computed(() => this.collaborators().filter(c => c.isActive).length);
  inactiveCount = computed(() => this.collaborators().filter(c => !c.isActive).length);

  tabs = computed<UiTab<CollaboratorTab>[]>(() => [
    { id: 'active', label: 'Activos', count: this.activeCount() },
    { id: 'inactive', label: 'Inactivos', count: this.inactiveCount(), tone: 'neutral' },
  ]);

  visibleCollaborators = computed(() =>
    this.collaborators().filter(c => (this.activeTab() === 'active' ? c.isActive : !c.isActive)),
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
  saving = signal(false);
  formError = signal<string | null>(null);

  // confirmación antes de dar de baja o reactivar
  pendingToggle = signal<Collaborator | null>(null);
  togglingStatus = signal(false);

  ngOnInit(): void {
    this.loadCollaborators();
  }

  changeTab(tab: CollaboratorTab): void {
    this.activeTab.set(tab);
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
    this.formError.set(null);
    this.showFormModal.set(true);
  }

  openEditModal(collaborator: Collaborator): void {
    this.editingCollaborator.set(collaborator);
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
}
