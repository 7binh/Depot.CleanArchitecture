import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { forkJoin, map } from 'rxjs';
import { errorCode, toMessage } from '../../core/api/api-error';
import { ERROR_MESSAGES } from '../../core/api/error-messages';
import { ListLoader, LoadStatus } from '../../core/api/list-loader';
import { notBlank } from '../../shared/forms/not-blank.validator';
import { Button } from '../../shared/ui/button';
import { ErrorBanner } from '../../shared/ui/error-banner';
import { LoadState } from '../../shared/ui/load-state';
import { PageHeader } from '../../shared/ui/page-header';
import { SelectField, SelectOption } from '../../shared/ui/select-field';
import { TextField } from '../../shared/ui/text-field';
import { ContainerTypeService } from '../catalogs/container-types/container-type.service';
import { LineOperatorService } from '../catalogs/line-operators/line-operator.service';
import { ContainerRow, toContainerRows } from '../containers/container-rows';
import { ContainerService } from '../containers/container.service';
import { ReleaseOrder, ReleaseOrderService } from '../release-orders/release-order.service';
import { VEHICLE_OPTIONS, nowLocal, notInFuture } from './gate-in-form';
import { GateService, NewGateOut, VehicleType } from './gate.service';

type Field = 'containerId' | 'releaseOrderId' | 'gateOutAt';

/** Which field an API error belongs under; errors not listed here go to the banner. */
const FIELD_OF_ERROR: Record<string, Field> = {
  'GateOut.ContainerNotFound': 'containerId',
  'GateOut.ContainerNotInYard': 'containerId',
  'Stay.HasContainerAbove': 'containerId',
  'Stay.AlreadyClosed': 'containerId',
  'GateOut.ReleaseOrderNotFound': 'releaseOrderId',
  'ReleaseOrder.Expired': 'releaseOrderId',
  'ReleaseOrder.LineFull': 'releaseOrderId',
  'ReleaseOrder.OperatorMismatch': 'releaseOrderId',
  'ReleaseOrder.NoMatchingLine': 'releaseOrderId',
  'GateOutAt.InFuture': 'gateOutAt',
  'GateOutAt.BeforeGateIn': 'gateOutAt',
};

function text(initial: string, required = false): FormControl<string> {
  return new FormControl(initial, { nonNullable: true, validators: required ? [notBlank] : [] });
}

/** An order can take this container when it is the same operator, still valid and has room on the matching line. */
function canRelease(order: ReleaseOrder, container: ContainerRow): boolean {
  return (
    order.lineOperatorId === container.lineOperatorId &&
    !order.isExpired &&
    order.items.some(
      (item) =>
        item.containerTypeId === container.containerTypeId &&
        item.sizeFeet === container.sizeFeet &&
        item.remaining > 0,
    )
  );
}

function worst(statuses: LoadStatus[]): LoadStatus {
  if (statuses.includes('failed')) {
    return 'failed';
  }
  return statuses.includes('loading') ? 'loading' : 'ready';
}

@Component({
  selector: 'app-gate-out-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    Button,
    ErrorBanner,
    LoadState,
    PageHeader,
    SelectField,
    TextField,
  ],
  templateUrl: './gate-out-page.html',
  host: { class: 'flex flex-col gap-8' },
})
export class GateOutPage implements OnInit {
  private readonly containerService = inject(ContainerService);
  private readonly operators = inject(LineOperatorService);
  private readonly types = inject(ContainerTypeService);
  private readonly orderService = inject(ReleaseOrderService);
  private readonly gate = inject(GateService);

  private readonly containers = new ListLoader<ContainerRow>(() =>
    forkJoin([this.containerService.list(), this.operators.list(), this.types.list()]).pipe(
      map(([containers, operators, types]) => toContainerRows(containers, operators, types)),
    ),
  );
  private readonly stays = new ListLoader(() => this.gate.inYard());
  private readonly orders = new ListLoader(() => this.orderService.list());

  protected readonly status = computed(() =>
    worst([this.containers.status(), this.stays.status(), this.orders.status()]),
  );
  protected readonly failure = computed(
    () => this.containers.failure() || this.stays.failure() || this.orders.failure(),
  );

  protected readonly form = new FormGroup({
    containerId: text('', true),
    releaseOrderId: text('', true),
    vehicleType: text('Truck'),
    vehicleNumber: text('', true),
    gateOutAt: new FormControl(nowLocal(), {
      nonNullable: true,
      validators: [notBlank, notInFuture],
    }),
  });
  protected readonly vehicleOptions = VEHICLE_OPTIONS;
  protected readonly messages = {
    container: { ...ERROR_MESSAGES, required: 'Chọn container đang trong bãi.' },
    order: { ...ERROR_MESSAGES, required: 'Chọn lệnh giao.' },
    vehicle: { ...ERROR_MESSAGES, required: 'Nhập biển số hoặc tên phương tiện.' },
    time: {
      ...ERROR_MESSAGES,
      required: 'Chọn giờ ra.',
      future: 'Giờ ra không được ở tương lai.',
    },
  };

  private readonly containerId = toSignal(this.form.controls.containerId.valueChanges, {
    initialValue: '',
  });

  private readonly inYard = computed(() => {
    const ids = new Set(this.stays.items().map((stay) => stay.containerId));
    return this.containers.items().filter((row) => ids.has(row.id));
  });
  protected readonly containerOptions = computed<SelectOption[]>(() =>
    this.inYard().map((row) => ({
      value: row.id,
      label: `${row.number} (${row.lineOperator}, ${row.containerType}, ${row.size})`,
    })),
  );
  protected readonly chosen = computed(() =>
    this.inYard().find((row) => row.id === this.containerId()),
  );
  protected readonly orderOptions = computed<SelectOption[]>(() => {
    const container = this.chosen();
    if (!container) {
      return [];
    }
    return this.orders
      .items()
      .filter((order) => canRelease(order, container))
      .map((order) => ({ value: order.id, label: `${order.orderNo} - ${order.exportVessel}` }));
  });

  protected readonly notice = signal<string | null>(null);
  protected readonly submitting = signal(false);
  protected readonly done = signal<string | null>(null);

  constructor() {
    this.form.controls.containerId.valueChanges.subscribe(() => {
      this.form.controls.releaseOrderId.setValue('');
    });
  }

  ngOnInit(): void {
    this.containers.load();
    this.stays.load();
    this.orders.load();
  }

  protected submit(): void {
    this.notice.set(null);
    this.done.set(null);
    const container = this.chosen();
    if (this.form.invalid || !container) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    this.gate.gateOut(this.toRequest()).subscribe({
      next: () => this.succeed(container.number),
      error: (error: unknown) => this.fail(error),
    });
  }

  private toRequest(): NewGateOut {
    const v = this.form.getRawValue();
    return {
      containerId: v.containerId,
      releaseOrderId: v.releaseOrderId,
      gateOutAt: new Date(v.gateOutAt).toISOString(),
      outVehicleType: v.vehicleType as VehicleType,
      outVehicleNumber: v.vehicleNumber.trim(),
    };
  }

  private succeed(number: string): void {
    this.submitting.set(false);
    this.done.set(number);
    this.form.reset();
    this.form.controls.gateOutAt.setValue(nowLocal());
    this.stays.load();
    this.orders.load();
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
