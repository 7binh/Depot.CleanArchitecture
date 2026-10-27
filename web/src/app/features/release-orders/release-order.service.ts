import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../../core/api/api-config';

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

  get(id: string): Observable<ReleaseOrder> {
    return this.http.get<ReleaseOrder>(`${this.url}/${id}`);
  }

  create(order: NewReleaseOrder): Observable<ReleaseOrder> {
    return this.http.post<ReleaseOrder>(this.url, order);
  }
}
