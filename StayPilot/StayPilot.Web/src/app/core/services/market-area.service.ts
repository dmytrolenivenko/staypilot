import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { MarketArea } from '../models/market-area';

@Injectable({ providedIn: 'root' })
export class MarketAreaService {
  private readonly baseUrl = '/api/MarketArea';

  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<MarketArea[]> {
    return this.http.get<MarketArea[]>(this.baseUrl);
  }
}
