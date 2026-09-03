import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MarketAreaStatsService } from '../../core/services/market-area-stats.service';
import { AreaLevel, Confidence, MarketAreaStatsResponse } from '../../core/models/market-area-stats';
import { PageHeaderComponent } from '../../shared/page-header.component';
import { ExplainerComponent } from '../../shared/explainer.component';
import { PlaceNameComponent, placeLevelLabel, placeOwnName } from '../../shared/place-name.component';
import { AreaScope, AreaScopePickerComponent, emptyScope } from '../../shared/area-scope-picker.component';

// Every column except Cost, which you type in and is therefore the same on every row.
type SortColumn =
  | 'place'
  | 'trust'
  | 'discount'
  | 'margin'
  | 'onAFlat'
  | 'projects'
  | 'needsWork'
  | 'finished'
  | 'verdict';
type SortDirection = 'asc' | 'desc';

// A default renovation rate per m², well under the €1,000+/m² of a new build in build-cost.ts:
// a refurbishment reuses the structure. Deliberately editable — it is the one number on this
// screen we have not measured, and pretending otherwise is how an estimate turns into a fact.
const DEFAULT_RENOVATION_COST_PER_M2 = 650;

// Below this many projects a median is those adverts rather than that market. Matches the
// threshold the server uses to refuse a High confidence, so the badge and the mark agree.
const RELIABLE_PROJECTS = 10;

// Buy finished (0) → break-even (1) → worth renovating (2). The same two thresholds the
// verdict text uses, kept here so sorting on the column matches what it says.
function verdictRank(margin: number): number {
  if (margin > 100) {
    return 2;
  }

  return margin >= -100 ? 1 : 0;
}

// Low (0) → Medium (1) → High (2), so the column sorts as one scale.
function confidenceRank(confidence: Confidence | undefined): number {
  switch (confidence) {
    case 'High':
      return 2;

    case 'Medium':
      return 1;

    default:
      return 0;
  }
}

// Renovation upside — where the market pays you enough for taking the work on.
//
// One half is measured (what project stock actually sells for against finished stock, from real
// adverts) and the other half is estimated (what the work costs). They are kept in separate
// columns on purpose, never quietly subtracted into one score, because they are not the same
// kind of number.
//
// The trust column exists because the discount is one subtraction between two medians, and two
// medians always differ by something. Whether that something is a finding depends on how many
// adverts each side rests on and how far the two spreads sit apart — so the server works that
// out and every row carries its own verdict and the reason behind it.
@Component({
  selector: 'app-market-area-renovation',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    PageHeaderComponent,
    ExplainerComponent,
    PlaceNameComponent,
    AreaScopePickerComponent
  ],
  templateUrl: './market-area-renovation.component.html',
  styleUrl: './market-area-renovation.component.css'
})
export class MarketAreaRenovationComponent implements OnInit {
  readonly levels: AreaLevel[] = ['District', 'Municipality', 'Town'];

  // The dropdown reads in the same words the table does — "Town" on its own never said whether
  // it meant a freguesia or a município.
  levelName = placeLevelLabel;

  areas = signal<MarketAreaStatsResponse[]>([]);
  calculatedAtUtc = signal<string | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  level = signal<AreaLevel>('Municipality');
  minListings = signal(5);
  renovationCostPerM2 = signal(DEFAULT_RENOVATION_COST_PER_M2);

  // Narrowed to one distrito, and inside it one município. Empty = the whole country.
  scope = signal<AreaScope>(emptyScope());

  // Hide the rows whose discount the data cannot actually support. Off by default: a low-trust
  // row is still a lead, and hiding it silently is how a screen starts lying by omission.
  trustedOnly = signal(false);

  // Which row is opened to show the working behind its discount.
  expandedPlace = signal<string | null>(null);

  sortColumn = signal<SortColumn>('margin');
  sortDirection = signal<SortDirection>('desc');

  // Only places where both sides were measured can be ranked — a discount needs a project price
  // AND a finished price to be a discount at all.
  measuredAreas = computed(() => this.areas().filter(area => area.renovationDiscountPerM2 !== null));

  // What the table draws, after the trust filter.
  visibleAreas = computed(() =>
    this.trustedOnly()
      ? this.measuredAreas().filter(area => area.renovationEvidence?.confidence !== 'Low')
      : this.measuredAreas()
  );

  // How many rows the trust filter would remove, so the checkbox can say what it costs you.
  lowTrustCount = computed(
    () => this.measuredAreas().filter(area => area.renovationEvidence?.confidence === 'Low').length
  );

