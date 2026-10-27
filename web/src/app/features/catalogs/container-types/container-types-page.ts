import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { toMessage } from '../../../core/api/api-error';
import { Button } from '../../../shared/ui/button';
import { DataTable, TableColumn } from '../../../shared/ui/data-table';
import { EmptyState } from '../../../shared/ui/empty-state';
import { ErrorBanner } from '../../../shared/ui/error-banner';
import { Icon } from '../../../shared/ui/icon';
import { PageHeader } from '../../../shared/ui/page-header';
import { Skeleton } from '../../../shared/ui/skeleton';
import { ContainerType, ContainerTypeService } from './container-type.service';

type LoadState = 'loading' | 'ready' | 'failed';

@Component({
  selector: 'app-container-types-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, DataTable, EmptyState, ErrorBanner, Icon, PageHeader, Skeleton],
  templateUrl: './container-types-page.html',
  host: { class: 'flex flex-col gap-8' },
})
export class ContainerTypesPage implements OnInit {
  private readonly service = inject(ContainerTypeService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly columns: TableColumn<ContainerType>[] = [
    { key: 'code', label: 'Mã loại' },
    { key: 'name', label: 'Tên loại container' },
  ];
  protected readonly placeholders = [1, 2, 3, 4];

  protected readonly state = signal<LoadState>('loading');
  protected readonly items = signal<ContainerType[]>([]);
  protected readonly failure = signal('');

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.state.set('loading');
    this.service
      .list()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (items) => {
          this.items.set(items);
          this.state.set('ready');
        },
        error: (error: unknown) => {
          this.failure.set(toMessage(error));
          this.state.set('failed');
        },
      });
  }
}
