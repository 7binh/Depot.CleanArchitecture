import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Icon } from './icon';

@Component({
  selector: 'app-brand',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Icon],
  template: `
    <span
      class="flex size-6.5 items-center justify-center rounded-field bg-brand text-on-brand shadow-hairline"
    >
      <app-icon name="box" [size]="16" />
    </span>
    <span class="text-section text-ink">{{ name() }}</span>
  `,
  host: { class: 'flex items-center gap-2' },
})
export class Brand {
  readonly name = input('Depot');
}
