import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MarketAreaStatsService } from '../../core/services/market-area-stats.service';
import { AreaLevel, MarketAreaStatsResponse } from '../../core/models/market-area-stats';
import { PageHeaderComponent } from '../../shared/page-header.component';
import { ExplainerComponent } from '../../shared/explainer.component';
import { PlaceNameComponent, placeLevelLabel, placeOwnName } from '../../shared/place-name.component';
import { AreaScope, AreaScopePickerComponent, emptyScope } from '../../shared/area-scope-picker.component';

// The columns you can sort by. Client-side only — the API never sees these.
type SortColumn = 'place' | 'listings' | 'pricePerM2' | 'area' | 'deals';
type SortDirection = 'asc' | 'desc';

// What share of a place's listings are asking below the model's estimate, 0-100.
// A share rather than a count: 40 deals out of 2,000 is a worse hunting ground than 8 out of 40.
function dealShare(area: MarketAreaStatsResponse): number {
  return area.listingCount === 0 ? 0 : (area.belowEstimateCount / area.listingCount) * 100;
}

// Below this many listings a median is worth reading with suspicion, so the row is marked.
// Not hidden: hiding them is what made the cheapest place on the board (Beja, 1,937) far from
// the cheapest place in the data (Póvoa de São Miguel, 419) — true, just thinly evidenced.
const RELIABLE_LISTINGS = 15;

// Leaderboard — places ranked on the middle price per m².
//
// The API hands over every place at the chosen level in one go (a few hundred rows at most),
// so sorting is instant and costs no request. Only Level and Min listings go back to the
// server, because those change which rows exist rather than their order.
@Component({
  selector: 'app-market-area-leaderboard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    PageHeaderComponent,
    ExplainerComponent,
    PlaceNameComponent,
    AreaScopePickerComponent
  ],
  templateUrl: './market-area-leaderboard.component.html',
  styleUrl: './market-area-leaderboard.component.css'
})
export class MarketAreaLeaderboardComponent implements OnInit {
  readonly levels: AreaLevel[] = ['District', 'Municipality', 'Town'];

  // The dropdown reads in the same words the table does — "Town" on its own never said whether
  // it meant a freguesia or a município.
  levelName = placeLevelLabel;

  areas = signal<MarketAreaStatsResponse[]>([]);
  calculatedAtUtc = signal<string | null>(null);
  loading = signal(true);
  recalculating = signal(false);
  error = signal<string | null>(null);

  // Municipality by default: districts are too broad to act on, and most towns do not have
  // enough listings to trust.
  level = signal<AreaLevel>('Municipality');

  // The sample gate. Five, not fifteen: fifteen kept the cheapest towns in the country off the
  // board entirely. Single-advert places are still excluded — their "median" is one price.
  minListings = signal(5);

  // Narrowed to one distrito, and inside it one municipio. Empty = the whole country. A national
  // board answers "where is cheapest in Portugal", which you ask once; scoped, it answers "where
  // is cheapest near where I am looking", which is the question you come back for.
  scope = signal<AreaScope>(emptyScope());

  sortColumn = signal<SortColumn>('pricePerM2');
  sortDirection = signal<SortDirection>('desc');

  // The ranking. Re-runs on every click without touching the network.
  sortedAreas = computed(() => {
    const rows = [...this.areas()];
    const column = this.sortColumn();
    const direction = this.sortDirection();

    rows.sort((a, b) => {
      let result = 0;

      switch (column) {
        case 'place':
          result = placeOwnName(a).localeCompare(placeOwnName(b), 'pt');
          break;

        case 'listings':
          result = a.listingCount - b.listingCount;
          break;

        case 'pricePerM2':
          result = a.medianPricePerM2 - b.medianPricePerM2;
          break;

        case 'area':
          result = a.medianAreaM2 - b.medianAreaM2;
          break;

        case 'deals':
          // On the share, not the count: 40 deals out of 2,000 listings is a worse hunting
          // ground than 8 out of 40, and sorting on the raw count just re-sorts by size.
          result = dealShare(a) - dealShare(b);
          break;
      }

      return direction === 'desc' ? -result : result;
    });

    return rows;
  });

  // Reads out what the table is currently showing, so the heading is never ambiguous.
  headline = computed(() => {
    const descending = this.sortDirection() === 'desc';

    switch (this.sortColumn()) {
      case 'listings':
        return descending ? 'Most listings' : 'Fewest listings';

      case 'place':
        return descending ? 'Z to A' : 'A to Z';

      case 'area':
        return descending ? 'Biggest homes' : 'Smallest homes';

      case 'deals':
        return descending ? 'Most under-priced' : 'Fewest under-priced';

      default:
        return descending ? 'Most expensive' : 'Best value';
    }
  });

  // The share of a place's listings asking below the model's estimate, for the Deals column.
  dealPercent(area: MarketAreaStatsResponse): number {
    return dealShare(area);
  }

  constructor(private readonly service: MarketAreaStatsService) {}

  ngOnInit(): void {
    this.load();
  }

  // Level and Min listings change which places come back, so both reload.
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

  // Click the same column again to flip the direction; click a new one and it starts at the
  // interesting end — highest first for the numbers, A to Z for the name.
  toggleSort(column: SortColumn): void {
    if (this.sortColumn() === column) {
      this.sortDirection.set(this.sortDirection() === 'asc' ? 'desc' : 'asc');

      return;
    }

    this.sortColumn.set(column);
    this.sortDirection.set(column === 'place' ? 'asc' : 'desc');
  }

  // True when there are too few listings behind the median to lean on it. The row still shows,
  // it just says so — a place ranked on 3 adverts is a lead, not a finding.
  isThin(area: MarketAreaStatsResponse): boolean {
    return area.listingCount < RELIABLE_LISTINGS;
  }

  // How many of the rows on screen are thin, for the count in the header.
  thinCount = computed(() => this.sortedAreas().filter(area => this.isThin(area)).length);

  // The little arrow shown next to the active column header.
  arrow(column: SortColumn): string {
    if (this.sortColumn() !== column) {
      return '';
    }

    return this.sortDirection() === 'asc' ? ' ▲' : ' ▼';
  }

  // Rebuild the numbers on the server, then read them back.
  recalculate(): void {
    this.recalculating.set(true);
    this.error.set(null);

    this.service.recalculate().subscribe({
      next: () => {
        this.recalculating.set(false);
        this.load();
      },
      error: () => {
        this.recalculating.set(false);
        this.error.set('Could not recalculate the stats. Check the API is running and you are allowed to write.');
      }
    });
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);

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
          this.error.set('Could not load the leaderboard. Check the API is running.');
          this.loading.set(false);
        }
      });
  }
}
