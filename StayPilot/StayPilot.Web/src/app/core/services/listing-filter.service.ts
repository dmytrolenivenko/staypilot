import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  FilterPropertyListingRequest,
  FilterPropertyListingResponse
} from '../models/filter-property-listing';
import { environment } from '../../../environments/environment';

// Page sizes the browser offers. All inside the API's [Range(1, 100)] on PageSize.
export const PAGE_SIZE_CHOICES = [20, 50, 100];

// Talks to the sort/filter backend: POST /api/PropertyListing/FilterProperty.
//
// One call per page, sorted and paged by the server.
//
// It used to fetch every matching row instead — page 1, then pages 2..N in parallel — and sort
// the pile in the browser. That was wrong twice over. The API caps a page at 100 rows and the
// page number at 10,000, and it used to cap them at 20 and 50, so the fetch stopped dead at
// 1,000 rows: a search of Faro (9,414 listings) quietly held the first 1,000 by Id and sorted
// only those. "Most expensive in Faro" answered with the most expensive of an arbitrary eighth
// of it, and nothing on screen said so. It also cost up to 50 requests per search.
@Injectable({ providedIn: 'root' })
export class ListingFilterService {
  private readonly baseUrl = `${environment.apiBase}/api/PropertyListing/FilterProperty`;

  constructor(private readonly http: HttpClient) {}

  // One page, exactly as asked for. Sorting and paging both happen on the server, so what
  // comes back is the real page N of the whole matching set, not of a truncated copy of it.
  filterPage(request: FilterPropertyListingRequest): Observable<FilterPropertyListingResponse> {
    return this.http.post<FilterPropertyListingResponse>(this.baseUrl, request);
  }
}
