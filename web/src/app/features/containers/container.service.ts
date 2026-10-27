import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../../core/api/api-config';

export interface Container {
  id: string;
  number: string;
  lineOperatorId: string;
  containerTypeId: string;
  isoCode: string;
  sizeFeet: number;
  maxWeightKg: number;
  tareWeightKg: number;
  /** `yyyy-MM-dd` */
  manufactureDate: string;
}

export type NewContainer = Omit<Container, 'id' | 'lineOperatorId'>;

@Injectable({ providedIn: 'root' })
export class ContainerService {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_URL)}/api/containers`;

  list(): Observable<Container[]> {
    return this.http.get<Container[]>(this.url);
  }

  create(container: NewContainer): Observable<Container> {
    return this.http.post<Container>(this.url, container);
  }
}
