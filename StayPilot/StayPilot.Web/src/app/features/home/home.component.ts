import { Component, OnInit, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { forkJoin } from 'rxjs';
import { MarketAreaStatsService } from '../../core/services/market-area-stats.service';
import { MarketOverviewService } from '../../core/services/market-overview.service';
import { MarketAreaBudgetItemResponse, MarketAreaStatsResponse, NeighbourGapResponse, RELIABLE_LISTINGS } from '../../core/models/market-area-stats';
import { MarketOverviewResponse, MarketOverviewTypology } from '../../core/models/market-overview';
import { NavLink } from '../../core/models/nav-groups';
import { apiErrorMessage } from '../../core/api-error';

// One example budget for the "what your money reaches" preview. Not configurable here — the
// real control lives on /market-areas/budget; this is a single real illustration of it.
const PREVIEW_BUDGET = 320_000;

// How many rows each narrative preview shows before it points the reader at the full screen.
const PREVIEW_ROWS = 5;

// The neighbour-gap preview's scope. Not the strictest possible reading — the same starting
// point market-area-neighbours.component.ts opens with (município grain, a modest listing
// floor, a 25km radius, a 20% gap floor), so the "biggest gap" shown here is the same kind of
// finding that screen leads with, not a cherry-picked extreme.
const PREVIEW_GAP_LEVEL = 'Municipality' as const;
const PREVIEW_GAP_MIN_LISTINGS = 5;
const PREVIEW_GAP_MAX_DISTANCE_KM = 25;
const PREVIEW_GAP_MIN_PERCENT = 20;

// The four "under the hood" cards. A hand-picked cross-group subset (Listings + Tools), not a
// full nav group, so it is its own small list rather than borrowed from one hub's links.
const UNDER_THE_HOOD: NavLink[] = [
  {
    title: 'Browse',
    path: '/listing-browser',
    desc: 'Filter and sort every listing by area, typology, price, size and beach distance.'
  },
  {
    title: 'Top deals',
    path: '/listings/top-deals',
    desc: "Active listings asking the most below their own typology's median in the same town."
  },
  {
    title: 'Feature impact',
    path: '/feature-impact',
    desc: 'What a garage, lift or sea view is worth as a premium, with confidence ranges.'
  },
  {
    title: 'Build cost',
    path: '/build-cost',
    desc: 'Shell, pool, garage, fees and VAT projected, held against local asking prices.'
  }
];

interface HeroStats {
  totalListings: number;
  placesTracked: number;
  districtsCovered: number;
  // Coast-wide, unfiltered — the same read GetMarketOverview gives the Market overview
  // screen when nothing is narrowed. Null only while that call itself is still loading.
  medianPricePerM2: number | null;
  busiestTypology: MarketOverviewTypology | null;
  busiest: MarketAreaStatsResponse | null;
}

// T3 sorts above T2, T10 above T9 — the number after the T, not the string.
function typologyRooms(typology: string): number {
  return Number(String(typology ?? '').replace(/^T/i, '')) || 0;
}

// StayPilot Comps landing page.
//
// Every number on it is real, read off the same MarketAreaStatsService the Leaderboard, Budget
// and Neighbour-gaps screens already use — nothing here is hardcoded or invented. Where the data
// cannot honestly answer a question (a blended market-wide median, a "last collection" figure,
// anything resembling measured demand), the figure is left out rather than approximated.
@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent implements OnInit {
  readonly previewBudget = PREVIEW_BUDGET;
  readonly underTheHood = UNDER_THE_HOOD;

  // --- Hero stat strip + "Where value sits" preview ---------------------------------
  // Both read off the same two leaderboard calls: District rows (unfiltered, top grain, so
  // summing listingCount across them cannot double-count) for the true total, Town rows
  // (unfiltered, for the place count; thinned to RELIABLE_LISTINGS for the extremes and the
  // preview table) for everything else.
  marketLoading = signal(true);
  marketError = signal<string | null>(null);
  private districtRows = signal<MarketAreaStatsResponse[]>([]);
  private townRows = signal<MarketAreaStatsResponse[]>([]);
  // Coast-wide asking-price read (median €, median €/m², typology mix) — the same unfiltered
  // GetMarketOverview call the Market overview screen makes when nothing is narrowed. Public:
  // the hero chart reads it directly for the typology mix and the total listing count.
  overview = signal<MarketOverviewResponse | null>(null);

  // Every typology on sale, smallest first — the hero chart. Reuses data the overview call
  // already carries, rather than a second endpoint: a coast-wide slice runs from T0 through
  // T6+, which is enough bars to read as a real chart rather than two or three columns.
  typologyMix = computed(() =>
    [...(this.overview()?.typologies ?? [])].sort((a, b) => typologyRooms(a.typology) - typologyRooms(b.typology))
  );
  typologyMax = computed(() => Math.max(0, ...this.typologyMix().map(t => t.listingCount)));
  // When the server last recalculated these figures. Either leaderboard call carries it —
  // whichever comes back non-null wins. There is no per-listing upload timestamp exposed
  // anywhere in the API, so this is the honest stand-in for "how fresh is this".
  calculatedAtUtc = signal<string | null>(null);

  private reliableTownRows = computed(() =>
    this.townRows().filter(row => row.listingCount >= RELIABLE_LISTINGS)
  );

  // --- "Renovation upside" preview ----------------------------------------------------
  // The single reliable town with the largest genuine, evidenced renovation discount.
  // Requires both a positive €/m² gap and server-provided evidence to back it — a discount
  // with no evidence record is not a finding, just two medians that happen to differ.
  renovationHighlight = computed<MarketAreaStatsResponse | null>(() =>
    this.reliableTownRows().reduce<MarketAreaStatsResponse | null>((best, row) => {
      if (row.renovationDiscountPerM2 == null || row.renovationDiscountPerM2 <= 0 || !row.renovationEvidence) {
        return best;
      }
      return !best || row.renovationDiscountPerM2 > (best.renovationDiscountPerM2 ?? -Infinity) ? row : best;
    }, null)
  );

  // The hero used to lead with a price comparison, but that's a finding, not a credibility
  // signal — it belongs down in "Borders you cannot see" (which already covers the same
  // ground with real neighbour-gap data). A flat coast-wide figure is different: it is not a
  // comparison between places, just one more measure of scale alongside listings/places/
  // districts, so the median price and typology mix now sit up here too.
  heroStats = computed<HeroStats | null>(() => {
    if (this.marketLoading() || this.marketError()) {
      return null;
    }

    const reliable = this.reliableTownRows();
    const overview = this.overview();

    return {
      totalListings: this.districtRows().reduce((sum, row) => sum + row.listingCount, 0),
      // The raw Town-row count, not the reliable-only one — this states how many places have
      // been measured at all, not how many are trustworthy.
      placesTracked: this.townRows().length,
      // One District-level row per district that has any data at all.
      districtsCovered: this.districtRows().length,
      medianPricePerM2: overview?.pricePerM2.median ?? null,
      // Most-listed typology across the whole coast, not the priciest or the cheapest — this
      // tile answers "what does this coast mostly sell", the same way "busiest" answers
      // "where" rather than "what's most in demand".
      busiestTypology:
        overview && overview.typologies.length > 0
          ? [...overview.typologies].sort((a, b) => b.listingCount - a.listingCount)[0]
          : null,
      // "Busiest" = most listings. There is no market-wide demand ranking anywhere in this
      // system — DemandLevel is wired into per-property valuation, not a place-level figure —
      // so this is deliberately not called "most in demand".
      busiest: reliable.reduce<MarketAreaStatsResponse | null>(
        (best, row) => (!best || row.listingCount > best.listingCount ? row : best),
        null
      )
    };
  });

  leaderboardPreview = computed(() =>
    [...this.reliableTownRows()].sort((a, b) => b.medianPricePerM2 - a.medianPricePerM2).slice(0, PREVIEW_ROWS)
  );

  // --- "What your money reaches" preview ---------------------------------------------
  budgetLoading = signal(true);
  budgetError = signal<string | null>(null);
  budgetPreview = signal<MarketAreaBudgetItemResponse[]>([]);
  // Charted on medianAreaM2, not medianPrice - every item here is already priced at (or just
  // under) the same fixed budget by construction, so the prices barely differ and a price bar
  // chart reads as flat/broken. Floor space is the number that actually varies place to place,
  // and it's the whole point of "what your money reaches" - the same budget buying more or
  // less room.
  budgetMax = computed(() => Math.max(0, ...this.budgetPreview().map(item => item.medianAreaM2)));

  // --- "Borders you cannot see" preview ------------------------------------------------
  gapLoading = signal(true);
  gapError = signal<string | null>(null);
  topGap = signal<NeighbourGapResponse | null>(null);

  constructor(
    private readonly service: MarketAreaStatsService,
    private readonly overviewService: MarketOverviewService
  ) {}

  ngOnInit(): void {
    this.loadMarketStats();
    this.loadBudgetPreview();
    this.loadGapPreview();
  }

  euro(value: number): string {
    return Number.isFinite(value) ? `€${Math.round(value).toLocaleString('en-GB')}` : '—';
  }

  formatCount(value: number): string {
    return Number.isFinite(value) ? value.toLocaleString('en-GB') : '—';
  }

  sqm(value: number): string {
    return Number.isFinite(value) ? Math.round(value).toLocaleString('en-GB') : '—';
  }

  formatKm(value: number): string {
    return Number.isFinite(value) ? `${value.toFixed(1)} km` : '—';
  }

  roundPercent(value: number): string {
    return Number.isFinite(value) ? Math.round(value).toString() : '—';
  }

  formatDate(iso: string): string {
    const date = new Date(iso);
    return Number.isNaN(date.getTime())
      ? '—'
      : date.toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' });
  }

  // Shared by every mini bar-chart on this page: height as a percentage of a scale's max,
  // floored at 8% so a bar never disappears next to a much larger one (a 5-10x spread between
  // cheapest and priciest, or project and move-in-ready, is common on this coast).
  barPct(value: number, max: number): number {
    return max > 0 ? Math.max(8, Math.round((value / max) * 100)) : 0;
  }

  // Two-bar comparisons (cheapest vs most expensive, pay-more vs pay-less, project vs
  // move-in ready): height relative to the larger of the pair, so the smaller bar shows its
  // real proportion instead of being independently normalised - the whole point of the chart.
  barPctOfPair(value: number, a: number, b: number): number {
    return this.barPct(value, Math.max(a, b));
  }

  // Mirrors market-area-renovation.component.ts's trustClass — same confidence, same badge.
  trustClass(evidence: { confidence: string } | null): string {
    switch (evidence?.confidence) {
      case 'High':
        return 'badge-high';

      case 'Medium':
        return 'badge-medium';

      default:
        return 'badge-low';
    }
  }

  private loadMarketStats(): void {
    this.marketLoading.set(true);
    this.marketError.set(null);

    forkJoin({
      // 1, not 0 - the API rejects MinListings outside [1, 1000] ([Range(1, 1000)] on
      // MarketAreaLeaderboardRequest), and 1 is already an effective "no filter" here: a
      // returned row always has at least one listing behind it.
      districts: this.service.getLeaderboard({ level: 'District', minListings: 1 }),
      towns: this.service.getLeaderboard({ level: 'Town', minListings: 1 }),
      // No filters = the whole coast, the same call the Market overview screen makes with
      // nothing narrowed — gives the hero a real median price/€/m² and typology mix.
      overview: this.overviewService.getMarketOverview({})
    }).subscribe({
      next: ({ districts, towns, overview }) => {
        this.districtRows.set(districts.items);
        this.townRows.set(towns.items);
        this.overview.set(overview);
        this.calculatedAtUtc.set(districts.calculatedAtUtc ?? towns.calculatedAtUtc ?? null);
        this.marketLoading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.districtRows.set([]);
        this.townRows.set([]);
        this.overview.set(null);
        this.marketError.set(apiErrorMessage(err, 'Could not load the market stats.'));
        this.marketLoading.set(false);
      }
    });
  }

  private loadBudgetPreview(): void {
    this.budgetLoading.set(true);
    this.budgetError.set(null);

    this.service
      .getBudgetRanking({ budget: PREVIEW_BUDGET, level: 'Town', minListings: RELIABLE_LISTINGS })
      .subscribe({
        next: response => {
          const rows = [...response.items]
            .sort((a, b) => typologyRooms(b.bestTypology) - typologyRooms(a.bestTypology))
            .slice(0, PREVIEW_ROWS);

          this.budgetPreview.set(rows);
          this.budgetLoading.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.budgetPreview.set([]);
          this.budgetError.set(apiErrorMessage(err, 'Could not load a budget example.'));
          this.budgetLoading.set(false);
        }
      });
  }

  private loadGapPreview(): void {
    this.gapLoading.set(true);
    this.gapError.set(null);

    this.service
      .getNeighbourGaps({
        level: PREVIEW_GAP_LEVEL,
        minListings: PREVIEW_GAP_MIN_LISTINGS,
        maxDistanceKm: PREVIEW_GAP_MAX_DISTANCE_KM,
        minGapPercent: PREVIEW_GAP_MIN_PERCENT
      })
      .subscribe({
        next: response => {
          const biggest = [...response.items].sort((a, b) => b.gapPercent - a.gapPercent)[0] ?? null;
          this.topGap.set(biggest);
          this.gapLoading.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.topGap.set(null);
          this.gapError.set(apiErrorMessage(err, 'Could not load a neighbour-gap example.'));
          this.gapLoading.set(false);
        }
      });
  }
}
