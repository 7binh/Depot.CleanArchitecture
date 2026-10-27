import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';
import { Button } from '../../shared/ui/button';

@Component({
  selector: 'app-home-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button],
  template: `
    <main class="flex min-h-screen flex-col items-start gap-6 bg-shell p-8">
      <h1 class="text-title text-ink">Depot</h1>
      <p class="text-body text-ink-2">Bạn đã đăng nhập.</p>
      <button appButton type="button" (click)="logout()">Đăng xuất</button>
    </main>
  `,
})
export class HomePage {
  private readonly auth = inject(AuthService);

  protected logout(): void {
    this.auth.logout();
  }
}
