import {Component,inject,signal,computed, OnInit} from '@angular/core';
import { DatePipe } from '@angular/common';
import { ManagedUser,UserStatus} from '../../interfaces/user.interface';
import { UserRole } from '../../../../core/auth/models/user-role';
import { UsersService } from '../../services/users.service';
import { ApproveUserModalComponent } from '../../components/approve-user-modal/approve-user-modal.component';
import {  RejectUserModalComponent } from '../../components/reject-user-modal/reject-user-modal.component';
import { ChangeRoleModalComponent } from "../../components/change-role-modal/change-role-modal.component";
import { AuthService } from '../../../../core/auth/auth.service';
import {
  UiButtonComponent,
  UiCellDirective,
  UiColumn,
  UiConfirmDialogComponent,
  UiDataTableComponent,
  UiFormFieldComponent,
  UiInputDirective,
  UiPaginationComponent,
  UiTab,
  UiTabsComponent,
} from '../../../../shared/ui';

// acción destructiva/sensible pendiente de confirmación en el modal (reemplaza a confirm() del navegador)
type PendingUserAction =
  | { type: 'deactivate'; user: ManagedUser }
  | { type: 'reactivate'; user: ManagedUser };

@Component({
  selector: 'app-user-management',
  imports: [
    DatePipe,
    ApproveUserModalComponent,
    RejectUserModalComponent,
    ChangeRoleModalComponent,
    UiTabsComponent,
    UiPaginationComponent,
    UiDataTableComponent,
    UiCellDirective,
    UiButtonComponent,
    UiFormFieldComponent,
    UiInputDirective,
    UiConfirmDialogComponent,
  ],
  templateUrl: './user-management.component.html',
})
export class UserManagementComponent implements OnInit {

      private usersService = inject(UsersService);
      private authService = inject(AuthService);


      // users = signal<ManagedUser[]>([]);

      loading = signal(false);

      error = signal<string | null>(null);

      activeTab = signal<UserStatus>('Pending');

      currentPage = signal(1);

      totalPages = signal(1);

      pageNumbers = computed(() => Array.from({ length: this.totalPages() }, (_, i) => i + 1));

      selectedUser = signal<ManagedUser | null>(null);

      showApproveModal = signal(false);

      showRejectModal = signal(false);

      showChangeRoleModal = signal(false);

      // acción pendiente de confirmar en ui-confirm-dialog (inactivar/reactivar); null = sin modal abierto
      pendingAction = signal<PendingUserAction | null>(null);
      confirmingAction = signal(false);


      dateFrom = signal('');

      dateTo = signal('');

      // tope para los filtros de fecha, no tiene sentido filtrar por fechas futuras
      today = new Date().toISOString().split('T')[0];

      // cantidad real de cada tab, para mostrar en el contador
      pendingTotal = signal(0);
      activeTotal = signal(0);
      suspendedTotal = signal(0);

      // columnas de ui-data-table (name/role/requestDate/actions tienen plantilla propia, ver el .html)
      columns: UiColumn[] = [
        { key: 'name', header: 'Nombre y Apellido' },
        { key: 'email', header: 'Email' },
        { key: 'role', header: 'Rol' },
        { key: 'requestDate', header: 'Fecha' },
        { key: 'actions', header: 'Acciones', align: 'center' },
      ];

      // pestañas de ui-tabs, con los mismos contadores que ya se cargaban
      tabs = computed<UiTab<UserStatus>[]>(() => [
        { id: 'Pending', label: 'Pendientes', count: this.pendingTotal(), tone: 'alert' },
        { id: 'Approved', label: 'Activos', count: this.activeTotal() },
        { id: 'Suspended', label: 'Inactivos / Suspendidos', count: this.suspendedTotal() },
      ]);

      // texto del ui-confirm-dialog según la acción pendiente (inactivar/reactivar)
      pendingActionDialog = computed(() => {
        const action = this.pendingAction();
        if (!action) {
          return null;
        }

        const fullName = `${action.user.name} ${action.user.lastname}`;

        return action.type === 'deactivate'
          ? {
              title: 'Inactivar usuario',
              message: `¿Estás seguro de que deseas inhabilitar/desactivar la cuenta de ${fullName}?`,
              confirmLabel: 'Inactivar',
              confirmVariant: 'warning' as const,
            }
          : {
              title: 'Reactivar usuario',
              message: `¿Deseas reactivar la cuenta de ${fullName}?`,
              confirmLabel: 'Reactivar',
              confirmVariant: 'info' as const,
            };
      });

      ngOnInit(): void {
        this.loadUsers();
        // precargamos los otros dos conteos aunque no estemos parados en ese tab
        this.usersService.getUsers('Approved', 1).subscribe((res) => this.activeTotal.set(res.total));
        this.usersService.getUsers('Suspended', 1).subscribe((res) => this.suspendedTotal.set(res.total));
      }

      users = signal<ManagedUser[]>([]);


