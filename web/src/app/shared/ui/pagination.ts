import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { Button } from './button';

/** Previous and next buttons with a "from to of total" count. Pages start at 1. */
@Component({
  selector: 'app-pagination',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button],
  template: `
    <nav class="flex items-center justify-between gap-4" aria-label="Phân trang">
      <p class="text-small text-ink-2 tabular-nums">
        {{ first() }}–{{ last() }} / {{ totalCount() }}
      </p>
      <div class="flex items-center gap-2">
        <button
          appButton
          type="button"
          [disabled]="page() <= 1"
          (click)="pageChange.emit(page() - 1)"
        >
          Trước
        </button>
        <span class="text-small text-ink-2 tabular-nums">{{ page() }} / {{ totalPages() }}</span>
        <button
          appButton
          type="button"
          [disabled]="page() >= totalPages()"
          (click)="pageChange.emit(page() + 1)"
        >
          Sau
        </button>
      </div>
    </nav>
  `,
})
export class Pagination {
  readonly page = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly totalCount = input.required<number>();
  readonly totalPages = input.required<number>();
  readonly pageChange = output<number>();

  protected readonly first = computed(() =>
    this.totalCount() === 0 ? 0 : (this.page() - 1) * this.pageSize() + 1,
  );
  protected readonly last = computed(() =>
    Math.min(this.page() * this.pageSize(), this.totalCount()),
  );
}
