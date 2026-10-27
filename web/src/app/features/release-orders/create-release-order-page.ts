import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { errorCode, toMessage } from '../../core/api/api-error';
import { ERROR_MESSAGES } from '../../core/api/error-messages';
import { ListLoader, worstStatus } from '../../core/api/list-loader';
import { parseNumber } from '../../shared/forms/number-text.validators';
import { Button } from '../../shared/ui/button';
import { Card } from '../../shared/ui/card';
import { ErrorBanner } from '../../shared/ui/error-banner';
import { Icon } from '../../shared/ui/icon';
import { LoadState } from '../../shared/ui/load-state';
import { PageHeader } from '../../shared/ui/page-header';
import { SelectField, SelectOption } from '../../shared/ui/select-field';
import { TextField } from '../../shared/ui/text-field';
import { ContainerTypeService } from '../catalogs/container-types/container-type.service';
import { CustomerService } from '../catalogs/customers/customer.service';
import { LineOperatorService } from '../catalogs/line-operators/line-operator.service';
import { ORDER_MESSAGES, SIZE_OPTIONS, buildOrderForm, newLine } from './release-order-form';
import { NewReleaseOrder, ReleaseOrderService } from './release-order.service';

type Field = 'lineOperatorId' | 'customerId' | 'orderNo' | 'exportVessel' | 'expiryDate';

/** Which field an API error belongs under; errors about the lines show under the lines, others in the banner. */
const FIELD_OF_ERROR: Record<string, Field> = {
  'ReleaseOrder.OperatorNotFound': 'lineOperatorId',
  'ReleaseOrder.CustomerNotFound': 'customerId',
  'OrderNo.Empty': 'orderNo',
  'OrderNo.TooLong': 'orderNo',
  'ReleaseOrder.OrderNoExists': 'orderNo',
  'ExportVessel.Empty': 'exportVessel',
  'ExportVessel.TooLong': 'exportVessel',
  'ExpiryDate.BeforeIssuedDate': 'expiryDate',
};
const LINE_ERRORS = new Set([
  'Lines.Empty',
  'Lines.Duplicate',
  'Quantity.Invalid',
  'ReleaseOrder.TypeNotFound',
]);

@Component({
  selector: 'app-create-release-order-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    Card,
    ReactiveFormsModule,
    Button,
    ErrorBanner,
    Icon,
    LoadState,
    PageHeader,
    RouterLink,
    SelectField,
    TextField,
  ],
  templateUrl: './create-release-order-page.html',
  host: { class: 'flex flex-col gap-6' },
})
export class CreateReleaseOrderPage implements OnInit {
  private readonly service = inject(ReleaseOrderService);
  private readonly router = inject(Router);

  private readonly operatorService = inject(LineOperatorService);
  private readonly customerService = inject(CustomerService);
  private readonly typeService = inject(ContainerTypeService);

  private readonly operators = new ListLoader(() => this.operatorService.list());
  private readonly customers = new ListLoader(() => this.customerService.list());
  private readonly types = new ListLoader(() => this.typeService.list());

  protected readonly status = computed(() =>
    worstStatus([this.operators.status(), this.customers.status(), this.types.status()]),
  );
  protected readonly failure = computed(
    () => this.operators.failure() || this.customers.failure() || this.types.failure(),
  );
  protected readonly operatorOptions = computed<SelectOption[]>(() =>
    this.operators.items().map((o) => ({ value: o.id, label: `${o.code} - ${o.name}` })),
  );
  protected readonly customerOptions = computed<SelectOption[]>(() =>
    this.customers.items().map((c) => ({ value: c.id, label: c.name })),
  );
  protected readonly typeOptions = computed<SelectOption[]>(() =>
    this.types.items().map((t) => ({ value: t.id, label: `${t.code} - ${t.name}` })),
  );

  protected readonly form = buildOrderForm();
  protected readonly sizeOptions = SIZE_OPTIONS;
  protected readonly messages = ORDER_MESSAGES;

  protected readonly notice = signal<string | null>(null);
  protected readonly linesNotice = signal<string | null>(null);
  protected readonly submitting = signal(false);

  ngOnInit(): void {
    this.operators.load();
    this.customers.load();
    this.types.load();
  }

  /** The problem with the lines as a whole: one the API reported, or a repeated type and size. */
  protected linesError(): string | null {
    if (this.linesNotice()) {
      return this.linesNotice();
    }
    return this.form.controls.lines.hasError('duplicate')
      ? ERROR_MESSAGES['Lines.Duplicate']
      : null;
  }

  protected addLine(): void {
    this.form.controls.lines.push(newLine());
  }

  protected removeLine(index: number): void {
    this.form.controls.lines.removeAt(index);
  }

  protected submit(): void {
    this.notice.set(null);
    this.linesNotice.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    this.service.create(this.toRequest()).subscribe({
      next: (order) => void this.router.navigate(['/lenh-giao', order.id]),
      error: (error: unknown) => this.fail(error),
    });
  }

  private toRequest(): NewReleaseOrder {
    const v = this.form.getRawValue();
    return {
      lineOperatorId: v.lineOperatorId,
      customerId: v.customerId,
      orderNo: v.orderNo.trim(),
      issuedDate: v.issuedDate,
      expiryDate: v.expiryDate,
      exportVessel: v.exportVessel.trim(),
      lines: v.lines.map((line) => ({
        containerTypeId: line.containerTypeId,
        sizeFeet: Number(line.sizeFeet),
        quantity: parseNumber(line.quantity),
      })),
    };
  }

  private fail(error: unknown): void {
    this.submitting.set(false);
    const code = errorCode(error);
    const field = code ? FIELD_OF_ERROR[code] : undefined;
    if (code && field) {
      const control = this.form.controls[field];
      control.setErrors({ [code]: true });
      control.markAsTouched();
    } else if (code && LINE_ERRORS.has(code)) {
      this.linesNotice.set(ERROR_MESSAGES[code]);
    } else {
      this.notice.set(toMessage(error));
    }
  }
}
