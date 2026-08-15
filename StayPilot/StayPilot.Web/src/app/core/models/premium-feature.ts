// Mirrors StayPilot.Application.Contracts.Response.PremiumFeatureResponse.
// How much a single feature (e.g. a sea view) typically adds to the price.
export interface PremiumFeatureResponse {
  feature: string;        // e.g. "HasSeaView"
  premiumPercent: number; // average price difference for having it, as a percentage
  lowerBoundPercent: number; // bottom of the 95% confidence range
  upperBoundPercent: number; // top of the 95% confidence range
  // False when the confidence range includes zero, i.e. the data cannot tell whether this
  // feature affects price at all. Show "no measurable effect", NOT the headline percentage —
  // printing "-0.2%" for one of these reads as "it makes the flat cheaper", which is wrong.
  isMeasurable: boolean;
  sampleSize: number;    // how many listings the estimate was fitted on
  // How many of those listings actually HAVE this feature AND had a comparable flat to be
  // measured against — the real evidence behind the percentage. Show it next to sampleSize:
  // the comparison size is identical on every row, so on its own it made a sea view read on
  // 1.2k listings look as solid as a bathroom read on 4.8k.
  listingsWithFeature: number;
  // The best this feature is worth under the conditions that favour it most — shown as "up to
  // X%". Null for features whose worth doesn't vary, which is most of them; today the sea view
  // (worth far more from the sand than from a sliver of horizon) and the lift (worth roughly
  // twice as much from the third floor up) both carry one. NEVER show it without maximumBasis:
  // an "up to" with no stated conditions is a marketing claim, not a measurement.
  maximumPercent: number | null;
  maximumBasis: string | null; // e.g. "within 500m of the beach"
  // What the percentage is measured against, when "if present" would mislead — "per bathroom",
  // "within 500m of the beach". Null for ordinary yes/no features.
  basis: string | null;
  calculatedAtUtc: string; // ISO date-time
}
