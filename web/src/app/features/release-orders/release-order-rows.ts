import { Customer } from '../catalogs/customers/customer.service';
import { LineOperator } from '../catalogs/line-operators/line-operator.service';
import { OrderStatus, formatDate, statusOf } from './release-order-status';
import { ReleaseOrder } from './release-order.service';

/** An order with the operator and customer names worked out, ready to show in a table. */
export interface ReleaseOrderRow {
  id: string;
  orderNo: string;
  lineOperator: string;
  customer: string;
  exportVessel: string;
  issuedDate: string;
  expiryDate: string;
  /** Containers delivered out of containers ordered, over all lines. */
  progress: string;
  status: OrderStatus;
}

function sum(order: ReleaseOrder, field: 'delivered' | 'quantity'): number {
  return order.items.reduce((total, item) => total + item[field], 0);
}

export function toOrderRows(
  orders: ReleaseOrder[],
  operators: LineOperator[],
  customers: Customer[],
): ReleaseOrderRow[] {
  const operatorCodes = new Map(operators.map((o) => [o.id, o.code]));
  const customerNames = new Map(customers.map((c) => [c.id, c.name]));
  return orders.map((order) => ({
    id: order.id,
    orderNo: order.orderNo,
    lineOperator: operatorCodes.get(order.lineOperatorId) ?? '',
    customer: customerNames.get(order.customerId) ?? '',
    exportVessel: order.exportVessel,
    issuedDate: formatDate(order.issuedDate),
    expiryDate: formatDate(order.expiryDate),
    progress: `${sum(order, 'delivered')}/${sum(order, 'quantity')}`,
    status: statusOf(order),
  }));
}
