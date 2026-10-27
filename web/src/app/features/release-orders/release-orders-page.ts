import { ChangeDetectionStrategy, Component, OnInit, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { forkJoin, map } from 'rxjs';
import { ListLoader } from '../../core/api/list-loader';
import { Button } from '../../shared/ui/button';
import { Chip } from '../../shared/ui/chip';
import { EmptyState } from '../../shared/ui/empty-state';
import { Icon } from '../../shared/ui/icon';
import { LoadState } from '../../shared/ui/load-state';
import { PageHeader } from '../../shared/ui/page-header';
import { SelectField, SelectOption } from '../../shared/ui/select-field';
import { TextField } from '../../shared/ui/text-field';
import { CustomerService } from '../catalogs/customers/customer.service';
import { LineOperatorService } from '../catalogs/line-operators/line-operator.service';
import { ReleaseOrderRow, toOrderRows } from './release-order-rows';
import { ORDER_STATUSES, OrderStatus } from './release-order-status';
import { ReleaseOrderService } from './release-order.service';

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
    RouterLink,
    SelectField,
    TextField,
  ],
  templateUrl: './release-orders-page.html',
  host: { class: 'flex flex-col gap-6' },
})
export class ReleaseOrdersPage implements OnInit {
  private readonly orders = inject(ReleaseOrderService);
  private readonly operators = inject(LineOperatorService);
  private readonly customers = inject(CustomerService);

  protected readonly list = new ListLoader<ReleaseOrderRow>(() =>
    forkJoin([this.orders.list(), this.operators.list(), this.customers.list()]).pipe(
      map(([orders, operators, customers]) => toOrderRows(orders, operators, customers)),
    ),
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

  protected readonly searchFilter = new FormControl('', { nonNullable: true });
  protected readonly statusFilter = new FormControl('', { nonNullable: true });
  private readonly search = toSignal(this.searchFilter.valueChanges, { initialValue: '' });
  private readonly status = toSignal(this.statusFilter.valueChanges, { initialValue: '' });

  protected readonly visible = computed(() => {
    const wanted = this.search().trim().toLowerCase();
    return this.list
      .items()
      .filter((row) => !this.status() || row.status === this.status())
      .filter((row) => `${row.orderNo} ${row.exportVessel}`.toLowerCase().includes(wanted));
  });

  ngOnInit(): void {
    this.list.load();
  }
}
