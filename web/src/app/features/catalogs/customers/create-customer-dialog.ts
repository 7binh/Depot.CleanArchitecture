import {
  ChangeDetectionStrategy,
  Component,
  inject,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { errorCode, toMessage } from '../../../core/api/api-error';
import { ERROR_MESSAGES } from '../../../core/api/error-messages';
import { notBlank } from '../../../shared/forms/not-blank.validator';
import { Button } from '../../../shared/ui/button';
import { Dialog } from '../../../shared/ui/dialog';
import { ErrorBanner } from '../../../shared/ui/error-banner';
import { TextField } from '../../../shared/ui/text-field';
import { CustomerService } from './customer.service';

const TAX_CODE_EXISTS = 'Customer.TaxCodeExists';

@Component({
  selector: 'app-create-customer-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, Button, Dialog, ErrorBanner, TextField],
  templateUrl: './create-customer-dialog.html',
})
export class CreateCustomerDialog {
  private readonly service = inject(CustomerService);
  protected readonly dialog = viewChild.required(Dialog);

  readonly closed = output();
  readonly created = output();

  protected readonly form = new FormGroup({
    taxCode: new FormControl('', {
      nonNullable: true,
      validators: [notBlank, Validators.pattern(/^(\d{10}|\d{13})$/)],
    }),
    name: new FormControl('', {
      nonNullable: true,
      validators: [notBlank, Validators.maxLength(200)],
    }),
  });

  protected readonly taxCodeMessages = {
    required: 'Nhập mã số thuế.',
    pattern: 'Mã số thuế gồm đúng 10 hoặc 13 chữ số.',
    conflict: ERROR_MESSAGES[TAX_CODE_EXISTS],
  };
  protected readonly nameMessages = {
    required: 'Nhập tên khách hàng.',
    maxlength: 'Tên khách hàng tối đa 200 ký tự.',
  };

  protected readonly notice = signal<string | null>(null);
  protected readonly submitting = signal(false);

  protected submit(): void {
    this.notice.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    const { taxCode, name } = this.form.getRawValue();
    this.service.create(taxCode.trim(), name.trim()).subscribe({
      next: () => {
        this.created.emit();
        this.dialog().close();
      },
      error: (error: unknown) => this.fail(error),
    });
  }

  private fail(error: unknown): void {
    this.submitting.set(false);
    if (errorCode(error) === TAX_CODE_EXISTS) {
      const taxCode = this.form.controls.taxCode;
      taxCode.setErrors({ conflict: true });
      taxCode.markAsTouched();
      return;
    }
    this.notice.set(toMessage(error));
  }
}
