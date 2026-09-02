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
  // Every owned property, read straight from the cache the last "Re-price" wrote - no model fit,
  // no comp search, so this is what the screen loads on every visit. A property never valued yet
  // comes back with valuatedAtUtc null rather than being left out. Not unwrapped to a bare array
  // like getAll: the totals on the response are the portfolio summary the screen puts at the top.
  portfolio(): Observable<OwnedPropertyPortfolioResponse> {
    return this.http.get<OwnedPropertyPortfolioResponse>(`${this.baseUrl}/ListValuationsOwnedproperty`);
  }

  // POST /api/OwnedProperty/RevalueOwnedProperties?months=&radiusMeters=&years=
  // Prices every owned property again and overwrites the cache "portfolio" reads - the expensive
  // path, only run when the user presses "Re-price" or changes the pricing settings.
  recalculateAll(months: number, radiusMeters: number, years: number): Observable<OwnedPropertyPortfolioResponse> {
    return this.http.post<OwnedPropertyPortfolioResponse>(
      `${this.baseUrl}/RevalueOwnedProperties?months=${months}&radiusMeters=${radiusMeters}&years=${years}`,
      null
    );
  }

  // POST /api/OwnedProperty/RevalueOwnedProperty/{id}?months=&radiusMeters=&years=
  // Reprices one property and overwrites its stored valuation, the same cache
  // recalculateAll() overwrites for the whole portfolio.
  recalculateOne(
    id: number,
    months: number,
    radiusMeters: number,
    years: number
  ): Observable<OwnedPropertyValuationResponse> {
    return this.http.post<OwnedPropertyValuationResponse>(
      `${this.baseUrl}/RevalueOwnedProperty/${id}?months=${months}&radiusMeters=${radiusMeters}&years=${years}`,
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
