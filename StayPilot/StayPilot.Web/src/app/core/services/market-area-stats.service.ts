import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  MarketAreaBudgetQuery,
  MarketAreaBudgetResponse,
  MarketAreaLeaderboardQuery,
  MarketAreaLeaderboardResponse,
  MarketAreaNeighbourGapQuery,
  MarketAreaNeighbourGapResponse
} from '../models/market-area-stats';
import { environment } from '../../../environments/environment';

// Talks to MarketAreaController's stats endpoints. api/[controller]/[action] routing.
@Injectable({ providedIn: 'root' })
export class MarketAreaStatsService {
  private readonly baseUrl = `${environment.apiBase}/api/MarketArea`;

  constructor(private readonly http: HttpClient) {}

  // GET /api/MarketArea/GetLeaderboard
  // Every place at this level, unsorted as far as the caller cares — the component ranks them.
  // Carries the deal counts and the renovation numbers too, so the leaderboard and the
  // renovation screen both read from here.
  getLeaderboard(query: MarketAreaLeaderboardQuery): Observable<MarketAreaLeaderboardResponse> {
    return this.http.get<MarketAreaLeaderboardResponse>(`${this.baseUrl}/GetLeaderboard`, {
      params: {
        level: query.level,
        minListings: query.minListings
      }
    });
  }

  // GET /api/MarketArea/GetBudgetRanking
  // What a budget buys in each place. Places where it reaches nothing are left out server-side.
  getBudgetRanking(query: MarketAreaBudgetQuery): Observable<MarketAreaBudgetResponse> {
    return this.http.get<MarketAreaBudgetResponse>(`${this.baseUrl}/GetBudgetRanking`, {
      params: {
        budget: query.budget,
        level: query.level,
        minListings: query.minListings
      }
    });
  }

  // GET /api/MarketArea/GetNeighbourGaps
  // Pairs of nearby places with a big price gap. Paired on the server because it is pairwise
  // work over the whole level, not a per-row read.
  getNeighbourGaps(query: MarketAreaNeighbourGapQuery): Observable<MarketAreaNeighbourGapResponse> {
    return this.http.get<MarketAreaNeighbourGapResponse>(`${this.baseUrl}/GetNeighbourGaps`, {
      params: {
        level: query.level,
        minListings: query.minListings,
        maxDistanceKm: query.maxDistanceKm,
        minGapPercent: query.minGapPercent
      }
    });
  }

  // POST /api/MarketArea/RecalculateMarketAreaStats
  // Rebuilds the whole stats table from current listings. Run it after an import.
  recalculate(): Observable<unknown> {
    return this.http.post<unknown>(`${this.baseUrl}/RecalculateMarketAreaStats`, {});
  }
}
