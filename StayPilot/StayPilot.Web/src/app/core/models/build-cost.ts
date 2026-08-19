// Build-cost types.
//
// There are no construction prices in this file, and that is the point. Every rate the screen
// shows comes from the API, which derives it from INE's construction cost index — public,
// monthly, not stored anywhere. A price list written down here would start rotting the day it
// was typed.
//
// What is left is the arithmetic that runs over what the *user* picks, which stays in the
// browser so the screen keeps recomputing as you go.

/** One priced choice, already escalated to today. Read whichever of `ratePerM2` and `cost` is present. */
export interface BuildCostOption {
  key: string;
  label: string;
  ratePerM2?: number;
  cost?: number;
  /** Pools only: the price stops falling below this, because the plant room does not. */
  minCost?: number;
  /** Garden sizes and garage bays: the area the option stands for. */
  areaM2?: number;
  /** Solar only. Never netted off `cost` — it gets its own line on the receipt. */
  grant?: number;
  note: string;
}

/** Mirrors BuildCostBasisResponse on the API. */
export interface BuildCostBasis {
  /** The month behind these rates. Empty when INE was unreachable and the rates are at 2021 prices. */
  indexPeriod: string;
  sinceBasePercent: number;
  tiers: BuildCostOption[];
  pools: BuildCostOption[];
  poolAddons: BuildCostOption[];
  garages: BuildCostOption[];
  elevators: BuildCostOption[];
  automation: BuildCostOption[];
  gardens: BuildCostOption[];
  solar: BuildCostOption[];
  extras: BuildCostOption[];
  gardenRatePerM2: number;
  vatPercent: number;
}

// --- What the browser owns ---------------------------------------------------------------

/**
 * A rough regional adjustment on the work done on site — labour and logistics do vary across
 * Portugal. Stays a plain choice rather than something derived: INE publishes house prices per
 * município, not build costs, and turning one into the other would be a guess dressed up as a
 * measurement.
 */
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

// Soft costs, as percentages of the works. Architecture and engineering run 3–6% of the contract
// on a normal moradia; municipal licences and taxes land near 2%.
export const DEFAULT_DESIGN_PERCENT = 6;
export const DEFAULT_LICENCE_PERCENT = 2;
export const DEFAULT_CONTINGENCY_PERCENT = 10;

// How wrong this whole thing can be. Estimates of this kind miss low more often than high —
// scope grows, ground conditions surprise, finishes get upgraded mid-build — so the band is
// deliberately asymmetric rather than a tidy ±20%.
export const ESTIMATE_LOW_FACTOR = 0.85;
export const ESTIMATE_HIGH_FACTOR = 1.25;

/**
 * How long the build runs. A T3 of 120–150 m² takes 12–18 months from first stone to the licença
 * de utilização: a fixed run of paperwork and groundwork, plus time proportional to size, plus a
 * month each for the two things that need a trade of their own on site.
 */
export function estimatedMonths(areaM2: number, hasPool: boolean, hasElevator: boolean): number {
  const months = 9 + Math.max(0, areaM2) / 30 + (hasPool ? 1 : 0) + (hasElevator ? 1 : 0);

  return Math.min(30, Math.max(10, Math.round(months)));
}
