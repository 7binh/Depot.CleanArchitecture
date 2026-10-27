import { ChangeDetectionStrategy, Component, OnInit, computed, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin, map } from 'rxjs';
import { ListLoader } from '../../core/api/list-loader';
import { Button } from '../../shared/ui/button';
import { Card } from '../../shared/ui/card';
import { Chip } from '../../shared/ui/chip';
import { ErrorBanner } from '../../shared/ui/error-banner';
import { LoadState } from '../../shared/ui/load-state';
import { PageHeader } from '../../shared/ui/page-header';
import { ContainerTypeService } from '../catalogs/container-types/container-type.service';
import { CustomerService } from '../catalogs/customers/customer.service';
import { LineOperatorService } from '../catalogs/line-operators/line-operator.service';
import { ReleaseOrderDetail, toOrderDetail } from './release-order-detail';
import { ORDER_STATUSES } from './release-order-status';
import { ReleaseOrderService } from './release-order.service';

@Component({
  selector: 'app-release-order-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Card, Button, Chip, ErrorBanner, LoadState, PageHeader, RouterLink],
  templateUrl: './release-order-page.html',
  host: { class: 'flex flex-col gap-6' },
})
export class ReleaseOrderPage implements OnInit {
  private readonly id = inject(ActivatedRoute).snapshot.paramMap.get('id') ?? '';
  private readonly orders = inject(ReleaseOrderService);
  private readonly operators = inject(LineOperatorService);
  private readonly customers = inject(CustomerService);
  private readonly types = inject(ContainerTypeService);

  /** A loader holding the one order, so the page gets the same status, failure and retry as the lists. */
  protected readonly loader = new ListLoader<ReleaseOrderDetail>(() =>
    forkJoin([
      this.orders.get(this.id),
      this.operators.list(),
      this.customers.list(),
      this.types.list(),
    ]).pipe(
      map(([order, operators, customers, types]) => [
        toOrderDetail(order, operators, customers, types),
      ]),
    ),
  );
  protected readonly order = computed(() => this.loader.items()[0]);
  protected readonly statuses = ORDER_STATUSES;
  protected readonly headings = ['Loại', 'Cỡ', 'Số lượng', 'Đã giao', 'Còn lại', 'Tiến độ'];

  ngOnInit(): void {
    this.loader.load();
  }
}
