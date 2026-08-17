// Price numbers per place, worked out on the server and read back.
// Mirrors MarketAreaStatsResponse / MarketAreaLeaderboardResponse on the API.

// Which grain a row measures. Sent as its name, matching the AreaLevel enum on the API.
export type AreaLevel = 'District' | 'Municipality' | 'Town';

export interface MarketAreaStatsResponse {
  level: AreaLevel;
  district: string;
  municipality: string;
  town: string;
  // The place written out for a human: "Albufeira (Faro)".
  displayName: string;
  // How many listings the median came from. Always show it next to the price.
  listingCount: number;
  medianPricePerM2: number;
  medianAreaM2: number;
  // Listings asking clearly under what the model thinks they are worth. Under-priced, not cheap.
  belowEstimateCount: number;
  // Renovation stock: needs work, or an energy certificate of D or worse.
  projectCount: number;
  projectMedianPricePerM2: number | null;
  moveInCount: number;
  moveInMedianPricePerM2: number | null;
  // Move-in price minus project price, in € per m². Null when either side is missing.
  renovationDiscountPerM2: number | null;
  calculatedAtUtc: string;
}

export interface MarketAreaLeaderboardResponse {
  items: MarketAreaStatsResponse[];
  // Null while the stats have never been worked out.
  calculatedAtUtc: string | null;
}

// The API filters, nothing more. Ordering happens in the browser: one level is a few hundred
// rows at most, so they all come over at once and re-sorting costs no request.
export interface MarketAreaLeaderboardQuery {
  level: AreaLevel;
  // Places with fewer listings than this are left out.
  minListings: number;
}

// --- What your money buys -----------------------------------------------------------

export type Typology = 'T0' | 'T1' | 'T2' | 'T3' | 'T4' | 'T5' | 'T6' | 'T7' | 'T8' | 'T9' | 'T10';

export interface MarketAreaBudgetItemResponse {
  displayName: string;
  district: string;
  municipality: string;
  town: string;
  // The most rooms the budget reaches here, on what that typology usually costs.
  bestTypology: Typology;
  medianPrice: number;
  medianAreaM2: number;
  medianPricePerM2: number;
  typologyListingCount: number;
  listingCount: number;
}

export interface MarketAreaBudgetResponse {
  budget: number;
  items: MarketAreaBudgetItemResponse[];
  calculatedAtUtc: string | null;
}

export interface MarketAreaBudgetQuery {
  budget: number;
  level: AreaLevel;
  minListings: number;
}

// --- Neighbour gaps -----------------------------------------------------------------

export interface NeighbourGapResponse {
  expensivePlace: string;
  expensivePricePerM2: number;
  expensiveListingCount: number;
  cheaperPlace: string;
  cheaperPricePerM2: number;
  cheaperListingCount: number;
  // Between the middle points of each place's listings, not between real borders.
  distanceKm: number;
  gapPercent: number;
}

export interface MarketAreaNeighbourGapResponse {
  items: NeighbourGapResponse[];
  calculatedAtUtc: string | null;
}

export interface MarketAreaNeighbourGapQuery {
  level: AreaLevel;
  minListings: number;
  maxDistanceKm: number;
  minGapPercent: number;
}
