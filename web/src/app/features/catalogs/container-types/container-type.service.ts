import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../../../core/api/api-config';

export interface ContainerType {
  id: string;
  code: string;
  name: string;
}

@Injectable({ providedIn: 'root' })
export class ContainerTypeService {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_URL)}/api/container-types`;

  list(): Observable<ContainerType[]> {
    return this.http.get<ContainerType[]>(this.url);
  }
}
