import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../../../core/api/api-config';

export interface LineOperator {
  id: string;
  code: string;
  name: string;
}

@Injectable({ providedIn: 'root' })
export class LineOperatorService {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_URL)}/api/line-operators`;

  list(): Observable<LineOperator[]> {
    return this.http.get<LineOperator[]>(this.url);
  }

  create(code: string, name: string): Observable<LineOperator> {
    return this.http.post<LineOperator>(this.url, { code, name });
  }
}
