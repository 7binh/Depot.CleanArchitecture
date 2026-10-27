import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';

export interface SegmentOption {
  value: string;
  label: string;
}

/** A short row of exclusive choices drawn as underline tabs; exactly one is active. */
@Component({
  selector: 'app-segmented',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @for (option of options(); track option.value) {
      <button
        type="button"
        class="-mb-px h-10 cursor-pointer border-b-2 text-body transition-colors duration-150 ease-out focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand motion-reduce:transition-none"
        [class]="
          option.value === value()
            ? 'border-brand font-medium text-ink'
            : 'border-transparent text-ink-3 hover:text-ink-body'
        "
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
    class: 'inline-flex gap-5 border-b border-line',
  },
})
export class Segmented {
  readonly label = input.required<string>();
  readonly options = input.required<readonly SegmentOption[]>();
  readonly value = model.required<string>();
}
