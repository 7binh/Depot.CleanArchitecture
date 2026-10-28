import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../../core/api/api-config';
import { Paged } from '../../core/api/paged';
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

/** A stay with the number and operator of its container, so a list needs no second request. */
export interface StayListItem extends StayRecord {
  containerNumber: string;
  lineOperatorCode: string;
}

/** `true` keeps the containers in the yard, `false` those that left, `null` all of them. */
export type InYardFilter = boolean | null;

@Injectable({ providedIn: 'root' })
export class StayService {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_URL)}/api/container-stays`;

  /** One page of stays, newest gate in first; blank text and a null scope are left out of the request. */
  search(
    number: string,
    inYard: InYardFilter,
    page: number,
    pageSize: number,
  ): Observable<Paged<StayListItem>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (number.trim()) {
      params = params.set('number', number.trim());
    }
    if (inYard !== null) {
      params = params.set('inYard', inYard);
    }
    return this.http.get<Paged<StayListItem>>(`${this.url}/search`, { params });
  }
}
