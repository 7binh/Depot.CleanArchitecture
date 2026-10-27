import { computed, Directive, input } from '@angular/core';

export type SkeletonRadius = 'chip' | 'control' | 'inset' | 'card';

const RADII: Record<SkeletonRadius, string> = {
  chip: 'rounded-chip',
  control: 'rounded-control',
  inset: 'rounded-inset',
  card: 'rounded-card',
};

/** Placeholder block shown while data loads. Size it with width and height classes. */
@Directive({
  selector: '[appSkeleton]',
  host: { '[class]': 'classes()', 'aria-hidden': 'true' },
})
export class Skeleton {
  /** Use the radius of the element this block stands in for. */
  readonly radius = input<SkeletonRadius>('control');

  protected readonly classes = computed(
    () =>
      `block animate-pulse bg-surface-sunken motion-reduce:animate-none ${RADII[this.radius()]}`,
  );
}
