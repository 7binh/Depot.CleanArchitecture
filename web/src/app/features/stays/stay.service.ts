import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../../core/api/api-config';
import { StayCondition, StayGrade, VehicleType } from '../gate/gate.service';

/** One stay of a container in the yard, from gate in until gate out. */
export interface StayRecord {
  id: string;
  containerId: string;
  /** ISO 8601 */
  gateInAt: string;
  grade: StayGrade;
  condition: StayCondition;
  damageNote: string | null;
  inVehicleType: VehicleType;
  inVehicleNumber: string;
  blockId: string;
  rowId: string | null;
  tier: number | null;
  /** Null while the container is still in the yard. */
  gateOutAt: string | null;
  outVehicleType: VehicleType | null;
  outVehicleNumber: string | null;
  releaseOrderItemId: string | null;
}

/** `true` keeps the containers in the yard, `false` those that left, `null` all of them. */
export type InYardFilter = boolean | null;

@Injectable({ providedIn: 'root' })
export class StayService {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_URL)}/api/container-stays`;

  list(inYard: InYardFilter): Observable<StayRecord[]> {
    const params = inYard === null ? new HttpParams() : new HttpParams().set('inYard', inYard);
    return this.http.get<StayRecord[]>(this.url, { params });
  }
}
