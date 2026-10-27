import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  afterNextRender,
  input,
  output,
  viewChild,
} from '@angular/core';
import { Button } from './button';
import { Icon } from './icon';

let nextId = 0;

/**
 * Modal built on the native `<dialog>`: the browser traps focus and closes it on Esc.
 * It opens as soon as it is rendered, so render it with `@if` and drop it on `closed`.
 */
@Component({
  selector: 'app-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, Icon],
  template: `
    <dialog
      #dialog
      [attr.aria-labelledby]="titleId"
      class="m-auto w-full max-w-md max-h-screen overflow-y-auto rounded-card border border-line bg-surface p-0 text-ink shadow-popover backdrop:bg-ink/40"
      (close)="closed.emit()"
    >
      <div class="flex flex-col gap-6 p-6">
        <div class="flex items-center justify-between gap-4">
          <h2 [id]="titleId" class="text-section text-ink">{{ title() }}</h2>
          <button appButton variant="icon" type="button" aria-label="Đóng" (click)="close()">
            <app-icon name="x" />
          </button>
        </div>
        <ng-content />
      </div>
    </dialog>
  `,
})
export class Dialog {
  readonly title = input.required<string>();
  readonly closed = output();

  protected readonly titleId = `dialog-title-${nextId++}`;

  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');

  constructor() {
    afterNextRender(() => {
      const element = this.dialog().nativeElement;
      element.addEventListener('click', (event) => this.closeOnBackdrop(event));
      element.showModal();
    });
  }

  close(): void {
    this.dialog().nativeElement.close();
  }

  /** A click on the dimmed area hits the `<dialog>` itself, not its content. */
  private closeOnBackdrop(event: MouseEvent): void {
    if (event.target === this.dialog().nativeElement) {
      this.close();
    }
  }
}
