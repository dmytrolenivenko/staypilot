import { HttpClient, HttpParams} from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, forkJoin, of } from 'rxjs';
import { map, switchMap } from 'rxjs/operators';
import { MarketArea, MarketAreaPage, MarketAreaQuery } from '../models/market-area';
import { environment } from '../../../environments/environment';

// GetAll is paged on the server now. This is the biggest page it accepts,
// so walking the whole table (getAll below) takes as few calls as possible.
const MAX_PAGE_SIZE = 200;

@Injectable({ providedIn: 'root' })
export class MarketAreaService {
  private readonly baseUrl = `${environment.apiBase}/api/MarketArea`;

  constructor(private readonly http: HttpClient) {}

  // One page. Optional search text matches district, municipality, town or zone.
  getPage(query: MarketAreaQuery): Observable<MarketAreaPage> {
    // The API routes as api/[controller]/[action], so the action name is part of the URL.
    let params = new HttpParams()
      .set('pageNumber', query.pageNumber)
      .set('pageSize', query.pageSize);
    if (query.search) params = params.set('search', query.search);

    return this.http.get<MarketAreaPage>(`${this.baseUrl}/GetAll`, { params });
  }

  // Every market area, gathered across however many pages it takes.
  // The dropdowns (create listing, owned properties) need the full list, so they use this.
  getAll(): Observable<MarketArea[]> {
    return this.getPage({ pageNumber: 1, pageSize: MAX_PAGE_SIZE }).pipe(
      switchMap(first => {
        const totalPages = Math.ceil(first.totalRecords / MAX_PAGE_SIZE);

        // Everything fit on page 1 — nothing more to fetch.
        if (totalPages <= 1) {
          return of(first.items);
        }

        // Fetch pages 2..N in parallel and stitch them onto page 1.
        const rest: Observable<MarketAreaPage>[] = [];
        for (let p = 2; p <= totalPages; p++) {
          rest.push(this.getPage({ pageNumber: p, pageSize: MAX_PAGE_SIZE }));
        }

        return forkJoin(rest).pipe(
          map(pages => [...first.items, ...pages.flatMap(page => page.items)])
        );
      })
    );
  }

  // Calls GET /api/MarketArea/GetOptions/options — returns the dropdown choices for the next level.
  // Pass what's already picked; the backend returns the level below it.
  getOptions(district?: string, municipality?: string, town?: string): Observable<string[]> {
    let params = new HttpParams();
    if (district) params = params.set('district', district);
    if (municipality) params = params.set('municipality', municipality);
    if (town) params = params.set('town', town);
    return this.http.get<string[]>(`${this.baseUrl}/GetOptions/options`, { params });
  }
}
