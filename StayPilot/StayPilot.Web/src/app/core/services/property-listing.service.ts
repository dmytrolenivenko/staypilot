import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PropertyListingRequest, PropertyListingResponse } from '../models/property-listing';

@Injectable({ providedIn: 'root' })
export class PropertyListingService {
  private readonly baseUrl = '/api/PropertyListing';

  constructor(private readonly http: HttpClient) {}

  // The API routes as api/[controller]/[action], so the action name is part of the URL.
  getById(id: number): Observable<PropertyListingResponse> {
    return this.http.get<PropertyListingResponse>(`${this.baseUrl}/GetById/${id}`);
  }

  create(request: PropertyListingRequest): Observable<PropertyListingResponse> {
    return this.http.post<PropertyListingResponse>(`${this.baseUrl}/AddPropertyListing`, request);
  }
}
