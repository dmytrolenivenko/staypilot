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
// and its sub-responses (ValuationAdjustment, ValuationComp, EquitySummary).

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
export interface EquitySummary {
  purchasePrice: number;
  currentEstimate: number;
  gainAmount: number;
  gainPercent: number;
  yearsHeld: number;
  roiPerYear: number;
  roiPerMonth: number;
}

// The full valuation result for one owned property.
export interface OwnedPropertyAnalysisResponse {
  minPrice: number;
  midPrice: number;
  maxPrice: number;
  averagePrice: number;

  confidenceLevel: ValuationConfidence;
  compsCount: number;

  marketRatePerM2: number;
  estimateBeforeAdjustments: number;

  minCompPricePerM2: number;
  medianCompPricePerM2: number;
  maxCompPricePerM2: number;
  averageCompPricePerM2: number;

  adjustments: ValuationAdjustment[];
  comps: ValuationComp[];
  equity: EquitySummary;
}
