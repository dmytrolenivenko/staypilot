import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { MarketAreaStatsService } from '../../core/services/market-area-stats.service';
import { AreaLevel, NeighbourGapPlace, NeighbourGapResponse } from '../../core/models/market-area-stats';
import { TYPOLOGIES, Typology } from '../../core/models/enums';
import { PageHeaderComponent } from '../../shared/page-header.component';
import { ExplainerComponent } from '../../shared/explainer.component';
import { PlaceNameComponent, placeLevelLabel, placeOwnName } from '../../shared/place-name.component';
import { AreaScope, AreaScopePickerComponent, emptyScope } from '../../shared/area-scope-picker.component';

// One neighbour, read from the anchor place's point of view. The API answers in pairs
// ("dear place → cheaper place"), which is the right shape for a league table and the wrong
// shape for "what is around Lisbon" — so a pair gets flipped into one of these.
interface NeighbourView {
  place: NeighbourGapPlace;
  pricePerM2: number;
  listingCount: number;
  distanceKm: number;
  // Always the magnitude; `cheaper` carries the direction.
  gapPercent: number;
  // True when this neighbour is cheaper than the anchor.
  cheaper: boolean;
  // € per m² saved (or paid extra) against the anchor.
  differencePerM2: number;
}

// Columns of the anchored table.
type NeighbourSort = 'place' | 'listings' | 'pricePerM2' | 'gap' | 'distance' | 'perM2';
// Columns of the all-pairs table.
type PairSort = 'expensive' | 'expensivePrice' | 'cheaper' | 'cheaperPrice' | 'distance' | 'saving' | 'gap';
type SortDirection = 'asc' | 'desc';

// The gap floor to drop to when a place is anchored. 1 is the API minimum, and "everything
// around Lisbon" means everything — hiding the similarly-priced neighbours is what made this
// screen read as a list of unrelated pairs.
const ANCHORED_MIN_GAP = 1;

// Neighbour gaps — two questions, one screen.
//
// Anchored on a place: what is around it, nearest first, and how each neighbour is priced
// against it. Unanchored: every pair at this level, biggest gap first.
//
// Pairing happens on the server (it is pairwise work over the whole level); the flip to an
// anchor's point of view happens here, because it is presentation.
@Component({
  selector: 'app-market-area-neighbours',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    PageHeaderComponent,
    ExplainerComponent,
    PlaceNameComponent,
    AreaScopePickerComponent
  ],
  templateUrl: './market-area-neighbours.component.html',
  styleUrl: './market-area-neighbours.component.css'
})
export class MarketAreaNeighboursComponent implements OnInit {
  readonly levels: AreaLevel[] = ['District', 'Municipality', 'Town'];
  readonly typologies = TYPOLOGIES;

  // The dropdown reads in the same words the table does — "Town" on its own never said whether
  // it meant a freguesia or a município.
  levelName = placeLevelLabel;

  gaps = signal<NeighbourGapResponse[]>([]);
  calculatedAtUtc = signal<string | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  level = signal<AreaLevel>('Municipality');
  minListings = signal(5);
  maxDistanceKm = signal(25);
  minGapPercent = signal(20);

  // Narrowed to one distrito, and inside it one município. Empty = the whole country.
  scope = signal<AreaScope>(emptyScope());

  // Compare like with like. '' compares all stock at once, which is the default and the loosest
  // reading — two places can differ 30% on all stock purely because one sells villas.
  typology = signal<Typology | ''>('');

  // The fewest listings of that typology a place needs before it can be half of a pair.
  minTypologyListings = signal(5);

  // What the pairs were actually compared on, as the API answered. Read off the response rather
  // than off the control, so the caption can never claim a basis the numbers were not built on.
  comparedOn = signal<Typology | null>(null);

  // The place everything is measured against. '' = show every pair instead.
  anchor = signal('');

  // Every place at this level, for the anchor picker. Comes from the leaderboard rather than
  // from the pairs: a place with no qualifying pair still has to be pickable, or you cannot
  // ask "what is around Lisbon" until Lisbon already has a big gap somewhere.
  places = signal<string[]>([]);
  private pricePerM2ByPlace = signal<Map<string, number>>(new Map());

