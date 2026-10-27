import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** Page title and short description; projected content (actions) sits on the right. */
@Component({
  selector: 'app-page-header',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex min-w-0 flex-col gap-2">
      <h1 class="text-title text-ink">{{ title() }}</h1>
      @if (description(); as text) {
        <p class="text-body text-ink-2">{{ text }}</p>
      }
    </div>
    <div class="flex items-center gap-2">
      <ng-content />
    </div>
  `,
  host: { class: 'flex flex-wrap items-start justify-between gap-4' },
})
export class PageHeader {
  readonly title = input.required<string>();
  readonly description = input('');
}
