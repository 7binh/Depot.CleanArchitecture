import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

const TICKS = 20;

/** A row of thin ticks showing a share from 0 to 100; the filled ones are dark, the rest pale. */
@Component({
  selector: 'app-tick-gauge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @for (filled of ticks(); track $index) {
      <span class="h-1.5 w-px" [class]="filled ? 'bg-ink-body' : 'bg-line-strong'"></span>
    }
  `,
  host: {
    role: 'img',
    class: 'inline-flex items-center gap-0.75',
    '[attr.aria-label]': 'label()',
  },
})
export class TickGauge {
  /** 0 to 100 */
  readonly percent = input.required<number>();
  readonly label = input.required<string>();

  protected readonly ticks = computed(() => {
    const filled = Math.round((Math.min(Math.max(this.percent(), 0), 100) / 100) * TICKS);
    return Array.from({ length: TICKS }, (_, index) => index < filled);
  });
}
