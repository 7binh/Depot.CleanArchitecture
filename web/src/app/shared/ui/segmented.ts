import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';

export interface SegmentOption {
  value: string;
  label: string;
}

/** A short row of exclusive choices; exactly one is active. */
@Component({
  selector: 'app-segmented',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @for (option of options(); track option.value) {
      <button
        type="button"
        class="h-9 min-w-16 rounded-control px-4 text-small font-medium transition-colors duration-150 ease-out focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand motion-reduce:transition-none"
        [class]="option.value === value() ? 'bg-brand text-on-brand' : 'text-ink-2 hover:text-ink'"
        [attr.aria-pressed]="option.value === value()"
        (click)="value.set(option.value)"
      >
        {{ option.label }}
      </button>
    }
  `,
  host: {
    role: 'group',
    '[attr.aria-label]': 'label()',
    class: 'inline-flex gap-1 rounded-inset bg-surface-sunken p-1',
  },
})
export class Segmented {
  readonly label = input.required<string>();
  readonly options = input.required<readonly SegmentOption[]>();
  readonly value = model.required<string>();
}
