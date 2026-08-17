import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { MarketAreaLeaderboardQuery, MarketAreaLeaderboardResponse } from '../models/market-area-stats';
import { environment } from '../../../environments/environment';

// Talks to MarketAreaController's stats endpoints. api/[controller]/[action] routing.
@Injectable({ providedIn: 'root' })
export class MarketAreaStatsService {
  private readonly baseUrl = `${environment.apiBase}/api/MarketArea`;

  constructor(private readonly http: HttpClient) {}

  // GET /api/MarketArea/GetLeaderboard
  // A plain table read: the numbers were worked out by a previous recalculation.
  // Returns every place at this level, unsorted as far as the caller cares — the component
  // does the ranking.
  getLeaderboard(query: MarketAreaLeaderboardQuery): Observable<MarketAreaLeaderboardResponse> {
    return this.http.get<MarketAreaLeaderboardResponse>(`${this.baseUrl}/GetLeaderboard`, {
      params: {
        level: query.level,
        minListings: query.minListings
      }
    });
  }

  // POST /api/MarketArea/RecalculateMarketAreaStats
  // Rebuilds the whole stats table from current listings. Run it after an import.
  recalculate(): Observable<unknown> {
    return this.http.post<unknown>(`${this.baseUrl}/RecalculateMarketAreaStats`, {});
  }
}
