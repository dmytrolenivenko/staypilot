import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { OwnedPropertyRequest, OwnedPropertyResponse } from '../models/owned-property';

// Talks to OwnedPropertyController. api/[controller]/[action] routing.
@Injectable({ providedIn: 'root' })
export class OwnedPropertyService {
  private readonly baseUrl = '/api/OwnedProperty';

  constructor(private readonly http: HttpClient) {}

  // GET /api/OwnedProperty/GetOwnedProperty/{id}
  get(id: number): Observable<OwnedPropertyResponse> {
    return this.http.get<OwnedPropertyResponse>(`${this.baseUrl}/GetOwnedProperty/${id}`);
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
