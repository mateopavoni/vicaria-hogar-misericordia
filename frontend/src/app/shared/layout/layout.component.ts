import { Component } from '@angular/core';
import { SidebarComponent } from "../components/sidebar/sidebar.component";
import { TopbarComponent } from "../components/topbar/topbar.components";
import { RouterOutlet } from '@angular/router';
import { NotificationMenuComponent} from "../components/notification-menu/notification-menu.component";

@Component({
  selector: 'app-layout',
  imports: [SidebarComponent, TopbarComponent, RouterOutlet, NotificationMenuComponent],
  templateUrl: './layout.component.html',
  styleUrl: './layout.component.css',
})
export class LayoutComponent {}
