import { Component, OnInit, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { forkJoin } from 'rxjs';
import { MarketAreaStatsService } from '../../core/services/market-area-stats.service';
import { MarketOverviewService } from '../../core/services/market-overview.service';
import {
  MarketAreaBudgetItemResponse,
  MarketAreaStatsResponse,
  NeighbourGapResponse,
  RELIABLE_LISTINGS
} from '../../core/models/market-area-stats';
import { MarketOverviewResponse } from '../../core/models/market-overview';
import { NavLink } from '../../core/models/nav-groups';
import { apiErrorMessage } from '../../core/api-error';
import { BarChartComponent, BarChartItem } from '../../shared/bar-chart.component';
import { CompareBarsComponent, CompareSide } from '../../shared/compare-bars.component';
import { APP_LOCALE } from '../../core/locale';

// One example budget for the "what your money reaches" preview. Not configurable here — the
// real control lives on /places?ask=budget; this is a single real illustration of it.
const PREVIEW_BUDGET = 320_000;

// How many rows each preview shows before it points the reader at the full screen.
const PREVIEW_ROWS = 5;

// How many areas the hero panel charts. Five, not eight: the panel sits beside the headline now
// rather than spanning the page, and a taller chart there pushes the buttons off the first screen.
const HERO_TOP_AREAS = 5;

// The neighbour-gap preview's scope. Not the strictest possible reading — the same starting
// point the neighbours screen opens with (município grain, a modest listing floor, a 25km
// radius, a 20% gap floor), so the "biggest gap" shown here is the same kind of finding that
// screen leads with, not a cherry-picked extreme.
const PREVIEW_GAP_LEVEL = 'Municipality' as const;
const PREVIEW_GAP_MIN_LISTINGS = 5;
const PREVIEW_GAP_MAX_DISTANCE_KM = 25;
const PREVIEW_GAP_MIN_PERCENT = 20;

// The area of a typical apartment, used only to restate a €/m² gap as a number in euros.
// Stated in the copy wherever it is used, because it is an illustration, not a measurement.
const TYPICAL_APARTMENT_M2 = 90;

// The four "what else is in here" cards. A hand-picked cross-section, not a full nav group.
const ELSEWHERE: NavLink[] = [
  {
    title: $localize`:@@home.elsewhere.browse.title:Browse every listing`,
    path: '/listings',
    query: { ask: 'browse' },
    desc: $localize`:@@home.elsewhere.browse.desc:Filter and sort by area, typology, price, size and distance to the beach.`
  },
  {
    title: $localize`:@@home.elsewhere.deals.title:Best deals`,
    path: '/listings',
    query: { ask: 'deals' },
    desc: $localize`:@@home.elsewhere.deals.desc:Listings asking the most below their own typology's median in the same town.`
  },
  {
    title: $localize`:@@home.elsewhere.features.title:What a feature is worth`,
    path: '/tools',
    query: { ask: 'features' },
    desc: $localize`:@@home.elsewhere.features.desc:The premium on a garage, a lift or a sea view, with a confidence range.`
  },
  {
    title: $localize`:@@home.elsewhere.build.title:Build cost`,
    path: '/tools',
    query: { ask: 'build' },
    desc: $localize`:@@home.elsewhere.build.desc:Shell, pool, garage, fees and VAT, held against local asking prices.`
  }
];

/** One figure in the coverage band: how much there is to read, said plainly. */
interface CoverageStat {
  label: string;
  value: string;
}


// T3 sorts above T2, T10 above T9 — the number after the T, not the string.
function typologyRooms(typology: string): number {
  return Number(String(typology ?? '').replace(/^T/i, '')) || 0;
}

/**
 * The landing page.
 *
 * Every number on it is real, read off the same services the Places and Listings screens use —
 * nothing here is hardcoded or invented. Where the data cannot honestly answer a question (a
 * blended market-wide median, a "last collection" figure, anything resembling measured demand),
 * the figure is left out rather than approximated.
 *
 * The four previews are a grid rather than four alternating full-width bands. They are four
 * answers of equal standing, and the alternating layout made the page three screens long while
 * implying an order of importance that does not exist.
 */
@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, BarChartComponent, CompareBarsComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent implements OnInit {
  readonly previewBudget = PREVIEW_BUDGET;
  readonly typicalApartmentM2 = TYPICAL_APARTMENT_M2;
  readonly elsewhere = ELSEWHERE;
  readonly budgetCaption = $localize`:@@home.budget.caption:Budget ${this.euro(PREVIEW_BUDGET)}:budget:`;

  // --- Market stats: the hero strip and three of the four previews -------------------
  marketLoading = signal(true);
  marketError = signal<string | null>(null);
  private districtRows = signal<MarketAreaStatsResponse[]>([]);
  private townRows = signal<MarketAreaStatsResponse[]>([]);
  private municipalityRows = signal<MarketAreaStatsResponse[]>([]);
  overview = signal<MarketOverviewResponse | null>(null);

  // When the server last recalculated these figures. Either leaderboard call carries it —
  // whichever comes back non-null wins.
  calculatedAtUtc = signal<string | null>(null);

  private reliableTownRows = computed(() =>
    this.townRows().filter(row => row.listingCount >= RELIABLE_LISTINGS)
  );
  private reliableMunicipalityRows = computed(() =>
    this.municipalityRows().filter(row => row.listingCount >= RELIABLE_LISTINGS)
  );

  // --- Hero --------------------------------------------------------------------------

  /** One District-level row per district that has any data at all. */
  districtCount = computed(() => this.districtRows().length);

  /**
   * The coverage band: four figures about the size of the collection, not about the market.
   *
   * Scale, not findings — the prices belong in the panel above, where the slice they describe
   * is named. Null while the numbers are still coming, which is what draws the skeletons.
   */
  coverage = computed<CoverageStat[] | null>(() => {
    if (this.marketLoading() || this.marketError()) {
      return null;
    }

    // Every usable listing lands in exactly one district row, so summing them at the top
    // grain is the true total and cannot double-count.
    const totalListings = this.districtRows().reduce((sum, row) => sum + row.listingCount, 0);

    return [
      { label: $localize`:@@home.coverage.adverts:Adverts tracked`, value: this.formatCount(totalListings) },
      // The raw Town-row count, not the reliable-only one: how many places have been measured
      // at all, not how many are trustworthy enough to rank.
      { label: $localize`:@@home.coverage.towns:Towns covered`, value: this.formatCount(this.townRows().length) },
      { label: $localize`:@@home.coverage.districts:Districts, wall to wall`, value: this.formatCount(this.districtCount()) },
      {
        label: $localize`:@@home.coverage.cadence:Collection & recalculation`,
        value: $localize`:@@home.coverage.daily:Daily`
      }
    ];
  });

  /**
   * The panel's two headline figures, already formatted.
   *
   * Named reads rather than an `@if (overview(); as ov)` alias in the template: `as` is only
   * allowed on a primary `@if`, and these sit in the `@else if` after the loading branch.
   */
  medianPrice = computed(() => {
    const overview = this.overview();

    return overview ? this.euro(overview.price.median) : '—';
  });

  medianPricePerM2 = computed(() => {
    const overview = this.overview();

    return overview ? this.euro(overview.pricePerM2.median) : '—';
  });


  /** The hero chart: where the stock actually is, by município. */
  busiestAreas = computed<BarChartItem[]>(() =>
    [...this.reliableMunicipalityRows()]
      .sort((a, b) => b.listingCount - a.listingCount)
      .slice(0, HERO_TOP_AREAS)
      .map(row => ({
        label: row.displayName,
        value: row.listingCount,
        valueText: this.formatCount(row.listingCount),
        note: `${this.euro(row.medianPricePerM2)}/m²`
      }))
  );

  // --- Preview 1: where value sits -----------------------------------------------------

  priciestPlaces = computed<BarChartItem[]>(() =>
    [...this.reliableTownRows()]
      .sort((a, b) => b.medianPricePerM2 - a.medianPricePerM2)
      .slice(0, PREVIEW_ROWS)
      .map(row => ({
        label: row.displayName,
        value: row.medianPricePerM2,
        valueText: `${this.euro(row.medianPricePerM2)}`,
        note: this.listingsNote(row.listingCount)
      }))
  );

  // --- Preview 2: what your money reaches ----------------------------------------------

  budgetLoading = signal(true);
  budgetError = signal<string | null>(null);
  private budgetRows = signal<MarketAreaBudgetItemResponse[]>([]);

  /**
   * Charted on floor space, not price: every place here is already priced at (or just under)
   * the same fixed budget by construction, so a price chart reads as flat. Floor space is what
   * actually varies, and is the whole point — the same money buying more or less room.
   */
  budgetPlaces = computed<BarChartItem[]>(() =>
    this.budgetRows().map(item => ({
      label: item.displayName,
      value: item.medianAreaM2,
      valueText: `${this.sqm(item.medianAreaM2)} m²`,
      note: `${item.bestTypology} · ${this.euro(item.medianPrice)}`
    }))
  );

  // --- Preview 3: neighbour gaps --------------------------------------------------------

  gapLoading = signal(true);
  gapError = signal<string | null>(null);
  topGap = signal<NeighbourGapResponse | null>(null);

  gapCheaper = computed<CompareSide | null>(() => {
    const gap = this.topGap();

    return gap
      ? {
          label: gap.cheaper.displayName,
          value: gap.cheaper.medianPricePerM2,
          valueText: `${this.euro(gap.cheaper.medianPricePerM2)}/m²`,
          note: this.listingsNote(gap.cheaper.listingCount)
        }
      : null;
  });

  gapDearer = computed<CompareSide | null>(() => {
    const gap = this.topGap();

    return gap
      ? {
          label: gap.expensive.displayName,
          value: gap.expensive.medianPricePerM2,
          valueText: `${this.euro(gap.expensive.medianPricePerM2)}/m²`,
          note: this.listingsNote(gap.expensive.listingCount)
        }
      : null;
  });

  /** The gap itself, worded for the chart's delta line. */
  gapDelta = computed<string>(() => {
    const gap = this.topGap();

    return gap
      ? $localize`:@@home.gap.delta:${this.roundPercent(gap.gapPercent)}:percent:% apart · ${this.formatKm(gap.distanceKm)}:distance:`
      : '';
  });

  /** The same gap restated as money on a typical apartment, which is how people hold it. */
  gapOnTypicalFlat = computed<string | null>(() => {
    const gap = this.topGap();

    if (!gap) {
      return null;
    }

    const perM2 = gap.expensive.medianPricePerM2 - gap.cheaper.medianPricePerM2;

    return this.euro(perM2 * TYPICAL_APARTMENT_M2);
  });

  // --- Preview 4: renovation upside ------------------------------------------------------

  /**
   * The single reliable town with the largest genuine, evidenced renovation discount. Requires
   * both a positive €/m² gap and server-provided evidence to back it — a discount with no
   * evidence record is not a finding, just two medians that happen to differ.
   */
  renovationHighlight = computed<MarketAreaStatsResponse | null>(() =>
    this.reliableTownRows().reduce<MarketAreaStatsResponse | null>((best, row) => {
      if (row.renovationDiscountPerM2 == null || row.renovationDiscountPerM2 <= 0 || !row.renovationEvidence) {
        return best;
      }

      return !best || row.renovationDiscountPerM2 > (best.renovationDiscountPerM2 ?? -Infinity) ? row : best;
    }, null)
  );

  renovationProject = computed<CompareSide | null>(() => {
    const area = this.renovationHighlight();

    return area?.projectMedianPricePerM2
      ? {
          label: $localize`:@@home.renovation.needsWork:Needs work`,
          value: area.projectMedianPricePerM2,
          valueText: `${this.euro(area.projectMedianPricePerM2)}/m²`,
          note: this.listingsNote(area.projectCount)
        }
      : null;
  });

  renovationFinished = computed<CompareSide | null>(() => {
    const area = this.renovationHighlight();

    return area?.moveInMedianPricePerM2
      ? {
          label: $localize`:@@home.renovation.moveInReady:Move-in ready`,
          value: area.moveInMedianPricePerM2,
          valueText: `${this.euro(area.moveInMedianPricePerM2)}/m²`,
          note: this.listingsNote(area.moveInCount)
        }
      : null;
  });

  /**
   * The rest of the renovation card, pulled out as its own reads.
   *
   * The template used to reach these off the `@if (…; as area)` alias, which does not survive
   * two levels of nesting inside the block that declares it. Naming them here is clearer than
   * flattening the template around a scoping rule.
   */
  renovationPlace = computed(() => this.renovationHighlight()?.displayName ?? '');
  renovationEvidence = computed(() => this.renovationHighlight()?.renovationEvidence ?? null);
  renovationProjectAreaM2 = computed(() => this.renovationHighlight()?.projectMedianAreaM2 ?? null);

  /** The gap itself, worded for the chart's delta line. */
  renovationDelta = computed<string>(() => {
    const perM2 = this.renovationHighlight()?.renovationDiscountPerM2;

    return perM2 ? $localize`:@@home.renovation.delta:+${this.euro(perM2)}:amount:/m² finished` : '';
  });

  /** What the renovation gap is worth in money on that town's own typical project. */
  renovationUpside = computed<string | null>(() => {
    const area = this.renovationHighlight();

    if (!area?.renovationDiscountPerM2 || !area.projectMedianAreaM2) {
      return null;
    }

    return this.euro(area.renovationDiscountPerM2 * area.projectMedianAreaM2);
  });

  constructor(
    private readonly service: MarketAreaStatsService,
    private readonly overviewService: MarketOverviewService
  ) {}

  ngOnInit(): void {
    this.loadMarketStats();
    this.loadBudgetPreview();
    this.loadGapPreview();
  }

  // --- Formatting ---------------------------------------------------------------------
  // Public because the template reads them, and shared with the computeds above so a number
  // is written the same way wherever it appears.

  // Currency style rather than a hand-placed '€': en-GB writes €320,000, pt-PT 320 000 €.
  euro(value: number): string {
    return Number.isFinite(value)
      ? Math.round(value).toLocaleString(APP_LOCALE, { style: 'currency', currency: 'EUR', maximumFractionDigits: 0 })
      : '—';
  }

  formatCount(value: number): string {
    return Number.isFinite(value) ? value.toLocaleString(APP_LOCALE) : '—';
  }

  sqm(value: number): string {
    return Number.isFinite(value) ? Math.round(value).toLocaleString(APP_LOCALE) : '—';
  }

  formatKm(value: number): string {
    return Number.isFinite(value)
      ? `${value.toLocaleString(APP_LOCALE, { minimumFractionDigits: 1, maximumFractionDigits: 1 })} km`
      : '—';
  }

  listingsNote(count: number): string {
    return $localize`:@@home.listingsNote:${this.formatCount(count)}:count: listings`;
  }

  roundPercent(value: number): string {
    return Number.isFinite(value) ? Math.round(value).toString() : '—';
  }

  formatDate(iso: string): string {
    const date = new Date(iso);

    return Number.isNaN(date.getTime())
      ? '—'
      : date.toLocaleDateString(APP_LOCALE, { day: 'numeric', month: 'short', year: 'numeric' });
  }

  /** Mirrors the renovation screen's trustClass — same confidence, same badge. */
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
      municipalities: this.service.getLeaderboard({ level: 'Municipality', minListings: 1 }),
      // No filters = the whole country, the same call the Market overview screen makes with
      // nothing narrowed — gives the hero a real median price/€/m² and typology mix.
      overview: this.overviewService.getMarketOverview({})
    }).subscribe({
      next: ({ districts, towns, municipalities, overview }) => {
        this.districtRows.set(districts.items);
        this.townRows.set(towns.items);
        this.municipalityRows.set(municipalities.items);
        this.overview.set(overview);
        this.calculatedAtUtc.set(districts.calculatedAtUtc ?? towns.calculatedAtUtc ?? null);
        this.marketLoading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.districtRows.set([]);
        this.townRows.set([]);
        this.municipalityRows.set([]);
        this.overview.set(null);
        this.marketError.set(apiErrorMessage(err, $localize`:@@home.error.market:Could not load the market stats.`));
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

          this.budgetRows.set(rows);
          this.budgetLoading.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.budgetRows.set([]);
          this.budgetError.set(apiErrorMessage(err, $localize`:@@home.error.budget:Could not load a budget example.`));
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
          this.gapError.set(apiErrorMessage(err, $localize`:@@home.error.gap:Could not load a neighbour-gap example.`));
          this.gapLoading.set(false);
        }
      });
  }
}
