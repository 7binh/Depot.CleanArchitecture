import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Icon, IconName } from './icon';

/** One sentence about what is missing; an optional projected button offers the fix. */
@Component({
  selector: 'app-empty-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Icon],
  template: `
    <span class="flex size-9 items-center justify-center rounded-control bg-control text-ink-2">
      <app-icon [name]="icon()" [size]="16" />
    </span>
    <p class="text-body text-ink-2">{{ message() }}</p>
    <ng-content />
  `,
  host: { class: 'flex flex-col items-start gap-4' },
})
export class EmptyState {
  readonly icon = input.required<IconName>();
  readonly message = input.required<string>();
}
