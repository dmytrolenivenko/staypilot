import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { MarketOverviewQuery, MarketOverviewResponse } from '../models/market-overview';
import { environment } from '../../../environments/environment';

// Talks to MarketOverviewController. api/[controller]/[action] routing, so the action name
// is part of the URL.
@Injectable({ providedIn: 'root' })
export class MarketOverviewService {
  private readonly baseUrl = `${environment.apiBase}/api/MarketOverview`;

  constructor(private readonly http: HttpClient) {}

  // GET /api/MarketOverview/GetMarketOverview
  // One call answers the whole screen: the summaries, the distribution and the layout rows.
  // Empty filters are dropped rather than sent blank — the API treats a blank string as
  // "no filter" too, but leaving them out keeps the URL readable in the Network tab.
  getMarketOverview(query: MarketOverviewQuery): Observable<MarketOverviewResponse> {
    let params = new HttpParams();

    if (query.district) params = params.set('district', query.district);
    if (query.municipality) params = params.set('municipality', query.municipality);
    if (query.town) params = params.set('town', query.town);
    if (query.propertyType) params = params.set('propertyType', query.propertyType);
    if (query.typology) params = params.set('typology', query.typology);
    if (query.bucketCount) params = params.set('bucketCount', query.bucketCount);

    return this.http.get<MarketOverviewResponse>(`${this.baseUrl}/GetMarketOverview`, { params });
  }
}
