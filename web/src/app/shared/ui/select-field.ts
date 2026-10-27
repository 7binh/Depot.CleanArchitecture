import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { startWith, switchMap } from 'rxjs';
import { Icon } from './icon';

export interface SelectOption {
  value: string;
  label: string;
}

let nextId = 0;

@Component({
  selector: 'app-select-field',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, Icon],
  template: `
    <div class="flex flex-col gap-2">
      <label [attr.for]="id" class="text-caption font-medium text-ink-2">{{ label() }}</label>
      <div class="relative">
        <select
          [id]="id"
          [formControl]="control()"
          [attr.aria-invalid]="error() ? true : null"
          [attr.aria-describedby]="error() ? id + '-error' : null"
          class="h-11 w-full appearance-none rounded-control border pr-11 pl-4 text-body text-ink transition-colors duration-150 ease-out focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand motion-reduce:transition-none"
          [class.border-transparent]="!error()"
          [class.bg-surface-sunken]="!error()"
          [class.border-danger-fg]="error()"
          [class.bg-surface]="error()"
        >
          @if (placeholder(); as text) {
            <option value="">{{ text }}</option>
          }
          @for (option of options(); track option.value) {
            <option [value]="option.value">{{ option.label }}</option>
          }
        </select>
        <span class="pointer-events-none absolute top-1/2 right-4 -translate-y-1/2 text-ink-3">
          <app-icon name="chevron-down" [size]="16" />
        </span>
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
export class SelectField {
  readonly label = input.required<string>();
  readonly control = input.required<FormControl<string>>();
  readonly options = input.required<readonly SelectOption[]>();
  /** Text of the empty choice (value ""); leave blank for no empty choice. */
  readonly placeholder = input('');
  /** Error key mapped to the message shown to the user. */
  readonly messages = input<Record<string, string>>({});

  protected readonly id = `select-field-${nextId++}`;

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
}
