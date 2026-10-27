import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type ChipTone = 'neutral' | 'info' | 'success' | 'warning' | 'danger';

const TONES: Record<ChipTone, string> = {
  neutral: 'bg-neutral-bg text-neutral-fg',
  info: 'bg-info-bg text-info-fg',
  success: 'bg-success-bg text-success-fg',
  warning: 'bg-warning-bg text-warning-fg',
  danger: 'bg-danger-bg text-danger-fg',
};

/** A status label: an opaque pastel box with deep ink of the same hue. No dot, no border. */
@Component({
  selector: 'app-chip',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<ng-content />`,
  host: {
    class:
      'inline-flex h-5.5 items-center justify-center whitespace-nowrap rounded-chip px-2 text-caption font-medium',
    '[class]': 'tone()',
  },
})
export class Chip {
  readonly color = input<ChipTone>('neutral');

  protected readonly tone = computed(() => TONES[this.color()]);
}
