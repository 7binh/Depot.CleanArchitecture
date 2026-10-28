import { ChangeDetectionStrategy, Component, OnInit, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin, map } from 'rxjs';
import { ListLoader } from '../../core/api/list-loader';
import { Button } from '../../shared/ui/button';
import { EmptyState } from '../../shared/ui/empty-state';
import { Icon, IconName } from '../../shared/ui/icon';
import { LoadState } from '../../shared/ui/load-state';
import { PageHeader } from '../../shared/ui/page-header';
import { Sparkline } from '../../shared/ui/sparkline';
import { TickGauge } from '../../shared/ui/tick-gauge';
import { ReleaseOrderService } from '../release-orders/release-order.service';
import { daysAgo, today } from '../reports/report-dates';
import { ReportService } from '../reports/report.service';
import { HomeSummary, toDayMovements, toHomeSummary } from './home-summary';

/** How many days of gate traffic the page shows, today included. */
const DAYS = 7;

interface Shortcut {
  label: string;
  hint: string;
  path: string;
  icon: IconName;
}

const SHORTCUTS: Shortcut[] = [
  { label: 'Sơ đồ bãi', hint: 'Xem và di chuyển container', path: '/so-do-bai', icon: 'layers' },
  {
    label: 'Lệnh giao',
    hint: 'Theo dõi các lệnh còn hiệu lực',
    path: '/lenh-giao',
    icon: 'layers',
  },
  { label: 'Container', hint: 'Tra cứu theo số container', path: '/container', icon: 'box' },
  {
    label: 'Lịch sử lưu bãi',
    hint: 'Lượt vào, ra của từng container',
    path: '/lich-su-luu-bai',
    icon: 'calendar',
  },
];

@Component({
  selector: 'app-home-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, EmptyState, Icon, LoadState, PageHeader, RouterLink, Sparkline, TickGauge],
  templateUrl: './home-page.html',
  host: { class: 'flex flex-col gap-6' },
})
export class HomePage implements OnInit {
  private readonly reports = inject(ReportService);
  private readonly orders = inject(ReleaseOrderService);

  protected readonly shortcuts = SHORTCUTS;
  protected readonly loader = new ListLoader<HomeSummary>(() => {
    const dates = Array.from({ length: DAYS }, (_, index) => daysAgo(DAYS - 1 - index));
    return forkJoin({
      stock: this.reports.yardStock(today()),
      throughput: this.reports.throughput(dates[0], today()),
      open: this.orders.searchOpen(1, 1),
    }).pipe(
      map(({ stock, throughput, open }) => [
        toHomeSummary(stock, toDayMovements(throughput, dates), open.totalCount),
      ]),
    );
  });
  protected readonly summary = computed(() => this.loader.items()[0]);
  protected readonly trend = computed(() => this.summary()?.days.map((d) => d.total) ?? []);
  protected readonly trendTotal = computed(() => this.trend().reduce((sum, n) => sum + n, 0));

  ngOnInit(): void {
    this.loader.load();
  }
}