  sortedAreas = computed(() => {
    const rows = [...this.visibleAreas()];
    const column = this.sortColumn();
    const direction = this.sortDirection();

    rows.sort((a, b) => {
      let result = 0;

      switch (column) {
        case 'place':
          result = placeOwnName(a).localeCompare(placeOwnName(b), 'pt');
          break;

        case 'trust':
          result = confidenceRank(a.renovationEvidence?.confidence)
            - confidenceRank(b.renovationEvidence?.confidence);
          break;

        case 'discount':
          result = (a.renovationDiscountPerM2 ?? 0) - (b.renovationDiscountPerM2 ?? 0);
          break;

        case 'margin':
          result = this.margin(a) - this.margin(b);
          break;

        case 'onAFlat':
          result = this.marginOnATypicalFlat(a) - this.marginOnATypicalFlat(b);
          break;

        case 'projects':
          result = a.projectCount - b.projectCount;
          break;

        case 'needsWork':
          result = (a.projectMedianPricePerM2 ?? 0) - (b.projectMedianPricePerM2 ?? 0);
          break;

        case 'finished':
          result = (a.moveInMedianPricePerM2 ?? 0) - (b.moveInMedianPricePerM2 ?? 0);
          break;

        // Groups the three verdicts together rather than ordering on the raw margin, so
        // "Worth renovating" rows sit in one block instead of interleaving at the boundary.
        case 'verdict':
          result = verdictRank(this.margin(a)) - verdictRank(this.margin(b));
          break;
      }

      // Low-trust rows sort last whatever the column, unless trust IS the column being sorted.
      //
      // They are never hidden - that is what the trust filter is for, and it stays off by
      // default on purpose. But leading the table with "EUR 607,192 on a typical flat" drawn
      // from 4 of 79 projects, wearing the same badge as a 692-project row, tells the reader the
      // opposite of what the trust column says two columns to its right. Demoted, not removed.
      if (column !== 'trust') {
        const trustGap = confidenceRank(b.renovationEvidence?.confidence)
          - confidenceRank(a.renovationEvidence?.confidence);

        if (trustGap !== 0) {
          return trustGap;
        }
      }

      return direction === 'desc' ? -result : result;
    });

    return rows;
  });

  constructor(private readonly service: MarketAreaStatsService) {}

  ngOnInit(): void {
    this.load();
  }

  changeLevel(level: AreaLevel): void {
    this.level.set(level);
    this.load();
  }

  changeMinListings(minListings: number): void {
    this.minListings.set(Number(minListings));
    this.load();
  }

  changeScope(scope: AreaScope): void {
    this.scope.set(scope);
    this.load();
  }

  // Costs are applied here, not on the server, so changing the rate is instant.
  changeRenovationCost(costPerM2: number): void {
    this.renovationCostPerM2.set(Number(costPerM2));
  }

  toggleExpanded(place: string): void {
    this.expandedPlace.set(this.expandedPlace() === place ? null : place);
  }

  isExpanded(place: string): boolean {
    return this.expandedPlace() === place;
  }

  // --- The maths, spelled out ----------------------------------------------

  // What the market pays you per m² for the work, minus what the work costs.
  // Positive means the discount more than covers the renovation.
  margin(area: MarketAreaStatsResponse): number {
    return (area.renovationDiscountPerM2 ?? 0) - this.renovationCostPerM2();
  }

  // The size the project stock here actually comes in, so a rate can become a sum of money.
  // Falls back to the place's overall median when the project side has no size of its own.
  //
  // Used for BOTH sides of the profit on purpose, and it is not a bug: a flat does not grow when
  // you renovate it, so the finished value of THIS flat is the finished rate applied to the size
  // it already is.
  //
  // What is worth knowing is that the two RATES are measured on differently sized stock -
  // finished stock is larger in 143 of 194 places, 107 m2 against 88 m2 - and EUR/m2 falls as
  // flats get bigger. So the finished rate is measured a little low for a flat this size, which
  // understates the discount rather than flattering it. Fixing that means segmenting the rates
  // by size, not swapping the footprint here.
  typicalFlatM2(area: MarketAreaStatsResponse): number {
    return area.projectMedianAreaM2 ?? area.medianAreaM2;
  }

  // The margin as money, on a project of the size this place actually sells.
  // "€420/m² over cost" means nothing until it is "€38,000 on a typical 90 m² flat".
  marginOnATypicalFlat(area: MarketAreaStatsResponse): number {
    return this.margin(area) * this.typicalFlatM2(area);
  }

