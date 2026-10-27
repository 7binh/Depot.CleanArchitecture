import {
  AbstractControl,
  FormArray,
  FormControl,
  FormGroup,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { ERROR_MESSAGES } from '../../core/api/error-messages';
import { notBlank } from '../../shared/forms/not-blank.validator';
import { parseNumber } from '../../shared/forms/number-text.validators';
import { SelectOption } from '../../shared/ui/select-field';

export const SIZE_OPTIONS: SelectOption[] = [
  { value: '20', label: '20 feet' },
  { value: '40', label: '40 feet' },
];

/** The messages of the release order form, keyed by validator error and API error code. */
export const ORDER_MESSAGES = {
  operator: { ...ERROR_MESSAGES, required: 'Chọn hãng khai thác.' },
  customer: { ...ERROR_MESSAGES, required: 'Chọn khách hàng.' },
  orderNo: {
    ...ERROR_MESSAGES,
    required: 'Nhập số lệnh giao.',
    maxlength: ERROR_MESSAGES['OrderNo.TooLong'],
  },
  vessel: {
    ...ERROR_MESSAGES,
    required: 'Nhập tên tàu xuất.',
    maxlength: ERROR_MESSAGES['ExportVessel.TooLong'],
  },
  issued: { ...ERROR_MESSAGES, required: 'Chọn ngày cấp.' },
  expiry: {
    ...ERROR_MESSAGES,
    required: 'Chọn hạn của lệnh.',
    beforeIssued: ERROR_MESSAGES['ExpiryDate.BeforeIssuedDate'],
  },
  type: { ...ERROR_MESSAGES, required: 'Chọn loại container.' },
  size: { ...ERROR_MESSAGES, required: 'Chọn cỡ container.' },
  quantity: {
    ...ERROR_MESSAGES,
    required: 'Nhập số lượng.',
    whole: 'Số lượng phải là số nguyên lớn hơn 0.',
  },
};

function text(initial: string, validators: ValidatorFn[] = []): FormControl<string> {
  return new FormControl(initial, { nonNullable: true, validators });
}

function wholeNumber(control: AbstractControl<string>): ValidationErrors | null {
  const value = parseNumber(control.value);
  return Number.isInteger(value) && value > 0 ? null : { whole: true };
}

function notBeforeIssued(control: AbstractControl<string>): ValidationErrors | null {
  const issued = control.parent?.get('issuedDate')?.value as string | undefined;
  return issued && control.value && control.value < issued ? { beforeIssued: true } : null;
}

/** Each pair of container type and size may appear on one line only. */
function uniqueLines(lines: AbstractControl): ValidationErrors | null {
  const keys = (lines as FormArray<FormGroup>).controls
    .map((line) => line.getRawValue() as { containerTypeId: string; sizeFeet: string })
    .filter((line) => line.containerTypeId && line.sizeFeet)
    .map((line) => `${line.containerTypeId}/${line.sizeFeet}`);
  return new Set(keys).size === keys.length ? null : { duplicate: true };
}

export function newLine() {
  return new FormGroup({
    containerTypeId: text('', [notBlank]),
    sizeFeet: text('', [notBlank]),
    quantity: text('', [notBlank, wholeNumber]),
  });
}

/** Today as `yyyy-MM-dd`, local time. */
export function today(): string {
  return new Date().toLocaleDateString('sv-SE');
}

export function buildOrderForm() {
  const form = new FormGroup({
    lineOperatorId: text('', [notBlank]),
    customerId: text('', [notBlank]),
    orderNo: text('', [notBlank, Validators.maxLength(50)]),
    exportVessel: text('', [notBlank, Validators.maxLength(100)]),
    issuedDate: text(today(), [notBlank]),
    expiryDate: text('', [notBlank, notBeforeIssued]),
    lines: new FormArray([newLine()], { validators: [uniqueLines] }),
  });
  form.controls.issuedDate.valueChanges.subscribe(() =>
    form.controls.expiryDate.updateValueAndValidity(),
  );
  return form;
}
