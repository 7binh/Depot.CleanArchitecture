import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Icon } from './icon';

@Component({
  selector: 'app-brand',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Icon],
  template: `
    <span class="flex size-10 items-center justify-center rounded-control bg-brand-soft text-brand">
      <app-icon name="box" />
    </span>
    <span class="text-card text-ink">{{ name() }}</span>
  `,
  host: { class: 'flex items-center gap-3' },
})
export class Brand {
  readonly name = input('Depot');
}
