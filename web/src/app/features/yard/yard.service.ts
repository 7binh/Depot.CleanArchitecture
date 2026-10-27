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

/** One container standing in a block, with the few fields the map shows. */
export interface MapContainer {
  stayId: string;
  number: string;
  sizeFeet: number;
  operatorCode: string;
  daysInYard: number;
  isLongStay: boolean;
  isDamaged: boolean;
}

/** `fromBay` and `toBay` are the bays the container covers on the floor (a 40ft one also covers the odd bays beside it). */
export interface TierMap {
  tier: number;
  fromBay: number;
  toBay: number;
  container: MapContainer;
}

export interface RowMap {
  rowNo: number;
  maxTier: number;
  /** Only the tiers that hold a container. */
  tiers: TierMap[];
}

export interface BayMap {
  bayNo: number;
  sizeFeet: number;
  rows: RowMap[];
}

export interface BlockMap {
  blockId: string;
  code: string;
  name: string;
  isVirtual: boolean;
  bays: BayMap[];
  /** Containers in the block without a position (a virtual block). */
  unplaced: MapContainer[];
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

  map(blockId: string): Observable<BlockMap> {
    return this.http.get<BlockMap>(`${this.url}/${blockId}/map`);
  }

  bays(blockId: string): Observable<Bay[]> {
    return this.http.get<Bay[]>(`${this.url}/${blockId}/bays`);
  }

  rows(blockId: string, bayId: string): Observable<YardRow[]> {
    return this.http.get<YardRow[]>(`${this.url}/${blockId}/bays/${bayId}/rows`);
  }
}
