// What one slice of the market is asking, read live from the listings.
// Mirrors MarketOverviewRequest / MarketOverviewResponse on the API.

import { PropertyType, Typology } from './enums';
import { AreaLevel } from './market-area-stats';

// What we send to GET /api/MarketOverview/GetMarketOverview (as query string).
// Everything is optional: the ones you leave out are not filtered on.
export interface MarketOverviewQuery {
  district?: string;
  municipality?: string;
  town?: string;
  propertyType?: PropertyType;
  typology?: Typology;
  // How many bars the price distribution is cut into. The API accepts 4 to 20.
  bucketCount?: number;
}

// One measured quantity, four ways. The gap between median and average is the reading:
// they agree on an even market, and the average runs away where a few big listings pull it.
export interface MarketOverviewStats {
  median: number;
  average: number;
  min: number;
  max: number;
}

// One bar of the price distribution.
export interface MarketOverviewPriceBucket {
  fromPrice: number;
  toPrice: number;
  listingCount: number;
  sharePercent: number;
}

// What one room layout costs in this slice.
export interface MarketOverviewTypology {
  typology: Typology;
  listingCount: number;
  medianPrice: number;
  medianAreaM2: number;
  medianPricePerM2: number;
}

// One place inside the slice, measured the same way the slice itself was.
export interface MarketOverviewBreakdownItem {
  district: string;
  municipality: string;
  town: string;
  displayName: string;
  listingCount: number;
  // This place's share of the whole slice.
  sharePercent: number;
  medianPrice: number;
  medianAreaM2: number;
  medianPricePerM2: number;
  // How far this place sits from the slice's own €/m² median. Positive is dearer.
  vsSlicePercent: number;
}

// The slice cut one grain finer, dearest first. The API picks the level from what was narrowed.
export interface MarketOverviewBreakdown {
  level: AreaLevel;
  items: MarketOverviewBreakdownItem[];
}

export interface MarketOverviewResponse {
  // The slice named for a human: "Guia (Albufeira)", or "All areas".
  placeName: string;
  // Read this first — everything else is worked out from this many listings.
  listingCount: number;
  price: MarketOverviewStats;
  pricePerM2: MarketOverviewStats;
  areaM2: MarketOverviewStats;
  // Cheapest bar first. Empty when nothing matched.
  distribution: MarketOverviewPriceBucket[];
  typologies: MarketOverviewTypology[];
  // The places inside the slice. Null once the slice is a single freguesia — nothing finer
  // is held — and the broader the slice, the more this is the real answer.
  breakdown: MarketOverviewBreakdown | null;
  // Worked out when you asked — nothing here comes from the stats table, so it is never stale.
  generatedAtUtc: string;
}
