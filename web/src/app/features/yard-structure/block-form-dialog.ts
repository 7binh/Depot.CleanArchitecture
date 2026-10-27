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
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { errorCode, toMessage } from '../../core/api/api-error';
import { ERROR_MESSAGES } from '../../core/api/error-messages';
import { notBlank } from '../../shared/forms/not-blank.validator';
import { Button } from '../../shared/ui/button';
import { Dialog } from '../../shared/ui/dialog';
import { ErrorBanner } from '../../shared/ui/error-banner';
import { SelectField, SelectOption } from '../../shared/ui/select-field';
import { TextField } from '../../shared/ui/text-field';
import { Block } from '../yard/yard.service';
import { YardStructureService } from './yard-structure.service';

const CODE_EXISTS = 'Block.CodeExists';

const KIND_OPTIONS: SelectOption[] = [
  { value: 'real', label: 'Block thật (có bay, row, tầng)' },
  { value: 'virtual', label: 'Block ảo (không có vị trí)' },
];

/** Adds a block, or renames the one it is given: the code and the kind cannot change afterwards. */
@Component({
  selector: 'app-block-form-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, Button, Dialog, ErrorBanner, SelectField, TextField],
  templateUrl: './block-form-dialog.html',
})
export class BlockFormDialog implements OnInit {
  private readonly service = inject(YardStructureService);
  protected readonly dialog = viewChild.required(Dialog);

  readonly block = input<Block | null>(null);
  readonly closed = output();
  readonly saved = output();

  protected readonly kindOptions = KIND_OPTIONS;
  protected readonly form = new FormGroup({
    code: new FormControl('', {
      nonNullable: true,
      validators: [notBlank, Validators.maxLength(10)],
    }),
    name: new FormControl('', {
      nonNullable: true,
      validators: [notBlank, Validators.maxLength(100)],
    }),
    kind: new FormControl('real', { nonNullable: true }),
  });

  protected readonly codeMessages = {
    required: 'Nhập mã block.',
    maxlength: 'Mã block tối đa 10 ký tự.',
    conflict: ERROR_MESSAGES[CODE_EXISTS],
  };
  protected readonly nameMessages = {
    required: 'Nhập tên block.',
    maxlength: 'Tên block tối đa 100 ký tự.',
  };

  protected readonly notice = signal<string | null>(null);
  protected readonly submitting = signal(false);

  ngOnInit(): void {
    const block = this.block();
    if (block) {
      this.form.controls.name.setValue(block.name);
    }
  }

  protected submit(): void {
    this.notice.set(null);
    const block = this.block();
    const { code, name, kind } = this.form.getRawValue();
    if (block ? this.form.controls.name.invalid : this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    const request = block
      ? this.service.renameBlock(block.id, name.trim())
      : this.service.createBlock(code.trim(), name.trim(), kind === 'virtual');
    request.subscribe({
      next: () => {
        this.saved.emit();
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
