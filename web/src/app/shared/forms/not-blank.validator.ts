import { AbstractControl, ValidationErrors } from '@angular/forms';

/** Fails with `required` when the text is empty or only spaces, which `Validators.required` allows. */
export function notBlank(control: AbstractControl<string>): ValidationErrors | null {
  return control.value.trim() === '' ? { required: true } : null;
}
