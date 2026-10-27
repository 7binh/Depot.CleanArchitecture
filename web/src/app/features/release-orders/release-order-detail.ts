import { ContainerType } from '../catalogs/container-types/container-type.service';
import { Customer } from '../catalogs/customers/customer.service';
import { LineOperator } from '../catalogs/line-operators/line-operator.service';
import { OrderStatus, formatDate, statusOf } from './release-order-status';
import { ReleaseOrder } from './release-order.service';

export interface OrderLineView {
  type: string;
  size: string;
  quantity: number;
  delivered: number;
  remaining: number;
  /** Whole percent of the line already delivered, for the progress bar. */
  percent: number;
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
}

export function toOrderDetail(
  order: ReleaseOrder,
  operators: LineOperator[],
  customers: Customer[],
  types: ContainerType[],
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
  };
}
