import { HttpClient, HttpParams} from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { MarketArea } from '../models/market-area';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class MarketAreaService {
  private readonly baseUrl = `${environment.apiBase}/api/MarketArea`;

  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<MarketArea[]> {
    // The API routes as api/[controller]/[action], so the action name is part of the URL.
    return this.http.get<MarketArea[]>(`${this.baseUrl}/GetAll`);
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
