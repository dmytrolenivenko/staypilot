import { CommonModule } from '@angular/common';
import { Component, Input, OnChanges, OnDestroy, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MarketAreaStatsService } from '../../core/services/market-area-stats.service';
import {
  AreaLevel,
  MarketAreaStatsResponse,
  RecalculateMarketAreaStatsResponse,
  RELIABLE_LISTINGS
} from '../../core/models/market-area-stats';
import { ExplainerComponent } from '../../shared/explainer.component';
import { PlaceNameComponent, placeLevelLabel, placeOwnName } from '../../shared/place-name.component';
import { AreaScope, emptyScope } from '../../shared/area-scope-picker.component';

// The columns you can sort by. Client-side only — the API never sees these.
import { HttpErrorResponse } from '@angular/common/http';
import { Subscription, switchMap, takeUntil, takeWhile, timer } from 'rxjs';
import { apiErrorMessage } from '../../core/api-error';
type SortColumn = 'place' | 'listings' | 'pricePerM2' | 'area' | 'deals';
type SortDirection = 'asc' | 'desc';

// How often to ask the server how the recalculation is going. Often enough to feel live, rare
// enough not to pester an API that is busy doing the actual work.
const POLL_EVERY_MS = 3000;

// When to stop asking. Far past any real run — it is here so a run that dies without saying so
// (the free-tier app can be recycled mid-run) ends the spinner instead of leaving it turning
// forever.
const POLL_GIVE_UP_MS = 20 * 60 * 1000;

// What share of a place's listings are asking below the model's estimate, 0-100.
// A share rather than a count: 40 deals out of 2,000 is a worse hunting ground than 8 out of 40.
function dealShare(area: MarketAreaStatsResponse): number {
  return area.listingCount === 0 ? 0 : (area.belowEstimateCount / area.listingCount) * 100;
}

// Below this many listings a median is worth reading with suspicion, so the row is marked.
// Not hidden: hiding them is what made the cheapest place on the board (Beja, 1,937) far from
// the cheapest place in the data (Póvoa de São Miguel, 419) — true, just thinly evidenced.

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
    ExplainerComponent,
    PlaceNameComponent
  ],
  templateUrl: './market-area-leaderboard.component.html',
  styleUrl: './market-area-leaderboard.component.css'
})
export class MarketAreaLeaderboardComponent implements OnInit, OnChanges, OnDestroy {
  // The dropdown reads in the same words the table does — "Town" on its own never said whether
  // it meant a freguesia or a município.
  levelName = placeLevelLabel;

  areas = signal<MarketAreaStatsResponse[]>([]);
  calculatedAtUtc = signal<string | null>(null);
  loading = signal(true);
  recalculating = signal(false);
  error = signal<string | null>(null);

  // What the running recalculation is doing, for the line under the button. Null when none is.
  recalcNote = signal<string | null>(null);

  // --- The shared filters, owned by the Places shell -------------------------------
  //
  // Place, grain and sample gate used to be three controls on this screen, and three more on
  // each of the other three screens that read the same table. They are now set once above the
  // question switcher and handed down, so changing the lens keeps the place you were looking at.
  // The signals stay because the whole template reads them; only who sets them has moved.

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

  @Input({ required: true, alias: 'level' }) set levelInput(value: AreaLevel) {
    this.level.set(value);
  }

  @Input({ required: true, alias: 'minListings' }) set minListingsInput(value: number) {
    this.minListings.set(Number(value));
  }

  @Input({ required: true, alias: 'scope' }) set scopeInput(value: AreaScope) {
    this.scope.set(value);
  }

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

  // Level, min listings and both scope dropdowns each reload, and picking a distrito then a
  // município is two of them in a second. Two requests then sit open and whichever answers last
  // wins - which can be the older, broader one. Cancelling keeps exactly one in flight.
  private inFlight?: Subscription;

  // The "how is the recalculation going" poll. Separate from inFlight: a poll runs for minutes
  // and must survive the reloads a user triggers while they wait.
  private polling?: Subscription;

  // False until the first load has been asked for, so the shell setting its three inputs on the
  // first change-detection pass does not fire a request before the component is even up.
  private started = false;

  constructor(private readonly service: MarketAreaStatsService) {}

  ngOnInit(): void {
    this.started = true;
    this.load();

    // A run started from another tab - or before a reload - is still going on the server, and
    // the screen should pick it up rather than look idle while the numbers are being rewritten.
    this.service.getRecalculationStatus().subscribe({
      next: status => {
        if (status.isRunning) {
          this.recalculating.set(true);
          this.showProgress(status);
          this.pollUntilDone(this.calculatedAtUtc());
        }
      },
      // A status we cannot read is not worth a red box on a screen that otherwise works.
      error: () => {}
    });
  }