        loadUsers(): void {this.loading.set(true);this.error.set(null);this.usersService.getUsers(this.activeTab(),this.currentPage(),
          {
            dateFrom: this.dateFrom() || undefined,
            dateTo: this.dateTo() || undefined
          }
        )
        .subscribe({

          next: (response) => {

            this.users.set(response.items);

            this.totalPages.set(
              response.totalPages
            );

            // actualizamos el contador del tab que se acaba de cargar
            if (this.activeTab() === 'Pending') {
              this.pendingTotal.set(response.total);
            } else if (this.activeTab() === 'Approved') {
              this.activeTotal.set(response.total);
            } else if (this.activeTab() === 'Suspended') {
              this.suspendedTotal.set(response.total);
            }

            this.loading.set(false);
          },

          error: () => {

            this.loading.set(false);

            this.error.set(
              'No se pudieron cargar los usuarios.'
            );
          }

        });
    }

      setDateFrom(value: string): void { this.dateFrom.set(value);}
      
      setDateTo(value: string): void {this.dateTo.set(value);}


      applyFilters(): void {
        if (!this.dateFrom() && !this.dateTo()) return; // nada cargado en los calendarios, no hay nada que filtrar

        this.currentPage.set(1);
        this.loadUsers();

        }

      clearFilters(): void {
        if (!this.dateFrom() && !this.dateTo()) return; // ya estaba limpio, no hay nada que limpiar

        this.dateFrom.set('');
        this.dateTo.set('');

        this.currentPage.set(1);

        this.loadUsers();
      }


      changeTab( status: UserStatus): void {

        this.activeTab.set(status);

        this.currentPage.set(1);

        this.loadUsers();

      }

    

      goToPage( page: number): void {

        if (
          page < 1 ||
          page > this.totalPages()
        ) {
          return;
        }

        this.currentPage.set(page);

        this.loadUsers();

      }


      openApproveModal( user: ManagedUser): void {
        this.closeModals(); // por si había otro modal abierto, que no se pisen

        this.selectedUser.set(user);

        this.showApproveModal.set(true);

      }


      openRejectModal(user: ManagedUser): void {
        this.closeModals(); // por si había otro modal abierto, que no se pisen

        this.selectedUser.set(user);

        this.showRejectModal.set(true);

      }


      closeModals(): void {

        this.showApproveModal.set(false);

        this.showRejectModal.set(false);

        this.showChangeRoleModal.set(false);

        this.selectedUser.set(null);

      }

      approveSelectedUser(role: UserRole): void {

      const user = this.selectedUser();

      if (!user) {
        return;
      }
        this.usersService.approveUser(user.id, { role }).subscribe({next: () => {

              this.closeModals();
              this.loadUsers();
            },

            error: () => {
              this.error.set(
                'No se pudo aprobar el usuario.'
              );
            }

          });
    }

    rejectSelectedUser(reason: string): void {

      const user = this.selectedUser();

      if (!user) {
        return;
      }

      this.usersService.rejectUser(user.id, { reason }).subscribe({next: () => {

            this.closeModals();
            this.loadUsers();
          },

          error: () => {
            
            this.error.set(
              'No se pudo rechazar la solicitud.'
            );

          }

        });
    }

      esUsuarioActual(user: ManagedUser): boolean {
        return user.id === this.authService.user()?.id;
      }

      // INACTIVAR, ACTIVAR Y REASIGNAR ROL

      // antes usaban confirm() nativo del navegador; ahora abren ui-confirm-dialog
      // (ver confirmPendingAction/cancelPendingAction) y la llamada al servicio se hace recién al confirmar.
      deactivateUser(user: ManagedUser): void {
        if (user.id === this.authService.user()?.id) {
          this.error.set('No podés desactivar tu propia cuenta.');
          return;
        }
        this.pendingAction.set({ type: 'deactivate', user });
      }

      reactivateUser(user: ManagedUser): void {
        this.pendingAction.set({ type: 'reactivate', user });
      }

      cancelPendingAction(): void {
        this.pendingAction.set(null);
      }

      confirmPendingAction(): void {
        const action = this.pendingAction();
        if (!action) {
          return;
        }

        const { type, user } = action;
        this.confirmingAction.set(true);

        const request$ =
          type === 'deactivate'
            ? this.usersService.deactivateUser(user.id)
            : this.usersService.reactivateUser(user.id);

        request$.subscribe({
          next: () => {
            this.confirmingAction.set(false);
            this.pendingAction.set(null);
            this.loadUsers();
          },
          error: (err) => {
            this.confirmingAction.set(false);
            this.pendingAction.set(null);
            const fallback =
              type === 'deactivate' ? 'No se pudo desactivar el usuario.' : 'No se pudo reactivar el usuario.';
            this.error.set(err?.error?.message || fallback);
          }
        });
      }

      openChangeRoleModal(user: ManagedUser): void {
        if (user.id === this.authService.user()?.id) {
          this.error.set('No podés cambiar tu propio rol.');
          return;
        }
        this.closeModals(); // por si había otro modal abierto, que no se pisen
        this.selectedUser.set(user);
        this.showChangeRoleModal.set(true);
      }

      updateUserRole(role: UserRole): void {
        const user = this.selectedUser();
        if (!user) return;

        this.usersService.updateRole(user.id, role).subscribe({
          next: () => {
            this.closeModals();
            this.loadUsers();
          },
          error: (err) => this.error.set(err?.error?.message || 'No se pudo cambiar el rol.'),
        });
      }
}