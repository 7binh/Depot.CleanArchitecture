import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../../core/api/api-config';
import { Paged } from '../../core/api/paged';

export interface ReleaseOrderItem {
  containerTypeId: string;
  sizeFeet: number;
  quantity: number;
  delivered: number;
  remaining: number;
}

export interface ReleaseOrder {
  id: string;
  lineOperatorId: string;
  customerId: string;
  orderNo: string;
  /** `yyyy-MM-dd` */
  issuedDate: string;
  /** `yyyy-MM-dd` */
  expiryDate: string;
  exportVessel: string;
  isExpired: boolean;
  status: 'NotDelivered' | 'Delivering' | 'Delivered';
  items: ReleaseOrderItem[];
}

/** A container that has left the depot on an order. */
export interface ReleaseOrderDelivery {
  stayId: string;
  containerId: string;
  containerNumber: string;
  containerTypeId: string;
  sizeFeet: number;
  /** ISO 8601 */
  gateOutAt: string;
  outVehicleType: 'Truck' | 'Barge' | 'Other' | null;
  outVehicleNumber: string | null;
}

export interface NewReleaseOrderLine {
  containerTypeId: string;
  sizeFeet: number;
  quantity: number;
}

export interface NewReleaseOrder {
  lineOperatorId: string;
  customerId: string;
  orderNo: string;
  /** `yyyy-MM-dd` */
  issuedDate: string;
  /** `yyyy-MM-dd` */
  expiryDate: string;
  exportVessel: string;
  lines: NewReleaseOrderLine[];
}

@Injectable({ providedIn: 'root' })
export class ReleaseOrderService {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_URL)}/api/release-orders`;

  list(): Observable<ReleaseOrder[]> {
    return this.http.get<ReleaseOrder[]>(this.url);
  }

  /** One page of the orders that can still deliver: not expired and with room on some line. */
  searchOpen(page: number, pageSize: number): Observable<Paged<ReleaseOrder>> {
    const params = new HttpParams()
      .set('openOnly', true)
      .set('page', page)
      .set('pageSize', pageSize);
    return this.http.get<Paged<ReleaseOrder>>(`${this.url}/search`, { params });
  }

  /** The containers that left on this order, latest gate out first. */
  deliveries(id: string): Observable<ReleaseOrderDelivery[]> {
    return this.http.get<ReleaseOrderDelivery[]>(`${this.url}/${id}/deliveries`);
  }

  get(id: string): Observable<ReleaseOrder> {
    return this.http.get<ReleaseOrder>(`${this.url}/${id}`);
  }

  create(order: NewReleaseOrder): Observable<ReleaseOrder> {
    return this.http.post<ReleaseOrder>(this.url, order);
  }
}
