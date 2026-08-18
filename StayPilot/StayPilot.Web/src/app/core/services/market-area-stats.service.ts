import { HttpClient, HttpParams } from '@angular/common/http';
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

/**
 * Adds a filter only when it has a value.
 *
 * The API treats a missing parameter as "not filtered on" and an empty string as a filter for
 * the empty string, which matches nothing. Sending `district=` therefore returns an empty board
 * rather than the whole country — so the empty ones are left off the query entirely.
 */
function withOptional(params: HttpParams, values: Record<string, string | number | undefined>): HttpParams {
  let next = params;

  for (const [key, value] of Object.entries(values)) {
    if (value !== undefined && value !== '') {
      next = next.set(key, value);
    }
  }

  return next;
}

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
    const params = withOptional(
      new HttpParams().set('level', query.level).set('minListings', query.minListings),
      { district: query.district, municipality: query.municipality }
    );

    return this.http.get<MarketAreaLeaderboardResponse>(`${this.baseUrl}/GetLeaderboard`, { params });
  }

  // GET /api/MarketArea/GetBudgetRanking
  // What a budget buys in each place. Places where it reaches nothing are left out server-side.
  getBudgetRanking(query: MarketAreaBudgetQuery): Observable<MarketAreaBudgetResponse> {
    const params = withOptional(
      new HttpParams()
        .set('budget', query.budget)
        .set('level', query.level)
        .set('minListings', query.minListings),
      {
        district: query.district,
        municipality: query.municipality,
        minTypology: query.minTypology,
        stretchPercent: query.stretchPercent
      }
    );

    return this.http.get<MarketAreaBudgetResponse>(`${this.baseUrl}/GetBudgetRanking`, { params });
  }

  // GET /api/MarketArea/GetNeighbourGaps
  // Pairs of nearby places with a big price gap. Paired on the server because it is pairwise
  // work over the whole level, not a per-row read.
  getNeighbourGaps(query: MarketAreaNeighbourGapQuery): Observable<MarketAreaNeighbourGapResponse> {
    const params = withOptional(
      new HttpParams()
        .set('level', query.level)
        .set('minListings', query.minListings)
        .set('maxDistanceKm', query.maxDistanceKm)
        .set('minGapPercent', query.minGapPercent),
      {
        district: query.district,
        municipality: query.municipality,
        typology: query.typology,
        minTypologyListings: query.minTypologyListings
      }
    );

    return this.http.get<MarketAreaNeighbourGapResponse>(`${this.baseUrl}/GetNeighbourGaps`, { params });
  }

  // POST /api/MarketArea/RecalculateMarketAreaStats
  // Rebuilds the whole stats table from current listings. Run it after an import.
  recalculate(): Observable<unknown> {
    return this.http.post<unknown>(`${this.baseUrl}/RecalculateMarketAreaStats`, {});
  }
}
