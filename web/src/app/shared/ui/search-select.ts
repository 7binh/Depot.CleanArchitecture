import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  input,
  output,
  signal,
  untracked,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { merge, of, startWith, switchMap } from 'rxjs';
import { Icon } from './icon';
import { SelectOption } from './select-field';

let nextId = 0;

/** Case and spaces do not matter when a typed text is compared with a label. */
function squash(text: string): string {
  return text.replace(/\s/g, '').toLowerCase();
}

/**
 * A select you search by typing: the list narrows to the options whose label contains the text.
 * The control holds the chosen option's value, or "" while nothing is chosen.
 */
@Component({
  selector: 'app-search-select',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Icon],
  template: `
    <div class="flex flex-col gap-2">
      <label [attr.for]="id" class="text-caption font-medium text-ink-2">{{ label() }}</label>
      <div class="relative">
        <span class="pointer-events-none absolute top-1/2 left-3 -translate-y-1/2 text-ink-3">
          <app-icon name="search" [size]="16" />
        </span>
        <input
          [id]="id"
          type="text"
          role="combobox"
          autocomplete="off"
          aria-autocomplete="list"
          [attr.aria-expanded]="open()"
          [attr.aria-controls]="id + '-list'"
          [attr.aria-activedescendant]="open() && matches().length > 0 ? optionId(active()) : null"
          [attr.aria-invalid]="error() ? true : null"
          [attr.aria-describedby]="error() ? id + '-error' : null"
          [placeholder]="placeholder()"
          [value]="text()"
          (input)="typed($event)"
          (focus)="open.set(true)"
          (blur)="leave()"
          (keydown)="press($event)"
          class="h-10 w-full rounded-field border bg-surface pr-3 pl-9 text-small text-ink shadow-hairline sm:h-8 transition-colors duration-150 ease-out placeholder:text-ink-3 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand motion-reduce:transition-none"
          [class]="error() ? 'border-danger-fg' : 'border-line-input hover:border-line-strong'"
        />
        @if (open()) {
          <ul
            [id]="id + '-list'"
            role="listbox"
            [attr.aria-label]="label()"
            class="absolute z-10 mt-2 max-h-60 w-full overflow-y-auto rounded-field border border-line bg-surface p-1 shadow-popover"
          >
            @for (option of matches(); track option.value; let i = $index) {
              <li
                [id]="optionId(i)"
                role="option"
                tabindex="-1"
                [attr.aria-selected]="option.value === control().value"
                class="cursor-pointer rounded-xs px-3 py-1.5 text-body text-ink"
                [class.bg-nav-active]="i === active()"
                (mousedown)="choose($event, option)"
              >
                <span>{{ option.label }}</span>
                @if (option.hint; as hint) {
                  &ngsp;
                  <span class="ml-2 text-small text-ink-2">{{ hint }}</span>
                }
              </li>
            } @empty {
              <li role="presentation" class="px-4 py-2 text-small text-ink-2">{{ emptyText() }}</li>
            }
          </ul>
        }
      </div>
      @if (error(); as message) {
        <p [id]="id + '-error'" class="flex items-center gap-2 text-caption text-danger-fg">
          <app-icon name="alert-circle" [size]="16" />
          {{ message }}
        </p>
      }
    </div>
  `,
})
export class SearchSelect {
  readonly label = input.required<string>();
  readonly control = input.required<FormControl<string>>();
  readonly options = input.required<readonly SelectOption[]>();
  readonly placeholder = input('');
  readonly emptyText = input('Không có kết quả khớp.');
  /** Most options shown at once; the rest appear as the text narrows the list. */
  readonly limit = input(8);
  /** Also match the text against each option's hint, not only its label. */
  readonly matchHint = input(false);
  readonly messages = input<Record<string, string>>({});
  /** The text as the user types it, for a page that fetches the options from the server. */
  readonly queryChange = output<string>();

  protected readonly id = `search-select-${nextId++}`;
  protected readonly open = signal(false);
  protected readonly text = signal('');
  protected readonly active = signal(0);
  private readonly filter = signal('');
  private typing = false;

  protected readonly matches = computed(() => {
    const wanted = this.filter().trim().toLowerCase();
    return this.options()
      .filter((option) => this.searchable(option).includes(wanted))
      .slice(0, this.limit());
  });

  private readonly changes = toSignal(
    toObservable(this.control).pipe(switchMap((c) => c.events.pipe(startWith(null)))),
  );

  protected readonly error = computed(() => {
    this.changes();
    const { touched, errors } = this.control();
    if (!touched || !errors) {
      return null;
    }
    return this.messages()[Object.keys(errors)[0]] ?? null;
  });

  constructor() {
    // Keeps the text in step with a value set from outside (a reset, or a value picked by code).
    effect((onCleanup) => {
      const control = this.control();
      const sub = merge(of(control.value), control.valueChanges).subscribe((value) => {
        if (!this.typing) {
          this.text.set(this.labelOf(value));
        }
      });
      onCleanup(() => sub.unsubscribe());
    });
    // Options that arrive later (from the server) may now hold exactly the text typed: choose it,
    // or name a value that was set before its option was known.
    effect(() => {
      const options = this.options();
      untracked(() => {
        this.pickTypedText(options);
        this.nameValue(options);
      });
    });
  }

  protected optionId(index: number): string {
    return `${this.id}-option-${index}`;
  }

  protected typed(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.text.set(value);
    this.filter.set(value);
    this.active.set(0);
    this.open.set(true);
    this.queryChange.emit(value);
    const exact = this.options().find((o) => squash(o.label) === squash(value));
    if (exact) {
      this.pick(exact);
      return;
    }
    this.typing = true;
    this.control().setValue('');
    this.typing = false;
  }

  protected press(event: KeyboardEvent): void {
    const count = this.matches().length;
    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      event.preventDefault();
      this.open.set(true);
      const step = event.key === 'ArrowDown' ? 1 : -1;
      this.active.update((i) => (count === 0 ? 0 : (i + step + count) % count));
    } else if (event.key === 'Enter' && this.open() && count > 0) {
      event.preventDefault();
      this.pick(this.matches()[this.active()]);
    } else if (event.key === 'Escape') {
      this.open.set(false);
    }
  }

  /** Mouse down, not click, so the choice lands before the input loses focus and closes the list. */
  protected choose(event: MouseEvent, option: SelectOption): void {
    event.preventDefault();
    this.pick(option);
  }

  protected leave(): void {
    this.open.set(false);
    this.control().markAsTouched();
  }

  private nameValue(options: readonly SelectOption[]): void {
    const value = this.control().value;
    const label = options.find((o) => o.value === value)?.label;
    if (value !== '' && label !== undefined && !this.typing && this.text() !== label) {
      this.text.set(label);
    }
  }

  private pickTypedText(options: readonly SelectOption[]): void {
    const typed = squash(this.text());
    if (typed === '' || this.control().value !== '') {
      return;
    }
    const exact = options.find((o) => squash(o.label) === typed);
    if (exact) {
      this.pick(exact);
    }
  }

  private pick(option: SelectOption): void {
    this.filter.set('');
    this.text.set(option.label);
    this.open.set(false);
    this.control().setValue(option.value);
    this.control().markAsTouched();
  }

  private searchable(option: SelectOption): string {
    const text = this.matchHint() ? `${option.label} ${option.hint ?? ''}` : option.label;
    return text.toLowerCase();
  }

  private labelOf(value: string): string {
    return untracked(() => this.options().find((o) => o.value === value)?.label ?? '');
  }
}
