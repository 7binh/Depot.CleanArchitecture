import { ContainerType } from '../catalogs/container-types/container-type.service';
import { Customer } from '../catalogs/customers/customer.service';
import { LineOperator } from '../catalogs/line-operators/line-operator.service';
import { VEHICLE_OPTIONS } from '../gate/gate-in-form';
import { formatDateTime } from '../stays/stay-rows';
import { OrderStatus, formatDate, statusOf } from './release-order-status';
import { ReleaseOrder, ReleaseOrderDelivery } from './release-order.service';

export interface OrderLineView {
  type: string;
  size: string;
  quantity: number;
  delivered: number;
  remaining: number;
  /** Whole percent of the line already delivered, for the progress bar. */
  percent: number;
}

/** A container that left on the order, ready to show in a table. */
export interface DeliveryView {
  id: string;
  number: string;
  type: string;
  size: string;
  gateOut: string;
  vehicle: string;
}

/** One order with the names worked out, ready to show on its detail page. */
export interface ReleaseOrderDetail {
  id: string;
  orderNo: string;
  lineOperator: string;
  customer: string;
  exportVessel: string;
  issuedDate: string;
  expiryDate: string;
  status: OrderStatus;
  lines: OrderLineView[];
  deliveries: DeliveryView[];
}

function vehicleOf(delivery: ReleaseOrderDelivery): string {
  const kind = VEHICLE_OPTIONS.find((o) => o.value === delivery.outVehicleType)?.label ?? '';
  return [kind, delivery.outVehicleNumber].filter(Boolean).join(' ');
}

export function toOrderDetail(
  order: ReleaseOrder,
  operators: LineOperator[],
  customers: Customer[],
  types: ContainerType[],
  deliveries: ReleaseOrderDelivery[],
): ReleaseOrderDetail {
  const operator = operators.find((o) => o.id === order.lineOperatorId);
  const customer = customers.find((c) => c.id === order.customerId);
  return {
    id: order.id,
    orderNo: order.orderNo,
    lineOperator: operator ? `${operator.code} - ${operator.name}` : '',
    customer: customer?.name ?? '',
    exportVessel: order.exportVessel,
    issuedDate: formatDate(order.issuedDate),
    expiryDate: formatDate(order.expiryDate),
    status: statusOf(order),
    lines: order.items.map((item) => ({
      type: types.find((t) => t.id === item.containerTypeId)?.code ?? '',
      size: `${item.sizeFeet} ft`,
      quantity: item.quantity,
      delivered: item.delivered,
      remaining: item.remaining,
      percent: item.quantity === 0 ? 0 : Math.round((item.delivered / item.quantity) * 100),
    })),
    deliveries: deliveries.map((d) => ({
      id: d.stayId,
      number: d.containerNumber,
      type: types.find((t) => t.id === d.containerTypeId)?.code ?? '',
      size: `${d.sizeFeet} ft`,
      gateOut: formatDateTime(d.gateOutAt),
      vehicle: vehicleOf(d),
    })),
  };
}
