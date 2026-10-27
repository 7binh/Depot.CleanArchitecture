import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../../core/api/api-config';

export type StayGrade = 'A' | 'B' | 'C';
export type StayCondition = 'Normal' | 'Damaged';
export type VehicleType = 'Truck' | 'Barge' | 'Other';

export interface NewGateIn {
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
}

export interface ContainerStay {
  id: string;
  containerId: string;
  gateInAt: string;
  gateOutAt: string | null;
}

@Injectable({ providedIn: 'root' })
export class GateService {
  private readonly http = inject(HttpClient);
  private readonly url = inject(API_URL);

  gateIn(request: NewGateIn): Observable<ContainerStay> {
    return this.http.post<ContainerStay>(`${this.url}/api/gate-in`, request);
  }
}
