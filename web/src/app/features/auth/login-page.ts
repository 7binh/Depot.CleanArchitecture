import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Button } from '../../shared/ui/button';
import { Icon } from '../../shared/ui/icon';
import { TextField } from '../../shared/ui/text-field';

@Component({
  selector: 'app-login-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, Button, Icon, TextField],
  templateUrl: './login-page.html',
  host: { class: 'flex flex-col gap-8' },
})
export class LoginPage {
  protected readonly form = new FormGroup({
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email],
    }),
    password: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  protected readonly emailMessages = {
    required: 'Nhập email của bạn.',
    email: 'Email chưa đúng định dạng, ví dụ ten@congty.vn.',
  };
  protected readonly passwordMessages = { required: 'Nhập mật khẩu.' };

  protected readonly notice = signal<string | null>(null);

  protected submit(): void {
    this.notice.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.notice.set('Thông tin hợp lệ. Việc đăng nhập sẽ được nối với API ở bước sau.');
  }
}
