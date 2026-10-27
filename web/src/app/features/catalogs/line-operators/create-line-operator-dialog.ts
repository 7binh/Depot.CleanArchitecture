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
import { LineOperatorService } from './line-operator.service';

const CODE_EXISTS = 'LineOperator.CodeExists';

@Component({
  selector: 'app-create-line-operator-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, Button, Dialog, ErrorBanner, TextField],
  templateUrl: './create-line-operator-dialog.html',
})
export class CreateLineOperatorDialog {
  private readonly service = inject(LineOperatorService);
  protected readonly dialog = viewChild.required(Dialog);

  readonly closed = output();
  readonly created = output();

  protected readonly form = new FormGroup({
    code: new FormControl('', {
      nonNullable: true,
      validators: [notBlank, Validators.minLength(3), Validators.maxLength(3)],
    }),
    name: new FormControl('', {
      nonNullable: true,
      validators: [notBlank, Validators.maxLength(200)],
    }),
  });

  protected readonly codeMessages = {
    required: 'Nhập mã hãng.',
    minlength: 'Mã hãng gồm đúng 3 ký tự.',
    maxlength: 'Mã hãng gồm đúng 3 ký tự.',
    conflict: ERROR_MESSAGES[CODE_EXISTS],
  };
  protected readonly nameMessages = {
    required: 'Nhập tên hãng.',
    maxlength: 'Tên hãng tối đa 200 ký tự.',
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
    const { code, name } = this.form.getRawValue();
    this.service.create(code.trim(), name.trim()).subscribe({
      next: () => {
        this.created.emit();
        this.dialog().close();
      },
      error: (error: unknown) => this.fail(error),
    });
  }

  private fail(error: unknown): void {
    this.submitting.set(false);
    if (errorCode(error) === CODE_EXISTS) {
      const code = this.form.controls.code;
      code.setErrors({ conflict: true });
      code.markAsTouched();
      return;
    }
    this.notice.set(toMessage(error));
  }
}