  ngOnDestroy(): void {
    this.polling?.unsubscribe();
    this.inFlight?.unsubscribe();
  }

  // The shell changed one of the shared filters. All three change which places come back, so
  // any of them reloads - but only once per change, however many moved together: picking a
  // distrito also narrows the grain, and that used to be two requests racing each other.
  //
  // Angular runs ngOnChanges before ngOnInit on the first cycle, so the first load is left to
  // ngOnInit rather than fired here against half-set inputs.
  ngOnChanges(): void {
    if (this.started) {
      this.load();
    }
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

  // Rebuild the numbers on the server.
  //
  // The POST only STARTS the run: it reads every listing we hold and rewrites the whole stats
  // table, which takes minutes once the data is national, and Azure closes the request at around
  // 230 seconds whatever the browser is willing to wait. So this starts it and then asks how it
  // is going until the server says it has stopped.
  recalculate(): void {
    this.recalculating.set(true);
    this.error.set(null);
    this.recalcNote.set('Starting…');

    // The stamp on the rows we already have. A finished run has to beat this to have landed.
    const stampBefore = this.calculatedAtUtc();

    this.service.recalculate().subscribe({
      next: status => {
        this.showProgress(status);
        this.pollUntilDone(stampBefore);
      },
      error: (err: HttpErrorResponse) => {
        this.stopRecalculating();
        this.error.set(
          apiErrorMessage(err, 'Could not start the recalculation. Check the API is running and you are signed in.')
        );
      }
    });
  }

  // Asks how the run is going until it stops, then says what happened.
  private pollUntilDone(stampBefore: string | null): void {
    this.polling?.unsubscribe();

    this.polling = timer(POLL_EVERY_MS, POLL_EVERY_MS)
      .pipe(
        switchMap(() => this.service.getRecalculationStatus()),
        // inclusive: takeWhile normally drops the emission that ends it, and that is the only
        // one carrying how the run finished.
        takeWhile(status => status.isRunning, true),
        takeUntil(timer(POLL_GIVE_UP_MS))
      )
      .subscribe({
        next: status => {
          if (status.isRunning) {
            this.showProgress(status);

            return;
          }

          this.settle(status, stampBefore);
        },
        error: (err: HttpErrorResponse) => {
          this.stopRecalculating();
          this.error.set(apiErrorMessage(err, 'Lost track of the recalculation. Reload to see where it got to.'));
        }
      });
  }

  // The run has stopped. Whether it actually rebuilt anything is a separate question.
  private settle(status: RecalculateMarketAreaStatsResponse, stampBefore: string | null): void {
    this.stopRecalculating();

    if (status.failureReason) {
      this.error.set(`The recalculation failed: ${status.failureReason}`);

      return;
    }

    // Stopped, did not fail, and yet the rows carry the stamp they had before: the run never
    // landed. The API runs on a plan with no "Always On", so an idle recycle can take a run down
    // with it — saying nothing here would leave the old numbers on screen looking rebuilt.
    if (!status.calculatedAtUtc || status.calculatedAtUtc === stampBefore) {
      this.error.set('The recalculation stopped before it finished. Nothing was changed — try again.');

      return;
    }

    this.load();
  }

  // "Working… 2m 15s", from the server's own start time rather than a timer in the browser, so
  // it stays right across a reload or a second tab.
  private showProgress(status: RecalculateMarketAreaStatsResponse): void {
    if (!status.isRunning || !status.startedAtUtc) {
      this.recalcNote.set('Working…');

      return;
    }

    const seconds = Math.max(0, Math.round((Date.now() - new Date(status.startedAtUtc).getTime()) / 1000));
    const elapsed = seconds < 60 ? `${seconds}s` : `${Math.floor(seconds / 60)}m ${seconds % 60}s`;

    this.recalcNote.set(`Working through every listing… ${elapsed}`);
  }

  private stopRecalculating(): void {
    this.polling?.unsubscribe();
    this.polling = undefined;
    this.recalculating.set(false);
    this.recalcNote.set(null);
  }

  private load(): void {
    this.inFlight?.unsubscribe();
    this.loading.set(true);
    this.error.set(null);

    this.inFlight = this.service
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
        error: (err: HttpErrorResponse) => {
          // Clear the rows too: the header counts them, so leaving them up prints
          // "47 places · 5 thinly evidenced" directly above a red box saying it failed.
          this.areas.set([]);
          this.calculatedAtUtc.set(null);
          this.error.set(apiErrorMessage(err, 'Could not load the leaderboard.'));
          this.loading.set(false);
        }
      });
  }
}
