import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Icon, IconName } from './icon';

/** Inline error: says what failed and what to do. The message is the projected content. */
@Component({
  selector: 'app-error-banner',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Icon],
  template: `
    <app-icon [name]="icon()" />
    <span><ng-content /></span>
  `,
  host: {
    role: 'alert',
    class: 'flex items-start gap-3 rounded-control bg-danger-bg p-4 text-small text-danger-fg',
  },
})
export class ErrorBanner {
  readonly icon = input<IconName>('alert-circle');
}
