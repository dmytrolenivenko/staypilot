import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AreaLevel } from '../../core/models/market-area-stats';
import { PageHeaderComponent } from '../../shared/page-header.component';
import { SegmentedComponent, SegmentedOption } from '../../shared/segmented.component';
import { placeLevelLabel } from '../../shared/place-name.component';
import {
  AreaScope,
  AreaScopePickerComponent,
  emptyScope,
  levelsInside
} from '../../shared/area-scope-picker.component';
import { MarketAreaLeaderboardComponent } from '../market-areas/market-area-leaderboard.component';
import { MarketAreaBudgetComponent } from '../market-areas/market-area-budget.component';
import { MarketAreaNeighboursComponent } from '../market-areas/market-area-neighbours.component';
import { MarketAreaRenovationComponent } from '../market-areas/market-area-renovation.component';

/** Which question the screen is answering. Also the `?ask=` value, so a view can be linked to. */
export type PlacesLens = 'value' | 'budget' | 'neighbours' | 'renovation';

const LENSES: SegmentedOption<PlacesLens>[] = [
  // Labels share the footer's ids (nav-groups.ts) so the two can never disagree.
  { value: 'value', label: $localize`:@@core.nav.ask.value:Where value sits`, hint: $localize`:@@hub.places.value.hint:Ranked on median €/m²` },
  { value: 'budget', label: $localize`:@@core.nav.ask.budget:What money buys`, hint: $localize`:@@hub.places.budget.hint:The most rooms a budget reaches` },
  { value: 'neighbours', label: $localize`:@@core.nav.ask.neighbours:Neighbour gaps`, hint: $localize`:@@hub.places.neighbours.hint:Nearby places priced far apart` },
  { value: 'renovation', label: $localize`:@@core.nav.ask.renovation:Renovation upside`, hint: $localize`:@@hub.places.renovation.hint:Where a fixer-upper pays` }
];

/** Lens → the one line under the title. The question in plain words, not the screen's name. */
const LENS_SUBS: Record<PlacesLens, string> = {
  value:
    $localize`:@@hub.places.value.sub:Places ranked on the middle price per square metre — not the middle asking price, so a town full of small flats cannot look cheap just for being small.`,
  budget:
    $localize`:@@hub.places.budget.sub:The other way round from every property portal: put in what you have, and see what it actually gets you in each place.`,
  neighbours:
    $localize`:@@hub.places.neighbours.sub:Where the biggest price borders are, or what one place looks like against everything near it.`,
  renovation:
    $localize`:@@hub.places.renovation.sub:Where the market pays you enough for taking the work on. The other three find value; this one says where to create it.`
};

/**
 * Places — one table of the country, asked four different questions.
 *
 * These were four screens: Leaderboard, What money buys, Neighbour gaps and Renovation upside.
 * Three of them called the same endpoint and the fourth called it with the typology children
 * attached, and every one of them carried its own copy of the same three controls. So picking
 * "Faro, município, min 5" and then wondering what a budget reached there meant navigating away
 * and setting the same three things again, on a screen that looked unrelated.
 *
 * The filters live here now and the question is a control rather than a destination. Each lens
 * keeps whatever is genuinely its own — the budget and its stretch, the gap radius, the
 * renovation rate — and receives place, grain and sample gate from above.
 *
 * The grain is kept legal for the scope here too (see levelsInside): narrowing to a município
 * leaves no municípios inside it to rank, which used to be a rule only one of the four knew.
 */
@Component({
  selector: 'app-places',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    PageHeaderComponent,
    SegmentedComponent,
    AreaScopePickerComponent,
    MarketAreaLeaderboardComponent,
    MarketAreaBudgetComponent,
    MarketAreaNeighboursComponent,
    MarketAreaRenovationComponent
  ],
  templateUrl: './places.component.html'
})
export class PlacesComponent implements OnInit {
  readonly lenses = LENSES;

  levelName = placeLevelLabel;

  lens = signal<PlacesLens>('value');

  // Municipality by default: districts are too broad to act on, and most freguesias do not have
  // enough listings to trust. The same default all four screens started from.
  level = signal<AreaLevel>('Municipality');

  // The sample gate. Five, not fifteen: fifteen kept the cheapest towns in the country off the
  // board entirely. Single-advert places are still excluded — their "median" is one price.
  minListings = signal(5);

  scope = signal<AreaScope>(emptyScope());

  // Only the grains that can actually be ranked inside the current scope.
  levels = computed(() => levelsInside(this.scope()));

  sub = computed(() => LENS_SUBS[this.lens()]);

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    // The lens is in the URL so a particular view can be linked to and survives a reload —
    // and so the old four routes can redirect straight to the question they used to be.
    const asked = this.route.snapshot.queryParamMap.get('ask') as PlacesLens | null;

    if (asked && LENSES.some(lens => lens.value === asked)) {
      this.lens.set(asked);
    }
  }

  pickLens(lens: PlacesLens): void {
    this.lens.set(lens);

    // Replace rather than push: flipping between the four questions is one visit, and pushing
    // would make Back walk through every lens you tried before leaving the screen.
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { ask: lens },
      replaceUrl: true
    });
  }

  changeLevel(level: AreaLevel): void {
    this.level.set(level);
  }

  changeMinListings(minListings: number): void {
    this.minListings.set(Number(minListings));
  }

  changeScope(scope: AreaScope): void {
    this.scope.set(scope);

    // Narrowing to a município leaves no municípios to rank inside it, so the grain follows the
    // scope down. Coarsest still possible, which is the smallest step from what was asked for.
    const allowed = levelsInside(scope);

    if (!allowed.includes(this.level())) {
      this.level.set(allowed[0]);
    }
  }
}
