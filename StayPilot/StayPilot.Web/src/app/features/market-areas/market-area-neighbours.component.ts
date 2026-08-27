import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { MarketAreaStatsService } from '../../core/services/market-area-stats.service';
import {
  AreaLevel,
  MarketAreaNeighbourGapResponse,
  NeighbourGapPlace,
  NeighbourGapResponse
} from '../../core/models/market-area-stats';
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

// The two questions this screen answers. Which one you are asking used to be inferred from
// whether a place was picked in a dropdown, which is not something a reader infers.
type NeighbourMode = 'pairs' | 'around';

// Columns of the anchored table.
type NeighbourSort = 'place' | 'listings' | 'pricePerM2' | 'gap' | 'distance' | 'perM2';
// Columns of the all-pairs table.
type PairSort = 'expensive' | 'expensivePrice' | 'cheaper' | 'cheaperPrice' | 'distance' | 'saving' | 'gap';
type SortDirection = 'asc' | 'desc';

// The gap floor to drop to when a place is anchored. 1 is the API minimum, and "everything
// around Lisbon" means everything — hiding the similarly-priced neighbours is what made this
// screen read as a list of unrelated pairs.
const ANCHORED_MIN_GAP = 1;

// The league table's floor. Every pair 1% apart, across a whole level, is thousands of rows of
// noise, so the biggest-gaps view starts where a gap is worth the drive.
const LEAGUE_MIN_GAP = 20;

// How many pairs the league table draws before it stops. Freguesias across the whole country come
// to over four thousand pairs, and a table that long is thousands of rows nobody scrolls past the
// top of — while costing every one of them in DOM. Trimmed with a button, never silently.
const PAIR_PREVIEW_ROWS = 25;

// How long to wait after the last keystroke in a threshold box before asking the API. Typing "125"
// is three separate changes, and at freguesia grain each one is a multi-megabyte answer to a number
// the reader is halfway through typing.
const TYPING_PAUSE_MS = 400;

// What the pairs half of the response looks like when we deliberately did not ask for it —
// around-one-place before a place is picked. The place list still loads; the pairs would be
// every pair in the country, fetched to show none of them.
const NO_PAIRS_ASKED_FOR: MarketAreaNeighbourGapResponse = {
  items: [],
  comparedOn: null,
  calculatedAtUtc: null
};

// Which grains can be compared inside a given scope: the ones strictly finer than the scope
// itself. "Municípios inside Loulé" is one place, Loulé, and one place makes no pairs — the table
// came back empty and looked like missing data. Ordered coarsest first, so the head of the list is
// the natural grain to fall back to when a scope makes the current one impossible.
function levelsInside(scope: AreaScope): AreaLevel[] {
  if (scope.municipality) {
    return ['Town'];
  }

  return scope.district ? ['Municipality', 'Town'] : ['District', 'Municipality', 'Town'];
}

