import { ChangeDetectionStrategy, Component, OnInit, computed, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, Validators } from '@angular/forms';
import { map } from 'rxjs';
import { ListLoader } from '../../core/api/list-loader';
import { EmptyState } from '../../shared/ui/empty-state';
import { LoadState } from '../../shared/ui/load-state';
import { PageHeader } from '../../shared/ui/page-header';
import { TextField } from '../../shared/ui/text-field';
import { TickGauge } from '../../shared/ui/tick-gauge';
import { notFuture, today } from './report-dates';
import { ReportService } from './report.service';
import { StockLine, toStockLines, totalOf } from './yard-stock-rows';

@Component({
  selector: 'app-yard-stock-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [EmptyState, LoadState, PageHeader, TextField, TickGauge],
  templateUrl: './yard-stock-page.html',
  host: { class: 'flex flex-col gap-6' },
})
export class YardStockPage implements OnInit {
  private readonly reports = inject(ReportService);

  protected readonly date = new FormControl(today(), {
    nonNullable: true,
    validators: [Validators.required, notFuture],
  });
  protected readonly dateMessages = {
    required: 'Chọn ngày cần xem.',
    future: 'Ngày không được ở tương lai.',
  };
  protected readonly list = new ListLoader<StockLine>(() =>
    this.reports.yardStock(this.date.value).pipe(map(toStockLines)),
  );
  protected readonly total = computed(() => totalOf(this.list.items()));
  protected readonly headings = ['Hãng khai thác', '0 đến 9 ngày', 'Từ 10 ngày', 'Tổng', 'Tồn lâu'];

  constructor() {
    this.date.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      if (this.date.valid) {
        this.list.load();
      }
    });
  }

  ngOnInit(): void {
    this.list.load();
  }
}
