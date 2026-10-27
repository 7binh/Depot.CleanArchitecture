import { FormControl } from '@angular/forms';
import { containerNumber, containerNumberProblem } from './container-number';

describe('containerNumberProblem', () => {
  it('accepts a number with the right check digit, ignoring case and spaces', () => {
    expect(containerNumberProblem('CSQU3054383')).toBeNull();
    expect(containerNumberProblem(' csqu3054383 ')).toBeNull();
  });

  it('rejects a wrong check digit', () => {
    expect(containerNumberProblem('CSQU3054384')).toBe('checkDigit');
  });

  it.each(['', 'CSQU305438', 'CSQU30543833', 'CSQA3054383', '1SQU3054383', 'CSQU30543X3'])(
    'rejects the malformed number "%s"',
    (value) => {
      expect(containerNumberProblem(value)).toBe('format');
    },
  );
});

describe('containerNumber validator', () => {
  it('reports the problem as an error key', () => {
    expect(containerNumber(new FormControl('CSQU3054384', { nonNullable: true }))).toEqual({
      checkDigit: true,
    });
    expect(containerNumber(new FormControl('CSQU3054383', { nonNullable: true }))).toBeNull();
  });
});
