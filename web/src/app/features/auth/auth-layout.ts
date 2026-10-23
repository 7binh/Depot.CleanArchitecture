import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Icon } from '../../shared/ui/icon';

@Component({
  selector: 'app-auth-layout',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, Icon],
  templateUrl: './auth-layout.html',
})
export class AuthLayout {
  protected readonly promoTitle = 'Quản lý depot container trong một nơi';
  protected readonly promoText =
    'Theo dõi bãi, container và lệnh giao ra ngoài mà không cần bảng tính.';
}
