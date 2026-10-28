import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../../core/api/api-config';

/** Containers in the yard per line operator: `recent` is 0-9 days, `longStay` is 10 days or more. */
export interface YardStockRow {
  operatorCode: string;
  operatorName: string;
  recent: number;
  longStay: number;
  total: number;
}

export interface ThroughputRow {
  /** `yyyy-MM-dd` */
  date: string;
  operatorCode: string;
  operatorName: string;
  gateIns: number;
  gateOuts: number;
}

@Injectable({ providedIn: 'root' })
export class ReportService {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_URL)}/api/reports`;

  /** `date` is `yyyy-MM-dd`. */
  yardStock(date: string): Observable<YardStockRow[]> {
    return this.http.get<YardStockRow[]>(`${this.url}/yard-stock`, { params: { date } });
  }

  /** Both dates are `yyyy-MM-dd` and included. */
  throughput(from: string, to: string): Observable<ThroughputRow[]> {
    return this.http.get<ThroughputRow[]>(`${this.url}/throughput`, { params: { from, to } });
  }
}
