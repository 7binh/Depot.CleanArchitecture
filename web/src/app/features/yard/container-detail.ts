import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { Button } from '../../shared/ui/button';
import { Chip } from '../../shared/ui/chip';
import { Icon } from '../../shared/ui/icon';
import { GridCell } from './block-grid';

/**
 * A drawer over the map with what is known about the container the person clicked: a floating card
 * at the right on wide screens, a sheet at the bottom on phones. The dimmed area behind it closes it.
 */
@Component({
  selector: 'app-container-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, Chip, Icon],
  template: `
    <button
      type="button"
      tabindex="-1"
      aria-label="Đóng chi tiết"
      class="absolute inset-0 cursor-default bg-ink/40"
      (click)="closed.emit()"
    ></button>
    @if (cell().container; as container) {
      <div
        role="dialog"
        aria-modal="true"
        aria-label="Chi tiết container"
        class="absolute inset-x-4 bottom-4 max-h-3/4 overflow-y-auto rounded-island border border-line bg-surface p-5 shadow-popover sm:inset-y-4 sm:right-4 sm:left-auto sm:max-h-none sm:w-96"
      >
        <div class="flex items-start justify-between gap-2">
          <h2 class="text-card text-ink tabular-nums">{{ container.number }}</h2>
          <button
            type="button"
            aria-label="Đóng"
            class="grid size-9 shrink-0 place-items-center rounded-control text-ink-2 transition-colors duration-150 ease-out hover:bg-control hover:text-ink focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand motion-reduce:transition-none"
            (click)="closed.emit()"
          >
            <app-icon name="x" [size]="20" />
          </button>
        </div>
        @if (container.isDamaged || container.isLongStay) {
          <div class="mt-2 flex flex-wrap gap-2">
            @if (container.isDamaged) {
              <app-chip color="danger">Hư hỏng</app-chip>
            }
            @if (container.isLongStay) {
              <app-chip color="warning">Tồn lâu</app-chip>
            }
          </div>
        }
        <dl class="mt-4 flex flex-col gap-0.5">
          @for (row of rows(); track row.label) {
            <div class="flex items-center justify-between gap-4 py-2 text-body">
              <dt class="text-ink-2">{{ row.label }}</dt>
              <dd class="text-right text-ink">{{ row.value }}</dd>
            </div>
          }
        </dl>
        <div class="mt-4 flex flex-col gap-2">
          <button
            appButton
            variant="primary"
            type="button"
            [disabled]="cell().above > 0"
            (click)="moveRequested.emit()"
          >
            Di chuyển
          </button>
          @if (cell().above > 0) {
            <p class="text-caption text-ink-2">
              Còn container xếp phía trên, phải di chuyển hoặc cho nó ra trước.
            </p>
          }
        </div>
      </div>
    }
  `,
  host: {
    class: 'fixed inset-0 z-40',
    '(document:keydown.escape)': 'closed.emit()',
  },
})
export class ContainerDetail {
  readonly cell = input.required<GridCell>();
  readonly blockCode = input.required<string>();
  readonly closed = output();
  readonly moveRequested = output();

  protected readonly rows = computed(() => {
    const cell = this.cell();
    const container = cell.container;
    if (!container) {
      return [];
    }
    return [
      { label: 'Hãng khai thác', value: container.operatorCode },
      { label: 'Kích thước', value: `${container.sizeFeet} ft` },
      { label: 'Block', value: this.blockCode() },
      { label: 'Bay', value: `${cell.bayNo}` },
      { label: 'Row', value: `${cell.rowNo}` },
      { label: 'Tầng', value: `${cell.tier}` },
      { label: 'Trong bãi', value: `${container.daysInYard} ngày` },
    ];
  });
}