// Joins the two halves of a pair into one key. Any character neither place name can contain.
const PAIR_KEY_SEPARATOR = ' → ';

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
  readonly typologies = TYPOLOGIES;

  // Only the grains that can actually be paired inside the current scope.
  levels = computed(() => levelsInside(this.scope()));

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

  // Which of the two questions is on screen. A control of its own, because it decides which
  // table you get — and "leave the dropdown on every pair" was a rule you had to be told.
  mode = signal<NeighbourMode>('pairs');

  // The place everything is measured against. Empty in around-one-place mode until one is picked;
  // always empty in biggest-gaps mode.
  anchor = signal('');

  // Around one place, before a place has been picked. The screen then asks for one rather than
  // showing the league table it is not in.
  awaitingAnchor = computed(() => this.mode() === 'around' && !this.anchor());

  // Every place at this level, for the anchor picker. Comes from the leaderboard rather than
  // from the pairs: a place with no qualifying pair still has to be pickable, or you cannot
  // ask "what is around Lisbon" until Lisbon already has a big gap somewhere.
  places = signal<string[]>([]);
  private pricePerM2ByPlace = signal<Map<string, number>>(new Map());

  // Typical flat size per place, off the same leaderboard. A gap in euros per square metre is
  // the honest way to compare two places and a useless way to picture the decision, so the
  // opened row multiplies it out by the size that place actually sells.
  private areaM2ByPlace = signal<Map<string, number>>(new Map());

  // The one open row. Both tables are mutually exclusive, so one signal serves both.
  expandedKey = signal<string | null>(null);

  // The league table draws its first page until asked for the rest.
  showAllPairs = signal(false);

  // What is currently loaded, as the key of the request that fetched it. The anchor is not part of
  // it: the pairs cover the whole level either way, so picking a different place only changes which
  // of the rows already here are shown. Empty when nothing usable is loaded.
  private loadedKey = signal('');

  // The same, for the place list, which moves with fewer things than the pairs do.
  private loadedPlacesKey = signal('');

  // Two loads can overlap — a heavy freguesia set and a quick município one — and the slow answer
  // must not repaint the screen after the fast one. Only the newest request is allowed to land.
  private newestRequest = 0;

  // Set while a threshold is being typed, so the request waits for the typing to stop.
  private pendingReload?: ReturnType<typeof setTimeout>;

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

  // What the league table actually renders: the first page until the rest is asked for.
  visiblePairs = computed(() =>
    this.showAllPairs() ? this.sortedGaps() : this.sortedGaps().slice(0, PAIR_PREVIEW_ROWS)
  );

  hiddenPairCount = computed(() => Math.max(0, this.sortedGaps().length - this.visiblePairs().length));

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
    this.loadWhenTypingStops();
  }

  changeMaxDistance(maxDistanceKm: number): void {
    this.maxDistanceKm.set(Number(maxDistanceKm));
    this.loadWhenTypingStops();
  }

  changeMinGap(minGapPercent: number): void {
    this.minGapPercent.set(Number(minGapPercent));
    this.loadWhenTypingStops();
  }

  changeScope(scope: AreaScope): void {
    this.scope.set(scope);

    // Narrowing to a município leaves no municípios to pair inside it, so the grain has to follow
    // the scope down. Coarsest still possible, which is the smallest step from what was asked for.
    const allowed = levelsInside(scope);

    if (!allowed.includes(this.level())) {
      this.level.set(allowed[0]);
    }

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
    this.loadWhenTypingStops();
  }

  // Biggest gaps: the league table for this level, at the floor that keeps it readable.
  showEveryPair(): void {
    if (this.mode() === 'pairs') {
      return;
    }

    this.mode.set('pairs');
    this.anchor.set('');
    this.minGapPercent.set(LEAGUE_MIN_GAP);

    // Coming back to a league table that was never invalidated: what is loaded is what this asks
    // for, so flipping between the two questions costs nothing after the first look at each.
    if (this.loadedKey() !== this.requestKey()) {
      this.load();
    }
  }

  // Around one place: every neighbour, including the ones priced much the same, because "what is
  // around Lisbon" means everything around it. Nothing is fetched until a place is picked.
  showAroundOnePlace(): void {
    if (this.mode() === 'around') {
      return;
    }

    this.mode.set('around');
    this.minGapPercent.set(ANCHORED_MIN_GAP);

    // The league table's pairs stay in hand — nothing shows them while a place is being asked for,
    // and they are what the screen goes back to if the reader flips straight back.
  }

  changeAnchor(place: string): void {
    this.anchor.set(place);

    if (!place) {
      // Back to asking for a place. What is loaded stays loaded — it is the whole level, so it will
      // answer whichever place is picked next.
      return;
    }

    // The pairs on hand already cover this level, and this place's neighbours are among them.
    // Re-asking would be megabytes to arrive at the same rows.
    if (this.loadedKey() !== this.requestKey()) {
      this.load();
    }
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

  // Everything the pairs and the place list depend on. Deliberately not the anchor.
  private requestKey(): string {
    return [
      this.level(),
      this.scope().district,
      this.scope().municipality,
      this.typology(),
      this.minListings(),
      this.maxDistanceKm(),
      this.minGapPercent(),
      this.minTypologyListings()
    ].join('|');
  }

  // The place list follows the grain, the scope and the listing floor — not the distance or the
  // gap. Re-reading every freguesia in the country because a gap went from 20% to 25% is 229 kB of
  // work nobody asked for.
  private placesKey(): string {
    return [this.level(), this.scope().district, this.scope().municipality, this.minListings()].join('|');
  }

  private loadWhenTypingStops(): void {
    clearTimeout(this.pendingReload);
    this.pendingReload = setTimeout(() => this.load(), TYPING_PAUSE_MS);
  }

  private load(): void {
    clearTimeout(this.pendingReload);
    this.loading.set(true);
    this.error.set(null);
    // Whatever was open described the old numbers.
    this.expandedKey.set(null);
    // A new set of pairs is a new first page — carrying "show all" across would open four thousand
    // rows because a six-row slice was expanded a moment ago.
    this.showAllPairs.set(false);

    const key = this.requestKey();
    const placesKey = this.placesKey();
    const askingForPairs = !this.awaitingAnchor();
    const askingForPlaces = this.loadedPlacesKey() !== placesKey;
    const request = ++this.newestRequest;

    // The pairs answer the table; the leaderboard fills the place picker and gives the anchor its
    // own €/m². Both are keyed on the same level + minListings, so they go together.
    //
    // Except while a place is being asked for: the pairs would then be every pair at this level —
    // megabytes of them at freguesia grain — fetched to display none.
    const gaps = this.awaitingAnchor()
      ? of(NO_PAIRS_ASKED_FOR)
      : this.service.getNeighbourGaps({
          level: this.level(),
          minListings: this.minListings(),
          maxDistanceKm: this.maxDistanceKm(),
          minGapPercent: this.minGapPercent(),
          district: this.scope().district || undefined,
          municipality: this.scope().municipality || undefined,
          typology: this.typology() || undefined,
          // Only means anything alongside a typology, and its input is hidden without one.
          minTypologyListings: this.typology() ? this.minTypologyListings() : undefined
        });

    const leaderboard = askingForPlaces
      ? this.service.getLeaderboard({
          level: this.level(),
          minListings: this.minListings(),
          district: this.scope().district || undefined,
          municipality: this.scope().municipality || undefined
        })
      : of(null);

    forkJoin({
      gaps,
      leaderboard
    }).subscribe({
      next: ({ gaps, leaderboard }) => {
        // A slower earlier request answering after a newer one would repaint the screen with numbers
        // the controls no longer describe.
        if (request !== this.newestRequest) {
          return;
        }

        this.loadedKey.set(askingForPairs ? key : '');
        this.gaps.set(gaps.items);
        this.comparedOn.set(gaps.comparedOn);
        this.calculatedAtUtc.set(gaps.calculatedAtUtc);

        if (leaderboard) {
          this.loadedPlacesKey.set(placesKey);

          this.places.set(
            leaderboard.items.map(item => item.displayName).sort((a, b) => a.localeCompare(b, 'pt'))
          );
          this.pricePerM2ByPlace.set(
            new Map(leaderboard.items.map(item => [item.displayName, item.medianPricePerM2]))
          );
          this.areaM2ByPlace.set(
            new Map(leaderboard.items.map(item => [item.displayName, item.medianAreaM2]))
          );
        }

        this.loading.set(false);
      },
      error: () => {
        if (request !== this.newestRequest) {
          return;
        }

        this.loadedKey.set('');
        this.loadedPlacesKey.set('');
        this.error.set('Could not load the neighbour gaps. Check the API is running.');
        this.loading.set(false);
      }
    });
  }

  // --- The opened row ------------------------------------------------------

  // A pair is identified by its two halves, the same key the table tracks on.
  pairKey(gap: NeighbourGapResponse): string {
    return gap.expensive.displayName + PAIR_KEY_SEPARATOR + gap.cheaper.displayName;
  }

  // Clicking the open row again closes it: a row that only opens leaves you hunting for the
  // way back out. Same rule as What money buys.
  toggleExpanded(key: string): void {
    this.expandedKey.set(this.expandedKey() === key ? null : key);
  }

  isExpanded(key: string): boolean {
    return this.expandedKey() === key;
  }

  // The panel spans the whole table, so it has to know how wide the table currently is —
  // the all-stock columns come and go with the Compare control.
  pairColspan = computed(() => (this.comparedOn() ? 10 : 8));
  anchorColspan = computed(() => (this.comparedOn() ? 8 : 7));

  // The size of flat a place actually sells. Null when the leaderboard has no median for it,
  // in which case the money line is left out rather than guessed at.
  typicalAreaM2(place: NeighbourGapPlace): number | null {
    return this.areaM2ByPlace().get(place.displayName) ?? null;
  }

  // What the gap comes to on one flat of the size the cheaper place sells — that is the flat
  // you would actually be buying, so it is the one the saving belongs to.
  savingOnATypicalFlat(gap: NeighbourGapResponse): number | null {
    const area = this.typicalAreaM2(gap.cheaper);

    return area === null ? null : this.saving(gap) * area;
  }

  // The same two places compared on all their stock. When a typology is chosen this is the
  // number that says whether the gap is about the places or about what they happen to sell.
  allStockGapPercent(gap: NeighbourGapResponse): number | null {
    const dear = gap.expensive.allStockPricePerM2;
    const cheap = gap.cheaper.allStockPricePerM2;

    if (!dear || !cheap) {
      return null;
    }

    // Signed against the pair as the typology ordered it: negative keeps meaning "the place in
    // the Pay less in column is the cheaper one on all stock too".
    return ((cheap - dear) / dear) * 100;
  }

  // A neighbour of the anchor, as money on one flat of that neighbour’s typical size.
  neighbourDifferenceOnATypicalFlat(n: NeighbourView): number | null {
    const area = this.typicalAreaM2(n.place);

    return area === null ? null : n.differencePerM2 * area;
  }
}
