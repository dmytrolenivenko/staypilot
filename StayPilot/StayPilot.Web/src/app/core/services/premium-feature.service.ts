import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PremiumFeatureResponse } from '../models/premium-feature';

// Talks to PremiumFeatureController. api/[controller]/[action] routing.
@Injectable({ providedIn: 'root' })
export class PremiumFeatureService {
  private readonly baseUrl = '/api/PremiumFeature';

  constructor(private readonly http: HttpClient) {}

  // GET /api/PremiumFeature/GetAllPremiumFeatures
  getAll(): Observable<PremiumFeatureResponse[]> {
    return this.http.get<PremiumFeatureResponse[]>(`${this.baseUrl}/GetAllPremiumFeatures`);
  }

  // POST /api/PremiumFeature/ReCalculatePremiumFeaturesValue
  // Recomputes every tracked feature's premium from current listings and saves them.
  // Returns the recalculated rows (domain shape); we just re-read GetAll after, so the
  // exact return shape doesn't matter to the caller.
  recalculate(): Observable<unknown> {
    return this.http.post<unknown>(`${this.baseUrl}/ReCalculatePremiumFeaturesValue`, {});
  }
}
