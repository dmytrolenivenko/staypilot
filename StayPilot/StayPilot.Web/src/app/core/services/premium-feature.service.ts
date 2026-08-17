import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { PremiumFeatureResponse } from '../models/premium-feature';
import { environment } from '../../../environments/environment';

// Talks to PremiumFeatureController. api/[controller]/[action] routing.
@Injectable({ providedIn: 'root' })
export class PremiumFeatureService {
  private readonly baseUrl = `${environment.apiBase}/api/PremiumFeature`;

  constructor(private readonly http: HttpClient) {}

  // GET /api/PremiumFeature/GetAllPremiumFeatures
  // The API wraps the list in a response object so it can carry errors; unwrap "items" here.
  getAll(): Observable<PremiumFeatureResponse[]> {
    return this.http
      .get<{ items: PremiumFeatureResponse[] }>(`${this.baseUrl}/GetAllPremiumFeatures`)
      .pipe(map(response => response.items));
  }

  // POST /api/PremiumFeature/ReCalculatePremiumFeaturesValue
  // Recomputes every tracked feature's premium from current listings and saves them.
  // Returns the recalculated rows (domain shape); we just re-read GetAll after, so the
  // exact return shape doesn't matter to the caller.
  recalculate(): Observable<unknown> {
    return this.http.post<unknown>(`${this.baseUrl}/ReCalculatePremiumFeaturesValue`, {});
  }
}
