import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, forkJoin, of } from 'rxjs';
import { map, switchMap } from 'rxjs/operators';
import {
  FilterPropertyListingRequest,
  FilterPropertyListingResponse
} from '../models/filter-property-listing';
import { PropertyListingResponse } from '../models/property-listing';
import { environment } from '../../../environments/environment';

// The API returns at most 20 rows per call (its PageSize cap), so to sort + page in the
// browser we first grab page 1, see how many matches there are, then pull the rest.
const API_PAGE_SIZE = 20;

// The API also caps PageNumber at 50, so we can fetch at most 50 * 20 = 1000 rows.
const MAX_PAGES = 50;

// Talks to the sort/filter backend: POST /api/PropertyListing/FilterProperty.
@Injectable({ providedIn: 'root' })
export class ListingFilterService {
  // Fix: this pointed at /api/ListPropertyListing, a controller that no longer
  // exists (its one action moved into PropertyListingController). Every search
  // would have 404'd.
  private readonly baseUrl = `${environment.apiBase}/api/PropertyListing/FilterProperty`;

  constructor(private readonly http: HttpClient) {}

  // One page (used internally).
  private page(
    request: FilterPropertyListingRequest,
    pageNumber: number
  ): Observable<FilterPropertyListingResponse> {
    const body: FilterPropertyListingRequest = { ...request, pageNumber, pageSize: API_PAGE_SIZE };
    return this.http.post<FilterPropertyListingResponse>(this.baseUrl, body);
  }

  // All matching rows, gathered across however many pages of 20 it takes.
  // Result carries both how many we actually hold and how many the server says match, because
  // once the fetch is capped those are different numbers and the header must not print the cap
  // as if it were the total.
  filterAll(
    request: FilterPropertyListingRequest
  ): Observable<{ items: PropertyListingResponse[]; capped: boolean; totalRecords: number }> {
    return this.page(request, 1).pipe(
      switchMap(first => {
        const neededPages = Math.ceil(first.totalRecords / API_PAGE_SIZE);
        const pagesToFetch = Math.min(neededPages, MAX_PAGES);

        // Everything fit on page 1 — nothing more to fetch.
        if (pagesToFetch <= 1) {
          return of({ items: first.items, capped: neededPages > MAX_PAGES, totalRecords: first.totalRecords });
        }

        // Fetch pages 2..N in parallel and stitch them onto page 1.
        const rest: Observable<FilterPropertyListingResponse>[] = [];
        for (let p = 2; p <= pagesToFetch; p++) {
          rest.push(this.page(request, p));
        }

        return forkJoin(rest).pipe(
          map(responses => {
            const items = first.items.concat(...responses.map(r => r.items));
            return { items, capped: neededPages > MAX_PAGES, totalRecords: first.totalRecords };
          })
        );
      })
    );
  }
}
