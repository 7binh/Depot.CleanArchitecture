import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type ChipTone = 'neutral' | 'success' | 'warning' | 'danger';

const TONES: Record<ChipTone, { box: string; dot: string }> = {
  neutral: { box: 'bg-surface-sunken text-ink-2', dot: 'bg-ink-3' },
  success: { box: 'bg-success-bg text-success-fg', dot: 'bg-success-dot' },
  warning: { box: 'bg-warning-bg text-warning-fg', dot: 'bg-warning-dot' },
  danger: { box: 'bg-danger-bg text-danger-fg', dot: 'bg-danger-dot' },
};

/** A status label: tinted fill, deeper text and a dot, so the text and not the colour carries the meaning. */
@Component({
  selector: 'app-chip',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="size-1.5 rounded-full" [class]="tone().dot"></span>
    <ng-content />
  `,
  host: {
    class:
      'inline-flex h-6 items-center gap-1.5 whitespace-nowrap rounded-chip px-2 text-caption font-medium',
    '[class]': 'tone().box',
  },
})
export class Chip {
  readonly color = input<ChipTone>('neutral');

  protected readonly tone = computed(() => TONES[this.color()]);
}
