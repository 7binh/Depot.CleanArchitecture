import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, ValidatorFn } from '@angular/forms';
import { toMessage } from '../../core/api/api-error';
import { parseNumber } from '../../shared/forms/number-text.validators';
import { Button } from '../../shared/ui/button';
import { Dialog } from '../../shared/ui/dialog';
import { ErrorBanner } from '../../shared/ui/error-banner';
import { TextField } from '../../shared/ui/text-field';
import { LayoutResult, YardStructureService } from './yard-structure.service';

/** A whole number from `min` to `max`, typed as text. */
function wholeBetween(min: number, max: number): ValidatorFn {
  return (control) => {
    const number = parseNumber(control.value);
    return Number.isInteger(number) && number >= min && number <= max ? null : { range: true };
  };
}

function field(validator: ValidatorFn, initial = ''): FormControl<string> {
  return new FormControl(initial, { nonNullable: true, validators: [validator] });
}

/** Creates the bays FromBay..ToBay of a block with RowCount rows each; what already exists is kept. */
@Component({
  selector: 'app-layout-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, Button, Dialog, ErrorBanner, TextField],
  templateUrl: './layout-dialog.html',
})
export class LayoutDialog {
  private readonly service = inject(YardStructureService);
  protected readonly dialog = viewChild.required(Dialog);

  readonly blockId = input.required<string>();
  readonly closed = output();
  readonly created = output<LayoutResult>();

  protected readonly form = new FormGroup({
    fromBay: field(wholeBetween(1, 999), '1'),
    toBay: field(wholeBetween(1, 999)),
    rowCount: field(wholeBetween(1, 99)),
    maxTier: field(wholeBetween(2, 5), '3'),
  });

  protected readonly bayMessages = { range: 'Nhập số bay từ 1 đến 999.' };
  protected readonly toBayMessages = {
    ...this.bayMessages,
    order: 'Bay kết thúc không được nhỏ hơn bay bắt đầu.',
  };
  protected readonly rowMessages = { range: 'Nhập số row từ 1 đến 99.' };
  protected readonly tierMessages = { range: 'Nhập số tầng từ 2 đến 5.' };

  protected readonly notice = signal<string | null>(null);
  protected readonly submitting = signal(false);

  protected submit(): void {
    this.notice.set(null);
    const { fromBay, toBay, rowCount, maxTier } = this.form.getRawValue();
    const request = {
      fromBay: parseNumber(fromBay),
      toBay: parseNumber(toBay),
      rowCount: parseNumber(rowCount),
      maxTier: parseNumber(maxTier),
    };
    if (this.form.invalid || request.fromBay > request.toBay) {
      this.form.markAllAsTouched();
      this.checkOrder(request.fromBay, request.toBay);
      return;
    }
    this.submitting.set(true);
    this.service.generateLayout(this.blockId(), request).subscribe({
      next: (result) => {
        this.created.emit(result);
        this.dialog().close();
      },
      error: (error: unknown) => {
        this.submitting.set(false);
        this.notice.set(toMessage(error));
      },
    });
  }

  private checkOrder(fromBay: number, toBay: number): void {
    if (this.form.controls.toBay.valid && fromBay > toBay) {
      this.form.controls.toBay.setErrors({ order: true });
    }
  }
}
