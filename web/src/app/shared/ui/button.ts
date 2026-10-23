import { computed, Directive, input } from '@angular/core';

export type ButtonVariant = 'primary' | 'secondary' | 'icon';
export type ButtonSize = 'default' | 'small';

const BASE =
  'inline-flex items-center justify-center gap-2 rounded-control text-body font-medium ' +
  'transition-colors duration-150 ease-out motion-reduce:transition-none ' +
  'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand ' +
  'disabled:cursor-not-allowed disabled:border-transparent disabled:bg-surface-sunken ' +
  'disabled:text-ink-disabled disabled:shadow-none';

const VARIANTS: Record<ButtonVariant, string> = {
  primary: 'bg-brand text-on-brand hover:bg-brand-hover active:bg-brand-press',
  secondary:
    'border border-line bg-surface text-ink shadow-hairline hover:border-line-strong ' +
    'hover:bg-surface-sunken active:bg-shell',
  icon: 'bg-transparent text-ink-2 hover:bg-surface-sunken active:bg-shell',
};

const SIZES: Record<ButtonVariant, Record<ButtonSize, string>> = {
  primary: { default: 'h-10 px-4', small: 'h-8 px-3' },
  secondary: { default: 'h-10 px-4', small: 'h-8 px-3' },
  icon: { default: 'size-10', small: 'size-8' },
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
