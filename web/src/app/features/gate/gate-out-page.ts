import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged, map } from 'rxjs';
import { errorCode, toMessage } from '../../core/api/api-error';
import { ERROR_MESSAGES } from '../../core/api/error-messages';
import { ListLoader } from '../../core/api/list-loader';
import { notBlank } from '../../shared/forms/not-blank.validator';
import { Button } from '../../shared/ui/button';
import { Card } from '../../shared/ui/card';
import { ErrorBanner } from '../../shared/ui/error-banner';
import { LoadState } from '../../shared/ui/load-state';
import { PageHeader } from '../../shared/ui/page-header';
import { SearchSelect } from '../../shared/ui/search-select';
import { SelectField, SelectOption } from '../../shared/ui/select-field';
import { TextField } from '../../shared/ui/text-field';
import { ContainerTypeService } from '../catalogs/container-types/container-type.service';
import { LineOperatorService } from '../catalogs/line-operators/line-operator.service';
import { ContainerListItem, ContainerService } from '../containers/container.service';
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

/** Orders and containers come one page at a time; a page this big holds all that are open in practice. */
const PAGE_SIZE = 100;
/** Typing waits this long for a pause before the containers are searched again. */
const SEARCH_DELAY_MS = 300;

function text(initial: string, required = false): FormControl<string> {
  return new FormControl(initial, { nonNullable: true, validators: required ? [notBlank] : [] });
}

/** An order can take this container when it is the same operator, still valid and has room on the matching line. */
function canRelease(order: ReleaseOrder, container: ContainerListItem): boolean {
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

@Component({
  selector: 'app-gate-out-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    Card,
    ReactiveFormsModule,
    Button,
    ErrorBanner,
    LoadState,
    PageHeader,
    SearchSelect,
    SelectField,
    TextField,
  ],
  templateUrl: './gate-out-page.html',
  host: { class: 'flex flex-col gap-6' },
})
export class GateOutPage implements OnInit {
  private readonly containerService = inject(ContainerService);
  private readonly operators = inject(LineOperatorService);
  private readonly types = inject(ContainerTypeService);
  private readonly orderService = inject(ReleaseOrderService);
  private readonly gate = inject(GateService);
  private readonly route = inject(ActivatedRoute);
  /** An order id from the address (the Gate Out button of the order page), chosen once the open orders arrive. */
  private preselected = this.route.snapshot.queryParamMap.get('order');

  /** Names for the order summary; the form works without them, so they never block the page. */
  private readonly operatorNames = new ListLoader(() => this.operators.list());
  private readonly typeNames = new ListLoader(() => this.types.list());
  private readonly orders = new ListLoader(() =>
    this.orderService.searchOpen(1, PAGE_SIZE).pipe(map((paged) => paged.items)),
  );
  private readonly containerQuery = signal('');
  /** The containers in the yard that belong to the chosen order's operator and match the typed text. */
  private readonly containers = new ListLoader<ContainerListItem>(() =>
    this.containerService
      .search(
        {
          number: this.containerQuery(),
          lineOperatorId: this.order()?.lineOperatorId,
          inYard: true,
        },
        1,
        PAGE_SIZE,
      )
      .pipe(map((paged) => paged.items)),
  );
  private readonly typed = new Subject<string>();

  protected readonly status = this.orders.status;
  protected readonly failure = this.orders.failure;
  protected readonly containerFailure = computed(() =>
    this.containers.status() === 'failed' ? this.containers.failure() : null,
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

  private readonly orderId = toSignal(this.form.controls.releaseOrderId.valueChanges, {
    initialValue: '',
  });
  private readonly containerId = toSignal(this.form.controls.containerId.valueChanges, {
    initialValue: '',
  });

  protected readonly orderOptions = computed<SelectOption[]>(() =>
    this.orders
      .items()
      .map((order) => ({ value: order.id, label: order.orderNo, hint: order.exportVessel })),
  );
  protected readonly order = computed(() =>
    this.orders.items().find((order) => order.id === this.orderId()),
  );
  protected readonly orderSummary = computed(() => {
    const order = this.order();
    if (!order) {
      return null;
    }
    const operator = this.operatorNames.items().find((o) => o.id === order.lineOperatorId);
    const lines = order.items.map((item) => {
      const type = this.typeNames.items().find((t) => t.id === item.containerTypeId);
      return `${type?.code ?? '?'} cỡ ${item.sizeFeet} ft: còn ${item.remaining}/${item.quantity}`;
    });
    return { operator: operator?.code ?? '?', order, lines };
  });

  protected readonly containerOptions = computed<SelectOption[]>(() => {
    const order = this.order();
    if (!order) {
      return [];
    }
    return this.containers
      .items()
      .filter((row) => canRelease(order, row))
      .map((row) => ({
        value: row.id,
        label: row.number,
        hint: `${row.lineOperatorCode} · ${row.containerTypeCode} · ${row.sizeFeet} ft`,
      }));
  });
  protected readonly containerEmpty = computed(() =>
    this.order() ? 'Không có container nào trong bãi khớp lệnh này.' : 'Chọn lệnh giao trước.',
  );
  protected readonly chosen = computed(() =>
    this.containers.items().find((row) => row.id === this.containerId()),
  );

  protected readonly notice = signal<string | null>(null);
  protected readonly submitting = signal(false);
  protected readonly done = signal<string | null>(null);

  constructor() {
    this.form.controls.releaseOrderId.valueChanges
      .pipe(distinctUntilChanged(), takeUntilDestroyed())
      .subscribe(() => {
        this.form.controls.containerId.setValue('');
        this.searchContainers('');
      });
    this.typed
      .pipe(debounceTime(SEARCH_DELAY_MS), takeUntilDestroyed())
      .subscribe((text) => this.searchContainers(text));
    effect(() => {
      const ready = this.orders.status() === 'ready';
      const items = this.orders.items();
      untracked(() => this.choosePreselected(ready, items));
    });
  }

  ngOnInit(): void {
    this.orders.load();
    this.operatorNames.load();
    this.typeNames.load();
  }

  protected typeContainer(text: string): void {
    this.typed.next(text.trim());
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
    this.orders.load(true);
  }

  /** Chooses the order named in the address, if it is still open; an order that is not open is left unchosen. */
  private choosePreselected(ready: boolean, orders: ReleaseOrder[]): void {
    if (!ready || this.preselected === null) {
      return;
    }
    if (orders.some((order) => order.id === this.preselected)) {
      this.form.controls.releaseOrderId.setValue(this.preselected);
    }
    this.preselected = null;
  }

  /** Asks the server for the containers in the yard; with no order chosen there is nothing to offer. */
  private searchContainers(text: string): void {
    this.containerQuery.set(text);
    if (this.order()) {
      this.containers.load(true);
    } else {
      this.containers.items.set([]);
    }
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
