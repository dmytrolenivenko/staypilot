import { BuildCostOption } from './build-cost';
import { PropertyCondition } from './enums';
import { ValuationConfidence } from './owned-property';

// Mirrors StayPilot.Application.Contracts.Response.InvestmentAnalysisResponse.
// Narrative is the only field an AI ever touches, and only to describe the numbers
// beside it — never to produce one of its own.
export interface InvestmentAnalysisResponse {
  propertyListingId: number | null;
  ownedPropertyId: number | null;
  askPrice: number;
  areaM2: number;
  condition: PropertyCondition;
  district: string;
  municipality: string;
  town: string;
  townMoveInMedianPricePerM2: number;
  townMoveInListingCount: number;
  estimatedRenovationCost: number;
  renovationCostIsOverride: boolean;
  /** Cosmetic / Full renovation / Full rebuild — fixed prices to pick from before typing a custom one. */
  renovationOptions: BuildCostOption[];
  estimatedResaleValue: number;
  totalInvestment: number;
  estimatedProfit: number;
  profitMarginPercent: number;
  confidence: ValuationConfidence;
  calculatedAtUtc: string;
  narrative: string | null;
}
