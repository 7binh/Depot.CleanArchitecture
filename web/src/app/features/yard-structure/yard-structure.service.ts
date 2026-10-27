import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../../core/api/api-config';
import { Block, YardRow } from '../yard/yard.service';

export interface LayoutRequest {
  fromBay: number;
  toBay: number;
  rowCount: number;
  maxTier: number;
}

export interface LayoutResult {
  baysCreated: number;
  rowsCreated: number;
}

/** The changes to the shape of the yard: blocks, the bays and rows under them. Reading stays in `YardService`. */
@Injectable({ providedIn: 'root' })
export class YardStructureService {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_URL)}/api/blocks`;

  createBlock(code: string, name: string, isVirtual: boolean): Observable<Block> {
    return this.http.post<Block>(this.url, { code, name, isVirtual });
  }

  renameBlock(blockId: string, name: string): Observable<Block> {
    return this.http.put<Block>(`${this.url}/${blockId}`, { name });
  }

  deleteBlock(blockId: string): Observable<unknown> {
    return this.http.delete(`${this.url}/${blockId}`);
  }

  generateLayout(blockId: string, request: LayoutRequest): Observable<LayoutResult> {
    return this.http.post<LayoutResult>(`${this.url}/${blockId}/layout`, request);
  }

  deleteBay(blockId: string, bayId: string): Observable<unknown> {
    return this.http.delete(`${this.url}/${blockId}/bays/${bayId}`);
  }

  changeMaxTier(blockId: string, row: YardRow, maxTier: number): Observable<YardRow> {
    return this.http.put<YardRow>(`${this.url}/${blockId}/bays/${row.bayId}/rows/${row.id}`, {
      maxTier,
    });
  }

  deleteRow(blockId: string, row: YardRow): Observable<unknown> {
    return this.http.delete(`${this.url}/${blockId}/bays/${row.bayId}/rows/${row.id}`);
  }
}
