import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import {
  OwnedPropertyAnalysisResponse,
  OwnedPropertyPortfolioResponse,
  OwnedPropertyRequest,
  OwnedPropertyResponse,
  OwnedPropertyValuationResponse
} from '../models/owned-property';
import { environment } from '../../../environments/environment';

// Talks to OwnedPropertyController. api/[controller]/[action] routing.
@Injectable({ providedIn: 'root' })
export class OwnedPropertyService {
  private readonly baseUrl = `${environment.apiBase}/api/OwnedProperty`;

  constructor(private readonly http: HttpClient) {}

  // GET /api/OwnedProperty/GetOwnedProperty/{id}
  get(id: number): Observable<OwnedPropertyResponse> {
    return this.http.get<OwnedPropertyResponse>(`${this.baseUrl}/GetOwnedProperty/${id}`);
  }

  // GET /api/OwnedProperty/GetAllOwnedProperty
  // The API wraps every list in a response object so it can carry errors, so unwrap "items"
  // here and keep handing the components the plain array they already expect.
  getAll(): Observable<OwnedPropertyResponse[]> {
    return this.http
      .get<{ items: OwnedPropertyResponse[] }>(`${this.baseUrl}/GetAllOwnedProperty`)
      .pipe(map(response => response.items));
  }

  // POST /api/OwnedProperty/EstimateEvaluationsOwnedproperty?id={id}&months={months}&radiusMeters={radiusMeters}
  // All three are simple types, so the API binds them from the query string.
  // radiusMeters is how far outside the property's own market area we still accept comps.
  estimate(id: number, months: number, radiusMeters: number): Observable<OwnedPropertyAnalysisResponse> {
    return this.http.post<OwnedPropertyAnalysisResponse>(
      `${this.baseUrl}/EstimateEvaluationsOwnedproperty?id=${id}&months=${months}&radiusMeters=${radiusMeters}`,
      null
    );
  }

  // GET /api/OwnedProperty/ListValuationsOwnedproperty
  // Every owned property priced as of the last recalculation. A plain read - no model fit, no
  // comps query - so it stays fast regardless of how many listings the database holds. A
  // property never recalculated still shows up, just with nothing priced yet. Not unwrapped to
  // a bare array like getAll: the totals on the response are the portfolio summary the screen
  // puts at the top.
  portfolio(): Observable<OwnedPropertyPortfolioResponse> {
    return this.http.get<OwnedPropertyPortfolioResponse>(`${this.baseUrl}/ListValuationsOwnedproperty`);
  }

  // POST /api/OwnedProperty/RecalculateAllValuations?months=&radiusMeters=&years=
  // Refits the pricing model once and reprices every owned property, overwriting what
  // portfolio() will read back next. Slow on purpose - this is the button, not the page load.
  recalculateAll(months: number, radiusMeters: number, years: number): Observable<OwnedPropertyPortfolioResponse> {
    return this.http.post<OwnedPropertyPortfolioResponse>(
      `${this.baseUrl}/RecalculateAllValuations?months=${months}&radiusMeters=${radiusMeters}&years=${years}`,
      null
    );
  }

  // POST /api/OwnedProperty/RecalculateValuation/{id}?months=&radiusMeters=&years=
  // Reprices one property and overwrites its stored valuation.
  recalculateOne(
    id: number,
    months: number,
    radiusMeters: number,
    years: number
  ): Observable<OwnedPropertyValuationResponse> {
    return this.http.post<OwnedPropertyValuationResponse>(
      `${this.baseUrl}/RecalculateValuation/${id}?months=${months}&radiusMeters=${radiusMeters}&years=${years}`,
      null
    );
  }

  // POST /api/OwnedProperty/AddOwnedProperty
  create(request: OwnedPropertyRequest): Observable<OwnedPropertyResponse> {
    return this.http.post<OwnedPropertyResponse>(`${this.baseUrl}/AddOwnedProperty`, request);
  }

  // PUT /api/OwnedProperty/UpdateOwnedProperty/{id}
  update(id: number, request: OwnedPropertyRequest): Observable<OwnedPropertyResponse> {
    return this.http.put<OwnedPropertyResponse>(`${this.baseUrl}/UpdateOwnedProperty/${id}`, request);
  }

  // DELETE /api/OwnedProperty/DeleteOwnedProperty/{id}
  // Answers with a JSON response now (it can carry an error), not the bare name as text.
  delete(id: number): Observable<string> {
    return this.http
      .delete<{ name?: string }>(`${this.baseUrl}/DeleteOwnedProperty/${id}`)
      .pipe(map(response => response.name ?? ''));
  }
}
