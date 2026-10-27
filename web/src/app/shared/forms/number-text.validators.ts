import { AbstractControl, ValidationErrors } from '@angular/forms';

/** Reads a number typed as text; a comma counts as the decimal point. NaN when it is not a number. */
export function parseNumber(text: string): number {
  const clean = text.trim().replace(',', '.');
  return clean === '' ? Number.NaN : Number(clean);
}

export function positiveNumber(control: AbstractControl<string>): ValidationErrors | null {
  return parseNumber(control.value) > 0 ? null : { positive: true };
}

/** Fails with `future` when the `yyyy-MM-dd` date is after today. */
export function notFuture(control: AbstractControl<string>): ValidationErrors | null {
  const today = new Date().toLocaleDateString('sv-SE');
  return control.value > today ? { future: true } : null;
}
