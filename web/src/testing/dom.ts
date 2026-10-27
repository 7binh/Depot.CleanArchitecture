import { ComponentFixture } from '@angular/core/testing';

function root(fixture: ComponentFixture<unknown>): HTMLElement {
  return fixture.nativeElement as HTMLElement;
}

/** Clicks the first button whose text contains `label`, then renders. */
export function click(fixture: ComponentFixture<unknown>, label: string): void {
  const buttons = [...root(fixture).querySelectorAll('button')];
  buttons.find((b) => b.textContent?.includes(label))?.click();
  fixture.detectChanges();
}

/** Types into the text field whose label contains `label`, then leaves it (touched). */
export function type(fixture: ComponentFixture<unknown>, label: string, value: string): void {
  const fields = [...root(fixture).querySelectorAll('app-text-field')];
  const input = fields.find((f) => f.textContent?.includes(label))?.querySelector('input');
  if (input) {
    input.value = value;
    input.dispatchEvent(new Event('input'));
    input.dispatchEvent(new Event('blur'));
  }
  fixture.detectChanges();
}

/** Picks `value` in the select field whose label contains `label`. */
export function choose(fixture: ComponentFixture<unknown>, label: string, value: string): void {
  const fields = [...root(fixture).querySelectorAll('app-select-field')];
  const select = fields.find((f) => f.textContent?.includes(label))?.querySelector('select');
  if (select) {
    select.value = value;
    select.dispatchEvent(new Event('change'));
    select.dispatchEvent(new Event('blur'));
  }
  fixture.detectChanges();
}
