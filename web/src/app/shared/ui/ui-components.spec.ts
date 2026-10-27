import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Avatar } from './avatar';
import { EmptyState } from './empty-state';
import { ErrorBanner } from './error-banner';
import { PageHeader } from './page-header';
import { Skeleton } from './skeleton';

@Component({
  imports: [Avatar, EmptyState, ErrorBanner, PageHeader, Skeleton],
  template: `
    <app-avatar name="  admin@gmail.com" />
    <app-page-header title="Container" description="Danh sách container">
      <button type="button">Thêm</button>
    </app-page-header>
    <app-error-banner>Không tải được dữ liệu.</app-error-banner>
    <app-empty-state icon="box" message="Chưa có container." />
    <div appSkeleton [radius]="radius"></div>
  `,
})
class Host {
  readonly radius = 'card' as const;
}

describe('shared UI', () => {
  let element: HTMLElement;

  beforeEach(() => {
    const fixture = TestBed.createComponent(Host);
    fixture.detectChanges();
    element = fixture.nativeElement as HTMLElement;
  });

  it('avatar shows the first letter in capitals', () => {
    expect(element.querySelector('app-avatar')?.textContent).toBe('A');
  });

  it('page header shows the title, description and projected action', () => {
    const header = element.querySelector('app-page-header');
    expect(header?.querySelector('h1')?.textContent).toBe('Container');
    expect(header?.textContent).toContain('Danh sách container');
    expect(header?.querySelector('button')?.textContent).toBe('Thêm');
  });

  it('error banner is announced as an alert', () => {
    const banner = element.querySelector('app-error-banner');
    expect(banner?.getAttribute('role')).toBe('alert');
    expect(banner?.textContent).toContain('Không tải được dữ liệu.');
  });

  it('empty state shows its message', () => {
    expect(element.querySelector('app-empty-state')?.textContent).toContain('Chưa có container.');
  });

  it('skeleton uses the requested radius', () => {
    expect(element.querySelector('[appSkeleton]')?.classList).toContain('rounded-card');
  });
});
