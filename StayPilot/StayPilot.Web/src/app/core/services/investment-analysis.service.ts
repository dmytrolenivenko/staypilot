import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { InvestmentAnalysisResponse } from '../models/investment-analysis';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class InvestmentAnalysisService {
  private readonly baseUrl = `${environment.apiBase}/api/InvestmentAnalysis`;

  constructor(private readonly http: HttpClient) {}

  // The API routes as api/[controller]/[action], so the action name is part of the URL.
  analyze(propertyListingId: number, renovationCostOverride?: number): Observable<InvestmentAnalysisResponse> {
    return this.http.get<InvestmentAnalysisResponse>(
      `${this.baseUrl}/Analyze/${propertyListingId}`,
      { params: this.paramsFor(renovationCostOverride) }
    );
  }

  analyzeOwnedProperty(ownedPropertyId: number, renovationCostOverride?: number): Observable<InvestmentAnalysisResponse> {
    return this.http.get<InvestmentAnalysisResponse>(
      `${this.baseUrl}/AnalyzeOwnedProperty/${ownedPropertyId}`,
      { params: this.paramsFor(renovationCostOverride) }
    );
  }

  private paramsFor(renovationCostOverride?: number): HttpParams {
    let params = new HttpParams();
    if (renovationCostOverride !== undefined) {
      params = params.set('renovationCostOverride', renovationCostOverride);
    }
    return params;
  }
}
