import { ChangeDetectionStrategy, Component } from '@angular/core';
import { EmptyState } from '../../shared/ui/empty-state';
import { PageHeader } from '../../shared/ui/page-header';

@Component({
  selector: 'app-home-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PageHeader, EmptyState],
  template: `
    <app-page-header [title]="title" [description]="description" />
    <app-empty-state icon="box" [message]="emptyMessage" />
  `,
  host: { class: 'flex flex-col gap-8' },
})
export class HomePage {
  protected readonly title = 'Trang chủ';
  protected readonly description = 'Tổng quan bãi sẽ hiển thị ở đây khi các màn hình được thêm.';
  protected readonly emptyMessage = 'Chưa có màn hình nào để hiển thị.';
}
