import { PropertyCondition, PropertyType, Typology } from './enums';

// How much to trust the estimate. Mirrors StayPilot.Domain.Enums.ValuationConfidence
// (serialized as a string by the API).
export type ValuationConfidence = 'Low' | 'Medium' | 'High';

// Mirrors StayPilot.Application.Contracts.Request.OwnedPropertyRequest.
// Name, PropertyType, Typology and AreaM2 are required by the API.
export interface OwnedPropertyRequest {
  name: string;
  country?: string;
  district: string;
  municipality: string;
  town: string;
  zone?: string | null;
  purchasePrice?: number | null;
  purchaseDate?: string | null; // ISO date
  propertyType: PropertyType;
  typology: Typology;
  areaM2: number;
  bathrooms: number;
  floor?: number | null;
  totalFloors?: number | null;
  hasElevator?: boolean | null;
  hasAirConditioning?: boolean | null;
  condition?: PropertyCondition | null;
  constructionYear?: number | null;
  renovationYear?: number | null;
  renovationInvestment?: number | null;
  balconyCount?: number | null;
  hasTerrace?: boolean | null;
  hasGarage?: boolean | null;
  hasParking?: boolean | null;
  hasSwimmingPool?: boolean | null;
  isFurnished?: boolean | null;
  hasSeaView?: boolean | null;
  hasCityView?: boolean | null;
  latitude?: number | null;
  longitude?: number | null;
  energyCertificate?: string | null;
  notes?: string | null;
}

// Mirrors StayPilot.Application.Contracts.Response.OwnedPropertyResponse.
export interface OwnedPropertyResponse {
  id: number;
  name: string;
  marketAreaId: number;
  purchasePrice?: number | null;
  purchaseDate?: string | null;
  propertyType: PropertyType;
  typology: Typology;
  areaM2: number;
  bathrooms: number;
  floor?: number | null;
  totalFloors?: number | null;
  hasElevator?: boolean | null;
  hasAirConditioning?: boolean | null;
  condition?: PropertyCondition | null;
  constructionYear?: number | null;
  renovationYear?: number | null;
  renovationInvestment?: number | null;
  balconyCount?: number | null;
  hasTerrace?: boolean | null;
  hasGarage?: boolean | null;
  hasParking?: boolean | null;
  hasSwimmingPool?: boolean | null;
  isFurnished?: boolean | null;
  hasSeaView?: boolean | null;
  hasCityView?: boolean | null;
  distanceToBeachMeters?: number | null;
  nearestBeachMarkerId?: number | null;
  nearestBeachName?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  energyCertificate?: string | null;
  notes?: string | null;
}

// --- Valuation ------------------------------------------------------------
// Mirrors StayPilot.Application.Contracts.Response.OwnedPropertyAnalysisResponse
// and its sub-responses (ValuationAdjustment, ValuationComp, AskSpreadSummary).

// One comparable listing that fed the estimate.
export interface ValuationComp {
  areaM2: number;
  pricePerM2: number;
  distanceToBeachMeters?: number | null;
  typology: Typology;
  snapshotDateUtc: string; // ISO date
}

// A single +/- tweak to the raw market estimate (e.g. "Sea view", "Needs renovation").
export interface ValuationAdjustment {
  label: string;
  amount: number;

  // What the amount was measured against, for the per-unit features — "3 vs 1.8 typical".
  // Null for plain yes/no features, where having it is the whole story.
  detail?: string | null;

  // False when the feature's confidence range straddles zero. The amount is still part of
  // the estimate, it just isn't a finding — shown greyed rather than dropped.
  isMeasurable: boolean;
}

// Purchase-vs-now block. Only meaningful when the property has a purchase price/date.
export interface AskSpreadSummary {
  purchasePrice: number;
  estimatedAskingPrice: number;
  spreadAmount: number;
  spreadPercent: number;
  yearsHeld: number;
  // Null under a year held: annualising weeks of movement is noise, and the screen says so
  // rather than printing 0, which reads as "returned nothing".
  spreadPerYearPercent: number | null;
  spreadPerMonthPercent: number;
}

// The full valuation result for one owned property.
export interface OwnedPropertyAnalysisResponse {
  minPrice: number;
  midPrice: number;
  maxPrice: number;
  averagePrice: number;

  confidenceLevel: ValuationConfidence;

