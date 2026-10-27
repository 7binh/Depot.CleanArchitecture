import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

const BASE = 'block rounded-card border border-line bg-surface';

/** The one level of card: white, 1px line, no shadow. Everything inside it is plain content, never another card. */
@Component({
  selector: 'app-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<ng-content />`,
  host: { '[class]': 'classes()' },
})
export class Card {
  /** Tables and lists use the default padding (20); forms and other large panels use 24. */
  readonly size = input<'default' | 'large'>('default');

  protected readonly classes = computed(() => `${BASE} ${this.size() === 'large' ? 'p-6' : 'p-5'}`);
}
