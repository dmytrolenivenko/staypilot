// Build-cost estimator reference data.
//
// These are STATIC baselines, not live prices. No Portuguese building retailer
// (Leroy Merlin PT, Maxmat, ...) exposes a public product/price API, so we use
// €/m² baselines grounded in public figures: INE's construction cost index and
// widely-cited 2026 private-build ranges (~€1,100–1,700/m² for a mid-range house,
// excluding land, taxes and professional fees).
//
// Tune these numbers in one place as the market moves — nothing else needs to change.

// A build quality tier and its all-in construction rate per m².
export interface QualityTier {
  key: string;
  label: string;
  ratePerM2: number; // € per m² of built area, standard finishes for the tier
}

export const QUALITY_TIERS: QualityTier[] = [
  { key: 'economy', label: 'Economy (basic finishes)', ratePerM2: 1000 },
  { key: 'standard', label: 'Standard (mid-range)', ratePerM2: 1300 },
  { key: 'premium', label: 'Premium (high-end finishes)', ratePerM2: 1700 },
  { key: 'luxury', label: 'Luxury (bespoke)', ratePerM2: 2300 }
];

// Regional cost multiplier — labour and logistics vary across Portugal.
export interface Region {
  key: string;
  label: string;
  multiplier: number;
}

export const REGIONS: Region[] = [
  { key: 'lisboa', label: 'Lisboa', multiplier: 1.2 },
  { key: 'algarve', label: 'Algarve', multiplier: 1.15 },
  { key: 'porto', label: 'Porto', multiplier: 1.1 },
  { key: 'coastal', label: 'Other coastal', multiplier: 1.05 },
  { key: 'interior', label: 'Interior / rural', multiplier: 1.0 }
];

// Optional fixed-cost extras added on top of the per-m² construction cost.
export interface BuildExtra {
  key: string;
  label: string;
  cost: number; // fixed € (rough turnkey figure)
}

export const BUILD_EXTRAS: BuildExtra[] = [
  { key: 'pool', label: 'Swimming pool', cost: 25000 },
  { key: 'garage', label: 'Garage', cost: 15000 },
  { key: 'elevator', label: 'Elevator', cost: 20000 },
  { key: 'solar', label: 'Solar panels', cost: 8000 },
  { key: 'automation', label: 'Home automation', cost: 5000 },
  { key: 'landscaping', label: 'Landscaping / garden', cost: 6000 }
];
