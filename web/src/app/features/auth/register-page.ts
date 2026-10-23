import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { sameAs } from '../../shared/forms/same-as.validator';
import { Button } from '../../shared/ui/button';
import { Icon } from '../../shared/ui/icon';
import { TextField } from '../../shared/ui/text-field';

const MIN_PASSWORD_LENGTH = 8;

@Component({
  selector: 'app-register-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, Button, Icon, TextField],
  templateUrl: './register-page.html',
  host: { class: 'flex flex-col gap-8' },
})
export class RegisterPage {
  private readonly password = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.minLength(MIN_PASSWORD_LENGTH)],
  });
  private readonly confirmPassword = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, sameAs(this.password)],
  });

  protected readonly form = new FormGroup({
    fullName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(2)],
    }),
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email],
    }),
    password: this.password,
    confirmPassword: this.confirmPassword,
  });

  protected readonly fullNameMessages = {
    required: 'Nhập họ và tên.',
    minlength: 'Họ và tên cần ít nhất 2 ký tự.',
  };
  protected readonly emailMessages = {
    required: 'Nhập email của bạn.',
    email: 'Email chưa đúng định dạng, ví dụ ten@congty.vn.',
  };
  protected readonly passwordMessages = {
    required: 'Nhập mật khẩu.',
    minlength: `Mật khẩu cần ít nhất ${MIN_PASSWORD_LENGTH} ký tự.`,
  };
  protected readonly confirmMessages = {
    required: 'Nhập lại mật khẩu.',
    sameAs: 'Mật khẩu nhập lại chưa khớp.',
  };

  protected readonly notice = signal<string | null>(null);

  constructor() {
    this.password.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.confirmPassword.updateValueAndValidity());
  }

  protected submit(): void {
    this.notice.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.notice.set('Thông tin hợp lệ. Việc tạo tài khoản sẽ được nối với API ở bước sau.');
  }
}
