import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/** Fails with `{ sameAs: true }` when the control's value differs from `other`'s value. */
export function sameAs(other: AbstractControl): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null =>
    control.value === other.value ? null : { sameAs: true };
}