  // compsCount is what the numbers rest on (the nearest few); comparablesFound is how many the
  // search turned up in total. Quoting the second next to statistics drawn from the first is how
  // a figure built on 25 adverts came to look like it rested on 317.
  compsCount: number;
  comparablesFound: number;

  marketRatePerM2: number;
  estimateBeforeAdjustments: number;

  // The middle half of the comps (P25-P75), not the full range - see the API contract.
  compPricePerM2P25: number;
  medianCompPricePerM2: number;
  compPricePerM2P75: number;
  averageCompPricePerM2: number;

  adjustments: ValuationAdjustment[];

  // Only the nearest few of compsCount — the statistics above are over all of them.
  comps: ValuationComp[];

  // Which zone the price was actually taken from. The coordinates decide this, so it does not
  // always match the zone stored on the property — and when it doesn't, that is the single most
  // useful thing on the screen for explaining a surprising number.
  locatedMarketAreaId: number;
  locatedAreaName: string;
  locatedByCoordinates: boolean;

  askSpread: AskSpreadSummary;
}

// How keen buyers are in a place. Mirrors StayPilot.Domain.Enums.DemandLevel.
export type DemandLevel = 'Cold' | 'Soft' | 'Balanced' | 'Firm' | 'Hot';

// The demand score for one place and the working behind it.
// Read isMeasurable BEFORE level: when it is false the level means "not measured", not "average".
export interface AreaDemandResponse {
  level: DemandLevel;
  score: number;
  isMeasurable: boolean;
  placeName: string;
  medianDaysOnMarket: number | null;
  daysMeasuredOnSold: boolean;
  daysScore: number | null;
  newListingsRecent: number;
  newListingsPrevious: number;
  supplyChangePercent: number | null;
  supplyScore: number | null;
  sampleSize: number;
  collectionSpanDays: number;
  reason: string;
}

// One projected path. values[0] is today, values[n] the end of year n.
export interface GrowthScenarioResponse {
  name: string;
  annualPercent: number;
  nextYearValue: number;
  finalYearValue: number;
  values: number[];
}

// Where a property's value is heading, with the two rates behind it kept apart.
// seededAnnualPercent is an assumption (read seededSource, it says so); localAnnualPercent is
// measured from the adverts nearby. Neither is the forecast on its own.
export interface GrowthForecastResponse {
  seededAnnualPercent: number;
  seededSource: string;
  seededDistrict: string;
  localAnnualPercent: number | null;
  localWeightPercent: number;
  localWasCapped: boolean;
  localSnapshotCount: number;
  localSpanDays: number;
  localMonthsObserved: number;
  localReason: string;
  blendedAnnualPercent: number;
  years: number;
  scenarios: GrowthScenarioResponse[];
}

// One owned property, priced, with what its place is doing around it.
export interface OwnedPropertyPortfolioItemResponse {
  id: number;
  name: string;
  propertyType: PropertyType;
  typology: Typology;
  areaM2: number;

  district: string;
  municipality: string;
  town: string;
  locatedAreaName: string;
  locatedByCoordinates: boolean;

  midPrice: number;
  minPrice: number;
  maxPrice: number;
  pricePerM2: number;
  confidenceLevel: ValuationConfidence;
  confidenceNote: string;
  askSpread: AskSpreadSummary;

  demand: AreaDemandResponse;
  forecast: GrowthForecastResponse;

  // Null when this property has never been recalculated — it still shows up in the list,
  // just with nothing priced yet.
  calculatedAtUtc: string | null;
}

// Every owned property priced in one pass. One request, because the valuation model is fitted
// over the whole listing table — ten separate calls would be ten fits of the same model.
export interface OwnedPropertyPortfolioResponse {
  items: OwnedPropertyPortfolioItemResponse[];
  propertyCount: number;
  totalEstimatedAskingPrice: number;
  totalPurchasePrice: number;
  totalAskSpreadAmount: number;
  totalAskSpreadPercent: number;
  totalProjectedAskingPrice: number;
  projectionYears: number;
  generatedAtUtc: string;
}

// The result of recalculating one owned property's valuation. item stays null when the API
// could not price it (not found, or too little market data).
export interface OwnedPropertyValuationResponse {
  item: OwnedPropertyPortfolioItemResponse | null;
}
