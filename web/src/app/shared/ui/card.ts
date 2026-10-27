import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

const BASE = 'block rounded-card border border-line';

/** A hairline group on the canvas: no fill and no shadow, so white stays for inputs and selected rows. */
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
