import { computed, Directive, input } from '@angular/core';

export type ButtonVariant = 'primary' | 'secondary' | 'icon';
export type ButtonSize = 'default' | 'small';

const BASE =
  'inline-flex cursor-pointer items-center justify-center gap-1.5 rounded-control text-small font-medium ' +
  'transition-colors duration-150 ease-out motion-reduce:transition-none ' +
  'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand ' +
  'disabled:cursor-not-allowed disabled:border-transparent disabled:bg-surface-sunken ' +
  'disabled:text-ink-disabled disabled:shadow-none';

const VARIANTS: Record<ButtonVariant, string> = {
  primary:
    'border border-brand-press bg-brand text-on-brand shadow-primary hover:bg-brand-hover ' +
    'active:translate-y-px active:bg-brand-press',
  secondary: 'bg-control text-ink hover:bg-control-hover',
  icon: 'bg-transparent text-ink-2 hover:bg-control hover:text-ink active:bg-control-hover',
};

/** Controls are 40px high on touch-sized screens and 28px (the spec) from `sm` up. */
const SIZES: Record<ButtonVariant, Record<ButtonSize, string>> = {
  primary: { default: 'h-10 px-3 sm:h-7', small: 'h-8 px-3 sm:h-6' },
  secondary: { default: 'h-10 px-3 sm:h-7', small: 'h-8 px-3 sm:h-6' },
  icon: { default: 'size-10 sm:size-7', small: 'size-8 sm:size-6' },
};

@Directive({
  selector: '[appButton]',
  host: { '[class]': 'classes()' },
})
export class Button {
  readonly variant = input<ButtonVariant>('secondary');
  readonly size = input<ButtonSize>('default');

  protected readonly classes = computed(
    () => `${BASE} ${VARIANTS[this.variant()]} ${SIZES[this.variant()][this.size()]}`,
  );
}
