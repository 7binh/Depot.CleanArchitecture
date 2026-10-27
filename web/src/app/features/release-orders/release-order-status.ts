import { ChipTone } from '../../shared/ui/chip';
import { ReleaseOrder } from './release-order.service';

/** What the screens show for an order: its delivery status, or "expired" while it still had room. */
export type OrderStatus = 'NotDelivered' | 'Delivering' | 'Delivered' | 'Expired';

export const ORDER_STATUSES: Record<OrderStatus, { label: string; tone: ChipTone }> = {
  NotDelivered: { label: 'Chưa giao', tone: 'neutral' },
  Delivering: { label: 'Đang giao', tone: 'warning' },
  Delivered: { label: 'Đã giao đủ', tone: 'success' },
  Expired: { label: 'Quá hạn', tone: 'danger' },
};

/** A fully delivered order stays "delivered" even after its expiry date. */
export function statusOf(order: ReleaseOrder): OrderStatus {
  if (order.status === 'Delivered') {
    return 'Delivered';
  }
  return order.isExpired ? 'Expired' : order.status;
}

/** `2026-10-01` becomes `01/10/2026`. */
export function formatDate(isoDate: string): string {
  const [year, month, day] = isoDate.split('-');
  return `${day}/${month}/${year}`;
}
