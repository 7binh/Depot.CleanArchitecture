import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../../core/api/api-config';

export interface Block {
  id: string;
  code: string;
  name: string;
  isVirtual: boolean;
}

export interface Bay {
  id: string;
  blockId: string;
  bayNo: number;
}

export interface YardRow {
  id: string;
  bayId: string;
  rowNo: number;
  maxTier: number;
}

/** Odd bays hold 20ft containers, even bays hold 40ft ones. */
export function baySize(bayNo: number): 20 | 40 {
  return bayNo % 2 === 1 ? 20 : 40;
}

@Injectable({ providedIn: 'root' })
export class YardService {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_URL)}/api/blocks`;

  blocks(): Observable<Block[]> {
    return this.http.get<Block[]>(this.url);
  }

  bays(blockId: string): Observable<Bay[]> {
    return this.http.get<Bay[]>(`${this.url}/${blockId}/bays`);
  }

  rows(blockId: string, bayId: string): Observable<YardRow[]> {
    return this.http.get<YardRow[]>(`${this.url}/${blockId}/bays/${bayId}/rows`);
  }
}
