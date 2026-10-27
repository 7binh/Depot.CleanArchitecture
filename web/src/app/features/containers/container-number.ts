import { AbstractControl, ValidationErrors } from '@angular/forms';

const FORMAT = /^[A-Z]{3}[UJZ]\d{7}$/;
const CHECK_DIGIT_INDEX = 10;
const MODULUS = 11;

// Values of A..Z; multiples of 11 are skipped (ISO 6346).
const LETTER_VALUES = [
  10, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 34, 35, 36,
  37, 38,
];

function charValue(char: string): number {
  const code = char.charCodeAt(0);
  return code >= 65 ? LETTER_VALUES[code - 65] : code - 48;
}

function checkDigit(number: string): number {
  let sum = 0;
  for (let i = 0; i < CHECK_DIGIT_INDEX; i++) {
    sum += charValue(number[i]) * 2 ** i;
  }
  return (sum % MODULUS) % 10;
}

export type ContainerNumberProblem = 'format' | 'checkDigit';

/** What is wrong with a container number, or null when it is valid. The backend stays the final judge. */
export function containerNumberProblem(value: string): ContainerNumberProblem | null {
  const number = value.trim().toUpperCase();
  if (!FORMAT.test(number)) {
    return 'format';
  }
  return Number(number[CHECK_DIGIT_INDEX]) === checkDigit(number) ? null : 'checkDigit';
}

export function containerNumber(control: AbstractControl<string>): ValidationErrors | null {
  const problem = containerNumberProblem(control.value);
  return problem ? { [problem]: true } : null;
}
