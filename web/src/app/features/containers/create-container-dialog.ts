import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { errorCode, toMessage } from '../../core/api/api-error';
import { ERROR_MESSAGES } from '../../core/api/error-messages';
import { ListLoader } from '../../core/api/list-loader';
import { notBlank } from '../../shared/forms/not-blank.validator';
import { notFuture, parseNumber, positiveNumber } from '../../shared/forms/number-text.validators';
import { Button } from '../../shared/ui/button';
import { Dialog } from '../../shared/ui/dialog';
import { ErrorBanner } from '../../shared/ui/error-banner';
import { SelectField, SelectOption } from '../../shared/ui/select-field';
import { TextField } from '../../shared/ui/text-field';
import {
  ContainerType,
  ContainerTypeService,
} from '../catalogs/container-types/container-type.service';
import { containerNumber } from './container-number';
import { ContainerService } from './container.service';

type Field =
  | 'number'
  | 'containerTypeId'
  | 'isoCode'
  | 'sizeFeet'
  | 'maxWeightKg'
  | 'tareWeightKg'
  | 'manufactureDate';

/** Which field an API error belongs under; errors not listed here go to the banner. */
const FIELD_OF_ERROR: Record<string, Field> = {
  'Number.Empty': 'number',
  'Container.NumberExists': 'number',
  'ContainerNumber.InvalidFormat': 'number',
  'ContainerNumber.InvalidCheckDigit': 'number',
  'ContainerNumber.OwnerMismatch': 'number',
  'Container.OperatorNotFound': 'number',
  'Container.TypeNotFound': 'containerTypeId',
  'IsoCode.Empty': 'isoCode',
  'IsoCode.Invalid': 'isoCode',
  'SizeFeet.Invalid': 'sizeFeet',
  'MaxWeightKg.Invalid': 'maxWeightKg',
  'TareWeightKg.Invalid': 'tareWeightKg',
  'TareWeightKg.ExceedsMax': 'tareWeightKg',
  'ManufactureDate.InFuture': 'manufactureDate',
};

function text(validators: ValidatorFn[]): FormControl<string> {
  return new FormControl('', { nonNullable: true, validators });
}

function tareNotAbove(max: FormControl<string>): ValidatorFn {
  return (control) =>
    parseNumber(control.value) > parseNumber(max.value) ? { tareAbove: true } : null;
}

function buildForm() {
  const maxWeightKg = text([notBlank, positiveNumber]);
  return new FormGroup({
    number: text([notBlank, containerNumber]),
    containerTypeId: text([notBlank]),
    isoCode: text([notBlank, Validators.pattern(/^[A-Za-z0-9]{4}$/)]),
    sizeFeet: text([notBlank]),
    maxWeightKg,
    tareWeightKg: text([notBlank, positiveNumber, tareNotAbove(maxWeightKg)]),
    manufactureDate: text([notBlank, notFuture]),
  });
}

@Component({
  selector: 'app-create-container-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, Button, Dialog, ErrorBanner, SelectField, TextField],
  templateUrl: './create-container-dialog.html',
})
export class CreateContainerDialog implements OnInit {
  private readonly service = inject(ContainerService);
  private readonly typeService = inject(ContainerTypeService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly dialog = viewChild.required(Dialog);

  readonly closed = output();
  readonly created = output();

  protected readonly form = buildForm();
  protected readonly types = new ListLoader<ContainerType>(() => this.typeService.list());
  protected readonly typeOptions = computed<SelectOption[]>(() =>
    this.types.items().map((t) => ({ value: t.id, label: `${t.code} - ${t.name}` })),
  );
  protected readonly sizeOptions: SelectOption[] = [
    { value: '20', label: '20 feet' },
    { value: '40', label: '40 feet' },
  ];

  protected readonly numberMessages = {
    ...ERROR_MESSAGES,
    required: 'Nhập số container.',
    format: 'Số container gồm 3 chữ cái hãng, 1 chữ cái U, J hoặc Z và 7 chữ số.',
    checkDigit: 'Số container sai chữ số kiểm tra (ISO 6346).',
  };
  protected readonly typeMessages = { ...ERROR_MESSAGES, required: 'Chọn loại container.' };
  protected readonly isoMessages = {
    ...ERROR_MESSAGES,
    required: 'Nhập mã ISO.',
    pattern: 'Mã ISO gồm đúng 4 chữ cái hoặc chữ số.',
  };
  protected readonly sizeMessages = { ...ERROR_MESSAGES, required: 'Chọn cỡ container.' };
  protected readonly maxMessages = {
    ...ERROR_MESSAGES,
    required: 'Nhập tải trọng tối đa.',
    positive: 'Tải trọng tối đa phải là số lớn hơn 0.',
  };
  protected readonly tareMessages = {
    ...ERROR_MESSAGES,
    required: 'Nhập trọng lượng vỏ.',
    positive: 'Trọng lượng vỏ phải là số lớn hơn 0.',
    tareAbove: 'Trọng lượng vỏ không được lớn hơn tải trọng tối đa.',
  };
  protected readonly dateMessages = {
    ...ERROR_MESSAGES,
    required: 'Chọn ngày sản xuất.',
    future: 'Ngày sản xuất không được ở tương lai.',
  };

  protected readonly notice = signal<string | null>(null);
  protected readonly submitting = signal(false);

  constructor() {
    this.form.controls.maxWeightKg.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.form.controls.tareWeightKg.updateValueAndValidity());
  }

  ngOnInit(): void {
    this.types.load();
  }

  protected submit(): void {
    this.notice.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    const value = this.form.getRawValue();
    this.service
      .create({
        number: value.number.trim(),
        containerTypeId: value.containerTypeId,
        isoCode: value.isoCode.trim(),
        sizeFeet: Number(value.sizeFeet),
        maxWeightKg: parseNumber(value.maxWeightKg),
        tareWeightKg: parseNumber(value.tareWeightKg),
        manufactureDate: value.manufactureDate,
      })
      .subscribe({
        next: () => {
          this.created.emit();
          this.dialog().close();
        },
        error: (error: unknown) => this.fail(error),
      });
  }

  private fail(error: unknown): void {
    this.submitting.set(false);
    const code = errorCode(error);
    const field = code ? FIELD_OF_ERROR[code] : undefined;
    if (code && field) {
      const control = this.form.controls[field];
      control.setErrors({ [code]: true });
      control.markAsTouched();
      return;
    }
    this.notice.set(toMessage(error));
  }
}
