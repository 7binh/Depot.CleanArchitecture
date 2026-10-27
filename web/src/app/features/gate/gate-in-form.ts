import { AbstractControl, FormControl, FormGroup, ValidatorFn } from '@angular/forms';
import { ERROR_MESSAGES } from '../../core/api/error-messages';
import { notBlank } from '../../shared/forms/not-blank.validator';
import { SelectOption } from '../../shared/ui/select-field';

export const GRADE_OPTIONS: SelectOption[] = [
  { value: 'A', label: 'Loại A' },
  { value: 'B', label: 'Loại B' },
  { value: 'C', label: 'Loại C' },
];

export const CONDITION_OPTIONS: SelectOption[] = [
  { value: 'Normal', label: 'Bình thường' },
  { value: 'Damaged', label: 'Hư hỏng' },
];

export const VEHICLE_OPTIONS: SelectOption[] = [
  { value: 'Truck', label: 'Xe tải' },
  { value: 'Barge', label: 'Sà lan' },
  { value: 'Other', label: 'Khác' },
];

/** The messages of the Gate In form, keyed by validator error and API error code. */
export const GATE_IN_MESSAGES = {
  number: {
    ...ERROR_MESSAGES,
    required: 'Nhập số container.',
    unknown: 'Chưa có hồ sơ cho số container này.',
  },
  condition: ERROR_MESSAGES,
  note: { ...ERROR_MESSAGES, required: 'Mô tả tình trạng hư hỏng.' },
  vehicle: { ...ERROR_MESSAGES, required: 'Nhập biển số hoặc tên phương tiện.' },
  time: {
    ...ERROR_MESSAGES,
    required: 'Chọn giờ vào.',
    future: 'Giờ vào không được ở tương lai.',
  },
  block: { ...ERROR_MESSAGES, required: 'Chọn block.' },
  bay: { ...ERROR_MESSAGES, required: 'Chọn bay.' },
  row: { ...ERROR_MESSAGES, required: 'Chọn row.' },
  tier: { ...ERROR_MESSAGES, required: 'Chọn tầng.' },
};

/** The current local time as the `yyyy-MM-ddTHH:mm` text a datetime-local input uses. */
export function nowLocal(): string {
  const now = new Date();
  const offsetMs = now.getTimezoneOffset() * 60_000;
  return new Date(now.getTime() - offsetMs).toISOString().slice(0, 16);
}

function notInFuture(control: AbstractControl<string>): { future: true } | null {
  return control.value > nowLocal() ? { future: true } : null;
}

function text(initial: string, validators: ValidatorFn[] = []): FormControl<string> {
  return new FormControl(initial, { nonNullable: true, validators });
}

/** What the form needs to know about its surroundings to validate itself. */
export interface GateInContext {
  hasContainer: (number: string) => boolean;
  isRealBlock: (blockId: string) => boolean;
}

export function buildGateInForm(context: GateInContext) {
  const unknownContainer: ValidatorFn = (c) =>
    context.hasContainer(c.value) ? null : { unknown: true };
  const damageNoteRequired: ValidatorFn = (c) =>
    c.parent?.get('condition')?.value === 'Damaged' && c.value.trim() === ''
      ? { required: true }
      : null;
  const positionRequired: ValidatorFn = (c) =>
    context.isRealBlock(c.parent?.get('blockId')?.value ?? '') && c.value === ''
      ? { required: true }
      : null;

  return new FormGroup({
    number: text('', [notBlank, unknownContainer]),
    grade: text('A'),
    condition: text('Normal'),
    damageNote: text('', [damageNoteRequired]),
    vehicleType: text('Truck'),
    vehicleNumber: text('', [notBlank]),
    gateInAt: text(nowLocal(), [notBlank, notInFuture]),
    blockId: text('', [notBlank]),
    bayId: text('', [positionRequired]),
    rowId: text('', [positionRequired]),
    tier: text('', [positionRequired]),
  });
}

export type GateInForm = ReturnType<typeof buildGateInForm>;
