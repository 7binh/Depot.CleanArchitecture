import { FormControl } from '@angular/forms';
import { TestBed } from '@angular/core/testing';
import { SearchSelect } from './search-select';

const OPTIONS = [
  { value: 'a', label: 'Alpha one' },
  { value: 'b', label: 'Beta two' },
  { value: 'c', label: 'Alpha three' },
];

function setup(limit = 8) {
  const control = new FormControl('', { nonNullable: true });
  const fixture = TestBed.createComponent(SearchSelect);
  fixture.componentRef.setInput('label', 'Mục');
  fixture.componentRef.setInput('control', control);
  fixture.componentRef.setInput('options', OPTIONS);
  fixture.componentRef.setInput('limit', limit);
  fixture.detectChanges();
  const element = fixture.nativeElement as HTMLElement;
  const input = element.querySelector('input') as HTMLInputElement;
  return { control, fixture, element, input };
}

function typeText(page: ReturnType<typeof setup>, text: string): void {
  page.input.dispatchEvent(new Event('focus'));
  page.input.value = text;
  page.input.dispatchEvent(new Event('input'));
  page.fixture.detectChanges();
}

function press(page: ReturnType<typeof setup>, key: string): void {
  page.input.dispatchEvent(new KeyboardEvent('keydown', { key, cancelable: true }));
  page.fixture.detectChanges();
}

function listed(page: ReturnType<typeof setup>): string[] {
  return [...page.element.querySelectorAll('[role="option"]')].map(
    (o) => o.textContent?.trim() ?? '',
  );
}

describe('SearchSelect', () => {
  it('narrows the list to the labels that contain the text', () => {
    const page = setup();
    typeText(page, 'alpha');
    expect(listed(page)).toEqual(['Alpha one', 'Alpha three']);
  });

  it('shows at most the limit', () => {
    const page = setup(2);
    typeText(page, '');
    expect(listed(page).length).toBe(2);
  });

  it('picks the highlighted option with the arrow keys and Enter', () => {
    const page = setup();
    typeText(page, 'alpha');
    press(page, 'ArrowDown');
    press(page, 'Enter');
    expect(page.control.value).toBe('c');
    expect(page.input.value).toBe('Alpha three');
    expect(page.element.querySelector('[role="listbox"]')).toBeNull();
  });

  it('picks an option with the mouse', () => {
    const page = setup();
    typeText(page, 'beta');
    page.element.querySelector('[role="option"]')?.dispatchEvent(new Event('mousedown'));
    page.fixture.detectChanges();
    expect(page.control.value).toBe('b');
  });

  it('closes the list on Escape', () => {
    const page = setup();
    typeText(page, 'a');
    press(page, 'Escape');
    expect(page.element.querySelector('[role="listbox"]')).toBeNull();
  });

  it('drops the choice when the text is edited again', () => {
    const page = setup();
    page.control.setValue('a');
    page.fixture.detectChanges();
    expect(page.input.value).toBe('Alpha one');
    typeText(page, 'Alph');
    expect(page.control.value).toBe('');
  });

  it('clears the text when the control is reset from outside', () => {
    const page = setup();
    page.control.setValue('b');
    page.fixture.detectChanges();
    page.control.reset();
    page.fixture.detectChanges();
    expect(page.input.value).toBe('');
  });
});
