import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ListingSnapshotRequest, ListingSnapshotResponse } from '../models/property-listing';
import { environment } from '../../../environments/environment';

// Talks to ListingSnapshotController. The API routes as api/[controller]/[action],
// so the action name is part of the URL (and ASP.NET drops the "Async" suffix).
@Injectable({ providedIn: 'root' })
export class ListingSnapshotService {
  private readonly baseUrl = `${environment.apiBase}/api/ListingSnapshot`;

  constructor(private readonly http: HttpClient) {}

  // POST /api/ListingSnapshot/CreateListingSnapshot
  create(request: ListingSnapshotRequest): Observable<ListingSnapshotResponse> {
    return this.http.post<ListingSnapshotResponse>(`${this.baseUrl}/CreateListingSnapshot`, request);
  }

  // GET /api/ListingSnapshot/GetListingSnapshotByPropertyId/{propertyListingId}
  getByPropertyId(propertyListingId: number): Observable<ListingSnapshotResponse> {
    return this.http.get<ListingSnapshotResponse>(
      `${this.baseUrl}/GetListingSnapshotByPropertyId/${propertyListingId}`
    );
  }
}
