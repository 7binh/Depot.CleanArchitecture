import { ChangeDetectionStrategy, Component, input, output, viewChild } from '@angular/core';
import { Button } from './button';
import { Dialog } from './dialog';
import { ErrorBanner } from './error-banner';

/**
 * Asks the person to confirm before something is removed. The screen does the work on `confirmed`,
 * shows a failure through `notice`, and drops the dialog (with `@if`) when it succeeds.
 */
@Component({
  selector: 'app-confirm-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, Dialog, ErrorBanner],
  template: `
    <app-dialog [title]="title()" (closed)="closed.emit()">
      <div class="flex flex-col gap-4">
        @if (notice(); as failure) {
          <app-error-banner>{{ failure }}</app-error-banner>
        }
        <p class="text-body text-ink-body">{{ message() }}</p>
        <div class="flex justify-end gap-2">
          <button appButton type="button" (click)="dialog().close()">Hủy</button>
          <button
            appButton
            variant="primary"
            type="button"
            [disabled]="submitting()"
            (click)="confirmed.emit()"
          >
            {{ confirmLabel() }}
          </button>
        </div>
      </div>
    </app-dialog>
  `,
})
export class ConfirmDialog {
  protected readonly dialog = viewChild.required(Dialog);

  readonly title = input.required<string>();
  readonly message = input.required<string>();
  readonly confirmLabel = input('Xóa');
  readonly notice = input<string | null>(null);
  readonly submitting = input(false);

  readonly confirmed = output();
  readonly closed = output();
}
