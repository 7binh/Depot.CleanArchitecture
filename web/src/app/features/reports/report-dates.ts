import { AbstractControl, ValidationErrors } from '@angular/forms';

/** A date as `yyyy-MM-dd` in local time, the format of a date input and of the report API. */
export function toIsoDate(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}

export function today(): string {
  return toIsoDate(new Date());
}

/** The date `days` before today. */
export function daysAgo(days: number): string {
  const date = new Date();
  date.setDate(date.getDate() - days);
  return toIsoDate(date);
}

/** Empty is `required`'s job; a date after today fails with `future`. */
export function notFuture(control: AbstractControl<string>): ValidationErrors | null {
  const value = control.value;
  return value && value > today() ? { future: true } : null;
}
