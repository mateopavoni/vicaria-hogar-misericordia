import {Component,inject,signal, OnInit} from '@angular/core';
import { DatePipe } from '@angular/common';
import { ManagedUser,UserStatus} from '../../interfaces/user.interface';
import { UserRole } from '../../../../core/auth/userRole';
import { UsersService } from '../../services/users.service';
import { ApproveUserModalComponent } from '../../components/approve-user-modal/approve-user-modal.component';
import {  RejectUserModalComponent } from '../../components/reject-user-modal/reject-user-modal.component';


@Component({
  selector: 'app-user-management',
  imports: [ DatePipe, ApproveUserModalComponent, RejectUserModalComponent ],
  templateUrl: './user-management.component.html',
  styleUrl: './user-management.component.css',
})
// export class UserManagementComponent implements OnInit {
export class UserManagementComponent{

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
      
      dateFrom = signal('');
      
      dateTo = signal('');

    // VAMOS A MOSTRAR UN DATO MOCKEADO HASTA QUE SE HAGA EL BACKEND, PARA PODER MOSTRAR LA TABLA DE USUARIOS
      //  ngOnInit(): void {

      //    this.loadUsers();

      // }
      users = signal<ManagedUser[]>([
      {
        id: '1',
        name: 'Antonio',
        lastname: 'Sanchez',
        email: 'anr.sanch@gmail.com',
        requestDate: '2026-05-17',
        status: 'Pending',
        role: null
      }
    ]);


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

        this.currentPage.set(1);
        this.loadUsers();

        }

      clearFilters(): void {

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

        this.selectedUser.set(user);

        this.showApproveModal.set(true);

      }


      openRejectModal(user: ManagedUser): void {

        this.selectedUser.set(user);

        this.showRejectModal.set(true);

      }


      closeModals(): void {

        this.showApproveModal.set(false);

        this.showRejectModal.set(false);

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

}