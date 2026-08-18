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
  // Of projectCount: flagged by the advert itself vs. caught only by a poor energy grade.
  projectByConditionCount: number;
  projectByEnergyCount: number;
  projectMedianPricePerM2: number | null;
  projectMedianAreaM2: number | null;
  // The middle half of the project prices — what says whether the discount is real.
  projectP25PricePerM2: number | null;
  projectP75PricePerM2: number | null;
  moveInCount: number;
  moveInMedianPricePerM2: number | null;
  moveInMedianAreaM2: number | null;
  moveInP25PricePerM2: number | null;
  moveInP75PricePerM2: number | null;
  // Neither a project nor clearly finished, so left out of both sides but counted.
  unclassifiedCount: number;
  // Move-in price minus project price, in € per m². Null when either side is missing.
  renovationDiscountPerM2: number | null;
  // Why that discount should or should not be believed. Null when there is none to judge.
  renovationEvidence: RenovationEvidence | null;
  calculatedAtUtc: string;
}

export type Confidence = 'Low' | 'Medium' | 'High';

// Why one place's renovation discount deserves to be trusted. Two medians always differ by
// something; this is what says whether that something is a finding.
export interface RenovationEvidence {
  confidence: Confidence;
  // How much the two middle halves overlap, against the narrower one. 0 = clean separation.
  spreadOverlapPercent: number;
  // Share of the place's listings that got a verdict at all, project or finished.
  classifiedSharePercent: number;
  // The one-line reason behind the verdict, written on the server so every screen agrees.
  reason: string;
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
  // Narrow to one distrito, and inside it one município. Empty means the whole country.
  district?: string;
  municipality?: string;
}

// --- What your money buys -----------------------------------------------------------

export type Typology = 'T0' | 'T1' | 'T2' | 'T3' | 'T4' | 'T5' | 'T6' | 'T7' | 'T8' | 'T9' | 'T10';

export interface MarketAreaBudgetItemResponse {
  level: AreaLevel;
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
  // True when the place is only in reach because the budget was stretched.
  needsStretch: boolean;
  // Every typology the budget reaches here, most rooms first — not only the biggest.
  affordableTypologies: MarketAreaBudgetTypology[];
}

// One typology a budget reaches, as an alternative to the headline answer.
export interface MarketAreaBudgetTypology {
  typology: Typology;
  medianPrice: number;
  medianAreaM2: number;
  medianPricePerM2: number;
  listingCount: number;
}

export interface MarketAreaBudgetResponse {
  budget: number;
  // The budget after the stretch — what a place had to come in under to appear at all.
  reach: number;
  items: MarketAreaBudgetItemResponse[];
  calculatedAtUtc: string | null;
}

export interface MarketAreaBudgetQuery {
  budget: number;
  level: AreaLevel;
  minListings: number;
  district?: string;
  municipality?: string;
  // Leave out places where the budget does not reach at least this many rooms.
  minTypology?: Typology;
  // How far past the budget it may stretch, as a percentage. 0 = strict.
  stretchPercent?: number;
}

// --- Neighbour gaps -----------------------------------------------------------------

// One half of a pair, with the place broken into its parts so the screen can say which grain
// it is showing instead of leaving a bracket to be guessed at.
export interface NeighbourGapPlace {
  level: AreaLevel;
  district: string;
  municipality: string;
  town: string;
  // The place on one line, for the anchor picker and anywhere a table cell would not fit.
  displayName: string;
  // The price the gap was worked out from: all stock, or one typology's stock when narrowed.
  medianPricePerM2: number;
  listingCount: number;
  // The place's median across all its stock, whatever the comparison ran on. The two together
  // are the finding: "T2s 30% apart, all stock 4% apart" says the gap is about the flats.
  allStockPricePerM2: number;
  allStockListingCount: number;
}

export interface NeighbourGapResponse {
  expensive: NeighbourGapPlace;
  cheaper: NeighbourGapPlace;
  // Between the middle points of each place's listings, not between real borders.
  distanceKm: number;
  gapPercent: number;
}

export interface MarketAreaNeighbourGapResponse {
  items: NeighbourGapResponse[];
  // The typology every pair was compared on. Null means all stock at once.
  comparedOn: Typology | null;
  calculatedAtUtc: string | null;
}

export interface MarketAreaNeighbourGapQuery {
  level: AreaLevel;
  minListings: number;
  maxDistanceKm: number;
  minGapPercent: number;
  district?: string;
  municipality?: string;
  // Compare like with like. Without it a 30% gap can be entirely explained by one place
  // selling villas while the other sells studios.
  typology?: Typology;
  // The fewest listings of that typology a place needs to be half of a pair.
  minTypologyListings?: number;
}
