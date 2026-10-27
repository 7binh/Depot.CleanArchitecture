import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { startWith, switchMap } from 'rxjs';
import { Button } from './button';
import { Icon, IconName } from './icon';

let nextId = 0;

@Component({
  selector: 'app-text-field',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, Icon, Button],
  templateUrl: './text-field.html',
})
export class TextField {
  readonly label = input.required<string>();
  readonly control = input.required<FormControl<string>>();
  readonly icon = input.required<IconName>();
  readonly type = input<'text' | 'email' | 'password' | 'date' | 'datetime-local'>('text');
  readonly inputmode = input<'text' | 'numeric' | 'decimal'>('text');
  readonly placeholder = input('');
  readonly autocomplete = input('off');
  /** Error key (for example `required`) mapped to the message shown to the user. */
  readonly messages = input<Record<string, string>>({});

  protected readonly id = `text-field-${nextId++}`;
  protected readonly revealed = signal(false);
  protected readonly isPassword = computed(() => this.type() === 'password');
  protected readonly inputType = computed(() =>
    this.isPassword() && this.revealed() ? 'text' : this.type(),
  );

  private readonly changes = toSignal(
    toObservable(this.control).pipe(switchMap((c) => c.events.pipe(startWith(null)))),
  );

  protected readonly error = computed(() => {
    this.changes();
    const { touched, errors } = this.control();
    if (!touched || !errors) {
      return null;
    }
    const key = Object.keys(errors)[0];
    return this.messages()[key] ?? null;
  });

  protected toggleReveal(): void {
    this.revealed.update((value) => !value);
  }
}