  // The anchor's own €/m², shown as the reference every row is compared to.
  //
  // Taken off the pairs first, because those carry whatever basis the comparison ran on: with a
  // typology chosen, the leaderboard's all-stock median is the wrong number to print next to a
  // column of T2-against-T2 gaps. Falls back to the leaderboard when the anchor has no pair.
  anchorPricePerM2 = computed(() => {
    const anchor = this.anchor();

    for (const gap of this.gaps()) {
      if (gap.expensive.displayName === anchor) {
        return gap.expensive.medianPricePerM2;
      }

      if (gap.cheaper.displayName === anchor) {
        return gap.cheaper.medianPricePerM2;
      }
    }

    return this.pricePerM2ByPlace().get(anchor) ?? null;
  });

  anchorSort = signal<NeighbourSort>('distance');
  anchorDirection = signal<SortDirection>('asc');

  pairSort = signal<PairSort>('gap');
  pairDirection = signal<SortDirection>('desc');

  // The anchored view: every pair that involves the anchor, flipped to face it.
  neighbours = computed<NeighbourView[]>(() => {
    const anchor = this.anchor();

    if (!anchor) {
      return [];
    }

    const rows = this.gaps()
      .filter(gap => gap.expensive.displayName === anchor || gap.cheaper.displayName === anchor)
      .map(gap => {
        const anchorIsExpensive = gap.expensive.displayName === anchor;
        const other = anchorIsExpensive ? gap.cheaper : gap.expensive;

        return {
          place: other,
          pricePerM2: other.medianPricePerM2,
          listingCount: other.listingCount,
          distanceKm: gap.distanceKm,
          gapPercent: gap.gapPercent,
          cheaper: anchorIsExpensive,
          differencePerM2: Math.abs(gap.expensive.medianPricePerM2 - gap.cheaper.medianPricePerM2)
        };
      });

    const column = this.anchorSort();
    const direction = this.anchorDirection();

    rows.sort((a, b) => {
      let result = 0;

      switch (column) {
        case 'place':
          result = placeOwnName(a.place).localeCompare(placeOwnName(b.place), 'pt');
          break;

        case 'listings':
          result = a.listingCount - b.listingCount;
          break;

        case 'pricePerM2':
          result = a.pricePerM2 - b.pricePerM2;
          break;

        // Cheaper neighbours sort below dearer ones at the same magnitude, so the column
        // reads as one scale from "much dearer" to "much cheaper" rather than two.
        case 'gap':
          result = a.gapPercent * (a.cheaper ? -1 : 1) - b.gapPercent * (b.cheaper ? -1 : 1);
          break;

        case 'distance':
          result = a.distanceKm - b.distanceKm;
          break;

        // Same signed scale as the gap column: money saved sorts opposite money paid.
        case 'perM2':
          result =
            a.differencePerM2 * (a.cheaper ? -1 : 1) - b.differencePerM2 * (b.cheaper ? -1 : 1);
          break;
      }

      return direction === 'desc' ? -result : result;
    });

    return rows;
  });

