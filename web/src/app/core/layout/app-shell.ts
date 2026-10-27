import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Avatar } from '../../shared/ui/avatar';
import { Brand } from '../../shared/ui/brand';
import { Button } from '../../shared/ui/button';
import { Icon } from '../../shared/ui/icon';
import { AuthService } from '../auth/auth.service';
import { SidebarNav } from './sidebar-nav';

@Component({
  selector: 'app-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, SidebarNav, Avatar, Brand, Button, Icon],
  templateUrl: './app-shell.html',
  host: { '(document:keydown.escape)': 'closeMenu()' },
})
export class AppShell {
  private readonly auth = inject(AuthService);

  protected readonly email = this.auth.email;
  protected readonly menuOpen = signal(false);

  protected openMenu(): void {
    this.menuOpen.set(true);
  }

  protected closeMenu(): void {
    this.menuOpen.set(false);
  }

  protected logout(): void {
    this.auth.logout();
  }
}
