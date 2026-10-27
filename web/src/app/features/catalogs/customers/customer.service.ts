import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../../../core/api/api-config';

export interface Customer {
  id: string;
  taxCode: string;
  name: string;
}

@Injectable({ providedIn: 'root' })
export class CustomerService {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_URL)}/api/customers`;

  list(): Observable<Customer[]> {
    return this.http.get<Customer[]>(this.url);
  }

  create(taxCode: string, name: string): Observable<Customer> {
    return this.http.post<Customer>(this.url, { taxCode, name });
  }
}
