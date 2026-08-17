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