  // The three numbers of the worked example, each as a sum of money for one typical flat.
  buyPrice(area: MarketAreaStatsResponse): number {
    return (area.projectMedianPricePerM2 ?? 0) * this.typicalFlatM2(area);
  }

  workCost(area: MarketAreaStatsResponse): number {
    return this.renovationCostPerM2() * this.typicalFlatM2(area);
  }

  finishedValue(area: MarketAreaStatsResponse): number {
    return (area.moveInMedianPricePerM2 ?? 0) * this.typicalFlatM2(area);
  }

  // --- Verdict and trust ---------------------------------------------------

  // A plain reading of the margin. The two numbers behind it stay on screen either way.
  verdict(area: MarketAreaStatsResponse): string {
    const margin = this.margin(area);

    if (margin > 100) {
      return 'Worth renovating';
    }

    if (margin >= -100) {
      return 'Break-even';
    }

    return 'Buy finished';
  }

  verdictClass(area: MarketAreaStatsResponse): string {
    const margin = this.margin(area);

    if (margin > 100) {
      return 'tag-good';
    }

    return margin >= -100 ? 'tag-muted' : 'tag-bad';
  }

  // The badge class for the server's trust verdict, matching the shared badge styles.
  trustClass(area: MarketAreaStatsResponse): string {
    switch (area.renovationEvidence?.confidence) {
      case 'High':
        return 'badge-high';

      case 'Medium':
        return 'badge-medium';

      default:
        return 'badge-low';
    }
  }

  // Too few projects to lean on, even though a median was taken. Marked, not hidden.
  isThin(area: MarketAreaStatsResponse): boolean {
    return area.projectCount < RELIABLE_PROJECTS;
  }

  // How much of this place's stock got any verdict at all, project or finished. The rest carry
  // neither a condition nor a certificate, so the discount has no opinion about them.
  classifiedCount(area: MarketAreaStatsResponse): number {
    return area.projectCount + area.moveInCount;
  }

  // --- The spread bars ------------------------------------------------------
  // Two bars on one scale, so the overlap is something you see rather than a percentage you
  // have to picture. Everything is positioned against the widest range on the row.

  private spreadFloor(area: MarketAreaStatsResponse): number {
    return Math.min(area.projectP25PricePerM2 ?? 0, area.moveInP25PricePerM2 ?? 0);
  }

  private spreadCeiling(area: MarketAreaStatsResponse): number {
    return Math.max(area.projectP75PricePerM2 ?? 0, area.moveInP75PricePerM2 ?? 0);
  }

  hasSpread(area: MarketAreaStatsResponse): boolean {
    return (
      area.projectP25PricePerM2 !== null &&
      area.projectP75PricePerM2 !== null &&
      area.moveInP25PricePerM2 !== null &&
      area.moveInP75PricePerM2 !== null &&
      this.spreadCeiling(area) > this.spreadFloor(area)
    );
  }

  // Where one side's middle half starts, as a percentage across the row.
  spreadOffset(area: MarketAreaStatsResponse, from: number | null): string {
    const floor = this.spreadFloor(area);
    const span = this.spreadCeiling(area) - floor;

    return span <= 0 ? '0%' : `${(((from ?? floor) - floor) / span) * 100}%`;
  }

  // How wide it is, on the same scale.
  spreadWidth(area: MarketAreaStatsResponse, from: number | null, to: number | null): string {
    const floor = this.spreadFloor(area);
    const span = this.spreadCeiling(area) - floor;

    return span <= 0 ? '0%' : `${(((to ?? floor) - (from ?? floor)) / span) * 100}%`;
  }

  toggleSort(column: SortColumn): void {
    if (this.sortColumn() === column) {
      this.sortDirection.set(this.sortDirection() === 'asc' ? 'desc' : 'asc');

      return;
    }

    this.sortColumn.set(column);
    this.sortDirection.set(column === 'place' ? 'asc' : 'desc');
  }

  arrow(column: SortColumn): string {
    if (this.sortColumn() !== column) {
      return '';
    }

    return this.sortDirection() === 'asc' ? ' ▲' : ' ▼';
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.expandedPlace.set(null);

    // Same read as the leaderboard: the renovation numbers ride on the stats row, so this needs
    // no endpoint of its own.
    this.service
      .getLeaderboard({
        level: this.level(),
        minListings: this.minListings(),
        district: this.scope().district || undefined,
        municipality: this.scope().municipality || undefined
      })
      .subscribe({
        next: response => {
          this.areas.set(response.items);
          this.calculatedAtUtc.set(response.calculatedAtUtc);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Could not load the renovation numbers. Check the API is running.');
          this.loading.set(false);
        }
      });
  }
}
