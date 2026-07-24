// Mirrors StayPilot.Application.Contracts.Response.PremiumFeatureResponse.
// How much a single feature (e.g. a sea view) typically adds to the price.
export interface PremiumFeatureResponse {
  feature: string;        // e.g. "HasSeaView"
  premiumPercent: number; // average price difference for having it, as a percentage
  calculatedAtUtc: string; // ISO date-time
}
