import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { toMessage } from '../../core/api/api-error';
import { parseNumber } from '../../shared/forms/number-text.validators';
import { Button } from '../../shared/ui/button';
import { Dialog } from '../../shared/ui/dialog';
import { ErrorBanner } from '../../shared/ui/error-banner';
import { TextField } from '../../shared/ui/text-field';
import { YardRow } from '../yard/yard.service';
import { YardStructureService } from './yard-structure.service';

/** Changes how many tiers a row may stack; the API refuses to go below a container already standing. */
@Component({
  selector: 'app-row-tier-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, Button, Dialog, ErrorBanner, TextField],
  template: `
    <app-dialog [title]="'Số tầng tối đa của row ' + row().rowNo" (closed)="closed.emit()">
      <form class="flex flex-col gap-4" [formGroup]="form" (ngSubmit)="submit()" novalidate>
        @if (notice(); as failure) {
          <app-error-banner>{{ failure }}</app-error-banner>
        }
        <app-text-field
          label="Số tầng tối đa (2 đến 5)"
          icon="layers"
          inputmode="numeric"
          [control]="form.controls.maxTier"
          [messages]="messages"
        />
        <div class="flex justify-end gap-2">
          <button appButton type="button" (click)="dialog().close()">Hủy</button>
          <button appButton variant="primary" type="submit" [disabled]="submitting()">Lưu</button>
        </div>
      </form>
    </app-dialog>
  `,
})
export class RowTierDialog implements OnInit {
  private readonly service = inject(YardStructureService);
  protected readonly dialog = viewChild.required(Dialog);

  readonly blockId = input.required<string>();
  readonly row = input.required<YardRow>();
  readonly closed = output();
  readonly saved = output();

  protected readonly form = new FormGroup({
    maxTier: new FormControl('', {
      nonNullable: true,
      validators: [
        (control) => {
          const number = parseNumber(control.value);
          return Number.isInteger(number) && number >= 2 && number <= 5 ? null : { range: true };
        },
      ],
    }),
  });
  protected readonly messages = { range: 'Nhập số tầng từ 2 đến 5.' };
  protected readonly notice = signal<string | null>(null);
  protected readonly submitting = signal(false);

  ngOnInit(): void {
    this.form.controls.maxTier.setValue(`${this.row().maxTier}`);
  }

  protected submit(): void {
    this.notice.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    this.service
      .changeMaxTier(this.blockId(), this.row(), parseNumber(this.form.controls.maxTier.value))
      .subscribe({
        next: () => {
          this.saved.emit();
          this.dialog().close();
        },
        error: (error: unknown) => {
          this.submitting.set(false);
          this.notice.set(toMessage(error));
        },
      });
  }
}
