import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { TopDealsQuery, TopDealsResponse } from '../models/top-deals';
import { environment } from '../../../environments/environment';

// Talks to PropertyListingController's GetTopDeals action.
@Injectable({ providedIn: 'root' })
export class TopDealsService {
  private readonly baseUrl = `${environment.apiBase}/api/PropertyListing/GetTopDeals`;

  constructor(private readonly http: HttpClient) {}

  getTopDeals(query: TopDealsQuery): Observable<TopDealsResponse> {
    let params = new HttpParams().set('count', query.count);

    if (query.district) {
      params = params.set('district', query.district);
    }

    if (query.municipality) {
      params = params.set('municipality', query.municipality);
    }

    if (query.condition) {
      params = params.set('condition', query.condition);
    }

    return this.http.get<TopDealsResponse>(this.baseUrl, { params });
  }
}
