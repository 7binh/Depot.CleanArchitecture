import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { toMessage } from '../../core/api/api-error';
import { AuthService } from '../../core/auth/auth.service';
import { Button } from '../../shared/ui/button';
import { ErrorBanner } from '../../shared/ui/error-banner';
import { TextField } from '../../shared/ui/text-field';

@Component({
  selector: 'app-login-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, Button, ErrorBanner, TextField],
  templateUrl: './login-page.html',
  host: { class: 'flex flex-col gap-8' },
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

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
  protected readonly submitting = signal(false);

  protected submit(): void {
    this.notice.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    const { email, password } = this.form.getRawValue();
    this.auth.login(email, password).subscribe({
      next: () => void this.router.navigateByUrl('/'),
      error: (error: unknown) => {
        this.submitting.set(false);
        this.notice.set(toMessage(error));
      },
    });
  }
}
