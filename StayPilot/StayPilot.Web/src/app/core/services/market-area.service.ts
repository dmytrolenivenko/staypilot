import { HttpClient, HttpParams} from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, forkJoin, of } from 'rxjs';
import { map, shareReplay, switchMap } from 'rxjs/operators';
import { MarketArea, MarketAreaPage, MarketAreaQuery, MarketAreaTreeDistrict } from '../models/market-area';
import { environment } from '../../../environments/environment';

// GetAll is paged on the server now. This is the biggest page it accepts,
// so walking the whole table (getAll below) takes as few calls as possible.
const MAX_PAGE_SIZE = 200;

@Injectable({ providedIn: 'root' })
export class MarketAreaService {
  private readonly baseUrl = `${environment.apiBase}/api/MarketArea`;

  // The whole place tree, fetched once per session and shared by every picker on every screen.
  // shareReplay resets on error, so a failed load is retried by the next dropdown, not cached.
  private readonly tree$: Observable<MarketAreaTreeDistrict[]>;

  constructor(private readonly http: HttpClient) {
    // Built here, not as a field initializer: those run before `http` is assigned.
    this.tree$ = this.http
      .get<{ districts: MarketAreaTreeDistrict[] }>(`${this.baseUrl}/GetTree`)
      .pipe(
        map(response => response.districts),
        shareReplay({ bufferSize: 1, refCount: false })
      );
  }

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

  // Starts the tree download early (the app shell calls this), so the first dropdown a
  // screen opens is already filled rather than waiting on the API.
  preloadTree(): void {
    this.tree$.subscribe({ error: () => {} });
  }

  // The dropdown choices for the next level. Pass what's already picked; you get the level
  // below it. Answered from the tree in the browser - it used to be one API call per level,
  // seconds each, just to open the next dropdown.
  getOptions(district?: string, municipality?: string, town?: string): Observable<string[]> {
    return this.tree$.pipe(map(tree => optionsFrom(tree, district, municipality, town)));
  }
}

// Case-insensitive, the way the API matched them before this moved into the browser.
function sameName(a: string, b: string): boolean {
  return a.localeCompare(b, 'pt', { sensitivity: 'accent' }) === 0;
}

function optionsFrom(tree: MarketAreaTreeDistrict[], district?: string, municipality?: string, town?: string): string[] {
  if (!district) {
    return tree.map(x => x.name);
  }

  const d = tree.find(x => sameName(x.name, district));
  if (!municipality) {
    return d?.municipalities.map(x => x.name) ?? [];
  }

  const m = d?.municipalities.find(x => sameName(x.name, municipality));
  if (!town) {
    return m?.towns.map(x => x.name) ?? [];
  }

  return m?.towns.find(x => sameName(x.name, town))?.zones ?? [];
}
