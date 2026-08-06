import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  OwnedPropertyAnalysisResponse,
  OwnedPropertyRequest,
  OwnedPropertyResponse
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
  getAll(): Observable<OwnedPropertyResponse[]> {
    return this.http.get<OwnedPropertyResponse[]>(`${this.baseUrl}/GetAllOwnedProperty`);
  }

  // POST /api/OwnedProperty/EstimateEvaluationsOwnedproperty?id={id}&months={months}
  // id + months are simple types, so the API binds them from the query string.
  estimate(id: number, months: number): Observable<OwnedPropertyAnalysisResponse> {
    return this.http.post<OwnedPropertyAnalysisResponse>(
      `${this.baseUrl}/EstimateEvaluationsOwnedproperty?id=${id}&months=${months}`,
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
  delete(id: number): Observable<string> {
    return this.http.delete(`${this.baseUrl}/DeleteOwnedProperty/${id}`, { responseType: 'text' });
  }
}
