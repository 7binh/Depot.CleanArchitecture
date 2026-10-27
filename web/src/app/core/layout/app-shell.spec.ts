import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { AppShell } from './app-shell';

describe('AppShell', () => {
  const logout = vi.fn();
  let fixture: ComponentFixture<AppShell>;
  let element: HTMLElement;

  beforeEach(() => {
    logout.mockReset();
    TestBed.configureTestingModule({
      imports: [AppShell],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: { email: signal('admin@gmail.com'), logout } },
      ],
    });
    fixture = TestBed.createComponent(AppShell);
    fixture.detectChanges();
    element = fixture.nativeElement as HTMLElement;
  });

  function click(label: string): void {
    const button = element.querySelector<HTMLButtonElement>(`button[aria-label="${label}"]`);
    button?.click();
    fixture.detectChanges();
  }

  it('shows the signed-in email and the menu', () => {
    expect(element.textContent).toContain('admin@gmail.com');
    expect(element.querySelector('nav[aria-label="Menu chính"]')?.textContent).toContain(
      'Trang chủ',
    );
  });

  it('signs out from the header button', () => {
    click('Đăng xuất');

    expect(logout).toHaveBeenCalledOnce();
  });

  it('opens the mobile menu and closes it again', () => {
    expect(element.querySelector('[role="dialog"]')).toBeNull();

    click('Mở menu');
    expect(element.querySelector('[role="dialog"]')).not.toBeNull();

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    fixture.detectChanges();
    expect(element.querySelector('[role="dialog"]')).toBeNull();
  });
});
