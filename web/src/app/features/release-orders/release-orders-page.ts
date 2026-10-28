import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { debounceTime, map, tap } from 'rxjs';
import { ListLoader } from '../../core/api/list-loader';
import { Paged } from '../../core/api/paged';
import { Button } from '../../shared/ui/button';
import { Chip } from '../../shared/ui/chip';
import { EmptyState } from '../../shared/ui/empty-state';
import { Icon } from '../../shared/ui/icon';
import { LoadState } from '../../shared/ui/load-state';
import { PageHeader } from '../../shared/ui/page-header';
import { Pagination } from '../../shared/ui/pagination';
import { SelectField, SelectOption } from '../../shared/ui/select-field';
import { TextField } from '../../shared/ui/text-field';
import { CustomerService } from '../catalogs/customers/customer.service';
import { LineOperatorService } from '../catalogs/line-operators/line-operator.service';
import { toOrderRows } from './release-order-rows';
import { ORDER_STATUSES, OrderStatus } from './release-order-status';
import { ReleaseOrder, ReleaseOrderService } from './release-order.service';

const PAGE_SIZE = 20;
/** Typing waits this long for a pause before it asks the API again. */
const FILTER_DELAY_MS = 300;

@Component({
  selector: 'app-release-orders-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    Button,
    Chip,
    EmptyState,
    Icon,
    LoadState,
    PageHeader,
    Pagination,
    RouterLink,
    SelectField,
    TextField,
  ],
  templateUrl: './release-orders-page.html',
  host: { class: 'flex flex-col gap-6' },
})
export class ReleaseOrdersPage implements OnInit {
  private readonly orders = inject(ReleaseOrderService);
  private readonly operatorService = inject(LineOperatorService);
  private readonly customerService = inject(CustomerService);

  /** Names for the table; the orders show without them, so they never block the page. */
  private readonly operators = new ListLoader(() => this.operatorService.list());
  private readonly customers = new ListLoader(() => this.customerService.list());

  protected readonly filters = new FormGroup({
    orderNo: new FormControl('', { nonNullable: true }),
    status: new FormControl('', { nonNullable: true }),
  });
  protected readonly page = signal(1);
  /** The page the list shows: totals for the pager. Null until the first answer. */
  protected readonly paging = signal<Paged<ReleaseOrder> | null>(null);
  /** Whether the answer on screen was filtered, which decides what an empty list says. */
  protected readonly filtered = signal(false);

  protected readonly list = new ListLoader<ReleaseOrder>(() => {
    const filter = this.filters.getRawValue();
    return this.orders.search(filter, this.page(), PAGE_SIZE).pipe(
      tap((paged) => {
        this.paging.set(paged);
        this.filtered.set(Object.values(filter).some((value) => value.trim() !== ''));
      }),
      map((paged) => paged.items),
    );
  });
  protected readonly rows = computed(() =>
    toOrderRows(this.list.items(), this.operators.items(), this.customers.items()),
  );
  protected readonly headings = [
    'Lệnh giao',
    'Hãng và khách hàng',
    'Hạn lệnh',
    'Đã giao',
    'Trạng thái',
  ];
  protected readonly statuses = ORDER_STATUSES;
  protected readonly statusOptions: SelectOption[] = (
    Object.entries(ORDER_STATUSES) as [OrderStatus, { label: string }][]
  ).map(([value, { label }]) => ({ value, label }));

  constructor() {
    this.filters.valueChanges
      .pipe(debounceTime(FILTER_DELAY_MS), takeUntilDestroyed())
      .subscribe(() => this.goTo(1));
  }

  ngOnInit(): void {
    this.list.load();
    this.operators.load();
    this.customers.load();
  }

  protected goTo(page: number): void {
    this.page.set(page);
    this.list.load(true);
  }
}
