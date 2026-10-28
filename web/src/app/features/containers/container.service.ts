import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../../core/api/api-config';
import { Paged } from '../../core/api/paged';

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

/** A container with the codes of its operator and type, as a list shows it. */
export interface ContainerListItem extends Container {
  lineOperatorCode: string;
  containerTypeCode: string;
}

/** Empty text means no filter on that field. */
export interface ContainerFilter {
  number: string;
  lineOperatorId: string;
  containerTypeId: string;
  sizeFeet: string;
  isoCode: string;
}

/** A search by any of the fields; `inYard` keeps only containers in the yard (true) or outside it (false). */
export interface ContainerSearch extends Partial<ContainerFilter> {
  inYard?: boolean;
}

export type NewContainer = Omit<Container, 'id' | 'lineOperatorId'>;

@Injectable({ providedIn: 'root' })
export class ContainerService {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_URL)}/api/containers`;

  list(): Observable<Container[]> {
    return this.http.get<Container[]>(this.url);
  }

  /** One page of containers matching the filter, ordered by number. */
  search(
    filter: ContainerSearch,
    page: number,
    pageSize: number,
  ): Observable<Paged<ContainerListItem>> {
    const { inYard, ...texts } = filter;
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    for (const [key, value] of Object.entries(texts)) {
      if (value?.trim()) {
        params = params.set(key, value.trim());
      }
    }
    if (inYard !== undefined) {
      params = params.set('inYard', inYard);
    }
    return this.http.get<Paged<ContainerListItem>>(`${this.url}/search`, { params });
  }

  create(container: NewContainer): Observable<Container> {
    return this.http.post<Container>(this.url, container);
  }
}
