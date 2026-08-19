import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BuildCostBasis } from '../models/build-cost';
import { environment } from '../../../environments/environment';

/**
 * Talks to BuildCostController. api/[controller]/[action] routing.
 *
 * A proxy as much as a service: INE sends no CORS headers, so the browser cannot read the
 * construction index directly however much it would like to. The API fetches it, escalates the
 * anchors, and hands back finished rates.
 */
@Injectable({ providedIn: 'root' })
export class BuildCostService {
  private readonly baseUrl = `${environment.apiBase}/api/BuildCost`;

  constructor(private readonly http: HttpClient) {}

  /** GET /api/BuildCost/GetBasis — every rate, priced for today. */
  getBasis(): Observable<BuildCostBasis> {
    return this.http.get<BuildCostBasis>(`${this.baseUrl}/GetBasis`);
  }
}
