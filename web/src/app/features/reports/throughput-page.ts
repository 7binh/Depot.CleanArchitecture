import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, Validators } from '@angular/forms';
import { map, merge } from 'rxjs';
import { ListLoader } from '../../core/api/list-loader';
import { ERROR_MESSAGES } from '../../core/api/error-messages';
import { EmptyState } from '../../shared/ui/empty-state';
import { ErrorBanner } from '../../shared/ui/error-banner';
import { LoadState } from '../../shared/ui/load-state';
import { PageHeader } from '../../shared/ui/page-header';
import { TextField } from '../../shared/ui/text-field';
import { daysAgo, notFuture, today } from './report-dates';
import { ReportService } from './report.service';
import { ThroughputLine, throughputTotals, toThroughputLines } from './throughput-rows';

@Component({
  selector: 'app-throughput-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [EmptyState, ErrorBanner, LoadState, PageHeader, TextField],
  templateUrl: './throughput-page.html',
  host: { class: 'flex flex-col gap-6' },
})
export class ThroughputPage implements OnInit {
  private readonly reports = inject(ReportService);

  protected readonly from = this.dateControl(daysAgo(6));
  protected readonly to = this.dateControl(today());
  protected readonly dateMessages = {
    required: 'Chọn ngày.',
    future: 'Ngày không được ở tương lai.',
  };
  protected readonly rangeError = signal('');
  protected readonly list = new ListLoader<ThroughputLine>(() =>
    this.reports.throughput(this.from.value, this.to.value).pipe(map(toThroughputLines)),
  );
  protected readonly totals = computed(() => throughputTotals(this.list.items()));
  protected readonly headings = ['Ngày', 'Hãng khai thác', 'Gate in', 'Gate out'];

  constructor() {
    merge(this.from.valueChanges, this.to.valueChanges)
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.refresh());
  }

  ngOnInit(): void {
    this.list.load();
  }

  /** Reloads when both dates are good; a range that ends before it starts is reported without calling the API. */
  private refresh(): void {
    const reversed = this.from.valid && this.to.valid && this.from.value > this.to.value;
    this.rangeError.set(reversed ? ERROR_MESSAGES['DateRange.Invalid'] : '');
    if (this.from.valid && this.to.valid && !reversed) {
      this.list.load();
    }
  }

  private dateControl(value: string): FormControl<string> {
    return new FormControl(value, {
      nonNullable: true,
      validators: [Validators.required, notFuture],
    });
  }
}
