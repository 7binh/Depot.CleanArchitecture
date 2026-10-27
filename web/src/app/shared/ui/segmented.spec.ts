import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Segmented } from './segmented';

@Component({
  imports: [Segmented],
  template: `<app-segmented label="Chọn tầng" [options]="options" [(value)]="value" />`,
})
class Host {
  readonly options = [
    { value: '1', label: 'Tầng 1' },
    { value: '2', label: 'Tầng 2' },
  ];
  readonly value = signal('1');
}

describe('Segmented', () => {
  it('marks the active segment and changes it on click', () => {
    const fixture = TestBed.createComponent(Host);
    fixture.detectChanges();
    const buttons = [...(fixture.nativeElement as HTMLElement).querySelectorAll('button')];
    expect(buttons.map((b) => b.getAttribute('aria-pressed'))).toEqual(['true', 'false']);

    buttons[1].click();
    fixture.detectChanges();

    expect(fixture.componentInstance.value()).toBe('2');
    expect(buttons.map((b) => b.getAttribute('aria-pressed'))).toEqual(['false', 'true']);
  });
});
