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
import { CreateCustomerDialog } from './create-customer-dialog';
import { Customer, CustomerService } from './customer.service';

type LoadState = 'loading' | 'ready' | 'failed';

@Component({
  selector: 'app-customers-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    Button,
    CreateCustomerDialog,
    DataTable,
    EmptyState,
    ErrorBanner,
    Icon,
    PageHeader,
    Skeleton,
  ],
  templateUrl: './customers-page.html',
  host: { class: 'flex flex-col gap-8' },
})
export class CustomersPage implements OnInit {
  private readonly service = inject(CustomerService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly columns: TableColumn<Customer>[] = [
    { key: 'taxCode', label: 'Mã số thuế' },
    { key: 'name', label: 'Tên khách hàng' },
  ];
  protected readonly placeholders = [1, 2, 3, 4];

  protected readonly state = signal<LoadState>('loading');
  protected readonly items = signal<Customer[]>([]);
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
