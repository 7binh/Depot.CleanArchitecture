import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { LoginPage } from './login-page';

describe('LoginPage', () => {
  const login = vi.fn();

  function setup() {
    TestBed.configureTestingModule({
      imports: [LoginPage],
      providers: [provideRouter([]), { provide: AuthService, useValue: { login } }],
    });
    const fixture = TestBed.createComponent(LoginPage);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    return { fixture, element };
  }

  function fillAndSubmit(element: HTMLElement, email: string, password: string): void {
    const inputs = element.querySelectorAll('input');
    inputs[0].value = email;
    inputs[0].dispatchEvent(new Event('input'));
    inputs[1].value = password;
    inputs[1].dispatchEvent(new Event('input'));
    element.querySelector('form')?.dispatchEvent(new Event('submit'));
  }

  beforeEach(() => login.mockReset());

  it('does not call the API when the form is invalid', () => {
    const { element } = setup();

    fillAndSubmit(element, '', '');

    expect(login).not.toHaveBeenCalled();
  });

  it('goes to the home page after a successful login', () => {
    login.mockReturnValue(of({}));
    const { element } = setup();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);

    fillAndSubmit(element, 'a@b.vn', 'secret');

    expect(login).toHaveBeenCalledWith('a@b.vn', 'secret');
    expect(navigate).toHaveBeenCalledWith('/');
  });

  it('shows the API message when the login fails', () => {
    const error = new HttpErrorResponse({
      status: 401,
      error: { title: 'Auth.InvalidCredentials' },
    });
    login.mockReturnValue(throwError(() => error));
    const { fixture, element } = setup();

    fillAndSubmit(element, 'a@b.vn', 'wrong');
    fixture.detectChanges();

    expect(element.querySelector('[role="alert"]')?.textContent).toContain(
      'Email hoặc mật khẩu không đúng.',
    );
  });
});