  // The unanchored view: every pair, sorted by whichever column was clicked.
  sortedGaps = computed(() => {
    const rows = [...this.gaps()];
    const column = this.pairSort();
    const direction = this.pairDirection();

    rows.sort((a, b) => {
      let result = 0;

      switch (column) {
        case 'expensive':
          result = placeOwnName(a.expensive).localeCompare(placeOwnName(b.expensive), 'pt');
          break;

        case 'expensivePrice':
          result = a.expensive.medianPricePerM2 - b.expensive.medianPricePerM2;
          break;

        case 'cheaper':
          result = placeOwnName(a.cheaper).localeCompare(placeOwnName(b.cheaper), 'pt');
          break;

        case 'cheaperPrice':
          result = a.cheaper.medianPricePerM2 - b.cheaper.medianPricePerM2;
          break;

        case 'distance':
          result = a.distanceKm - b.distanceKm;
          break;

        case 'saving':
          result = this.saving(a) - this.saving(b);
          break;

        case 'gap':
          result = a.gapPercent - b.gapPercent;
          break;
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
    // Place names are level-specific — an anchor picked at Town level is not a Município.
    this.anchor.set('');
    this.load();
  }

  changeMinListings(minListings: number): void {
    this.minListings.set(Number(minListings));
    this.load();
  }

  changeMaxDistance(maxDistanceKm: number): void {
    this.maxDistanceKm.set(Number(maxDistanceKm));
    this.load();
  }

  changeMinGap(minGapPercent: number): void {
    this.minGapPercent.set(Number(minGapPercent));
    this.load();
  }

  changeScope(scope: AreaScope): void {
    this.scope.set(scope);
    // The anchor is a place inside the old scope, and may not exist inside the new one.
    this.anchor.set('');
    this.load();
  }

  changeTypology(typology: Typology | ''): void {
    this.typology.set(typology);
    this.load();
  }

  changeMinTypologyListings(minTypologyListings: number): void {
    this.minTypologyListings.set(Number(minTypologyListings));
    this.load();
  }

  // Picking a place also drops the gap floor: "show me everything around Lisbon" means
  // everything, including the neighbours priced much the same. Clearing it puts the league-table
  // default back, because a list of every pair 1% apart is noise.
  changeAnchor(place: string): void {
    this.anchor.set(place);
    this.minGapPercent.set(place ? ANCHORED_MIN_GAP : 20);
    this.load();
  }

  toggleAnchorSort(column: NeighbourSort): void {
    if (this.anchorSort() === column) {
      this.anchorDirection.set(this.anchorDirection() === 'asc' ? 'desc' : 'asc');

      return;
    }

    this.anchorSort.set(column);
    // Nearest and A-to-Z read up; the money columns start at their interesting end.
    this.anchorDirection.set(column === 'distance' || column === 'place' ? 'asc' : 'desc');
  }

  anchorArrow(column: NeighbourSort): string {
    if (this.anchorSort() !== column) {
      return '';
    }

    return this.anchorDirection() === 'asc' ? ' ▲' : ' ▼';
  }

  togglePairSort(column: PairSort): void {
    if (this.pairSort() === column) {
      this.pairDirection.set(this.pairDirection() === 'asc' ? 'desc' : 'asc');

      return;
    }

    this.pairSort.set(column);
    this.pairDirection.set(column === 'expensive' || column === 'cheaper' ? 'asc' : 'desc');
  }

  pairArrow(column: PairSort): string {
    if (this.pairSort() !== column) {
      return '';
    }

    return this.pairDirection() === 'asc' ? ' ▲' : ' ▼';
  }

  // What you would save per m² by moving to the cheaper side.
  saving(gap: NeighbourGapResponse): number {
    return gap.expensive.medianPricePerM2 - gap.cheaper.medianPricePerM2;
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);

    // The pairs answer the table; the leaderboard fills the place picker and gives the anchor
    // its own €/m². Both are keyed on the same level + minListings, so they go together.
    forkJoin({
      gaps: this.service.getNeighbourGaps({
        level: this.level(),
        minListings: this.minListings(),
        maxDistanceKm: this.maxDistanceKm(),
        minGapPercent: this.minGapPercent(),
        district: this.scope().district || undefined,
        municipality: this.scope().municipality || undefined,
        typology: this.typology() || undefined,
        minTypologyListings: this.minTypologyListings()
      }),
      leaderboard: this.service.getLeaderboard({
        level: this.level(),
        minListings: this.minListings(),
        district: this.scope().district || undefined,
        municipality: this.scope().municipality || undefined
      })
    }).subscribe({
      next: ({ gaps, leaderboard }) => {
        this.gaps.set(gaps.items);
        this.comparedOn.set(gaps.comparedOn);
        this.calculatedAtUtc.set(gaps.calculatedAtUtc);

        this.places.set(
          leaderboard.items.map(item => item.displayName).sort((a, b) => a.localeCompare(b, 'pt'))
        );
        this.pricePerM2ByPlace.set(
          new Map(leaderboard.items.map(item => [item.displayName, item.medianPricePerM2]))
        );

        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load the neighbour gaps. Check the API is running.');
        this.loading.set(false);
      }
    });
  }
}
