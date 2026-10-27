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
import { CreateLineOperatorDialog } from './create-line-operator-dialog';
import { LineOperator, LineOperatorService } from './line-operator.service';

type LoadState = 'loading' | 'ready' | 'failed';

@Component({
  selector: 'app-line-operators-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    Button,
    CreateLineOperatorDialog,
    DataTable,
    EmptyState,
    ErrorBanner,
    Icon,
    PageHeader,
    Skeleton,
  ],
  templateUrl: './line-operators-page.html',
  host: { class: 'flex flex-col gap-8' },
})
export class LineOperatorsPage implements OnInit {
  private readonly service = inject(LineOperatorService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly columns: TableColumn<LineOperator>[] = [
    { key: 'code', label: 'Mã hãng' },
    { key: 'name', label: 'Tên hãng' },
  ];
  protected readonly placeholders = [1, 2, 3, 4];

  protected readonly state = signal<LoadState>('loading');
  protected readonly items = signal<LineOperator[]>([]);
  protected readonly failure = signal('');
  protected readonly creating = signal(false);

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
