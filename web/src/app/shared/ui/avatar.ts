import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type AvatarSize = 24 | 36 | 48;

const SIZES: Record<AvatarSize, string> = {
  24: 'size-6 text-micro',
  36: 'size-9 text-caption font-semibold',
  48: 'size-12 text-section',
};

@Component({
  selector: 'app-avatar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `{{ initial() }}`,
  host: {
    '[class]': 'classes()',
    'aria-hidden': 'true',
  },
})
export class Avatar {
  /** Name or email; the avatar shows its first letter. */
  readonly name = input.required<string>();
  readonly size = input<AvatarSize>(36);

  protected readonly initial = computed(() => this.name().trim().charAt(0).toUpperCase() || '?');
  protected readonly classes = computed(
    () =>
      `inline-flex shrink-0 items-center justify-center rounded-full bg-avatar text-ink-body ${SIZES[this.size()]}`,
  );
}
