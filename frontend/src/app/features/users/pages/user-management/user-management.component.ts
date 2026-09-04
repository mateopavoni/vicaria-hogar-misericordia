import {Component,inject,signal, OnInit} from '@angular/core';
import { DatePipe } from '@angular/common';
import { ManagedUser,UserStatus} from '../../interfaces/user.interface';
import { UserRole } from '../../../../core/auth/userRole';
import { UsersService } from '../../services/users.service';
import { ApproveUserModalComponent } from '../../components/approve-user-modal/approve-user-modal.component';
import {  RejectUserModalComponent } from '../../components/reject-user-modal/reject-user-modal.component';
import { ChangeRoleModalComponent } from "../../components/change-role-modal/change-role-modal.component";


@Component({
  selector: 'app-user-management',
  imports: [DatePipe, ApproveUserModalComponent, RejectUserModalComponent, ChangeRoleModalComponent],
  templateUrl: './user-management.component.html',
  styleUrl: './user-management.component.css',
})
export class UserManagementComponent implements OnInit {

      private usersService = inject(UsersService);


      // users = signal<ManagedUser[]>([]);

      loading = signal(false);

      error = signal<string | null>(null);

      activeTab = signal<UserStatus>('Pending');

      currentPage = signal(1);

      totalPages = signal(1);

      selectedUser = signal<ManagedUser | null>(null);

      showApproveModal = signal(false);

      showRejectModal = signal(false);

      showChangeRoleModal = signal(false);
      
      dateFrom = signal('');

      dateTo = signal('');

      // cantidad real de cada tab, para mostrar en el contador
      pendingTotal = signal(0);
      activeTotal = signal(0);
      suspendedTotal = signal(0);

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

      // INACTIVAR, ACTIVAR Y REASIGNAR ROL
    
      deactivateUser(user: ManagedUser): void {
        if (confirm(`¿Estás seguro de que deseas inhabilitar/desactivar la cuenta de ${user.name} ${user.lastname}?`)) {
          // Llama a tu endpoint en el servicio para desactivar
          this.usersService.deactivateUser(user.id).subscribe({
            next: () => {
              this.loadUsers();
            },
            error: () => {
              this.error.set('No se pudo desactivar el usuario.');
            }
          });
        }
      }

      reactivateUser(user: ManagedUser): void {
        if (confirm(`¿Deseas reactivar la cuenta de ${user.name} ${user.lastname}?`)) {
          // Llama a tu endpoint en el servicio para reactivar con efecto inmediato
          this.usersService.reactivateUser(user.id).subscribe({
            next: () => {
              this.loadUsers();
            },
            error: () => {
              this.error.set('No se pudo reactivar el usuario.');
            }
          });
        }
      }

      openChangeRoleModal(user: ManagedUser): void {
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
          error: () => this.error.set('No se pudo cambiar el rol.'),
        });
      }
}