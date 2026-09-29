import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { Subscription } from 'rxjs';
import { TopDealsService } from '../../core/services/top-deals.service';
import { TopDealResponse } from '../../core/models/top-deals';
import { PROPERTY_CONDITION_OPTIONS } from '../../core/models/enums';
import { AreaScope, AreaScopePickerComponent, emptyScope } from '../../shared/area-scope-picker.component';
import { apiErrorMessage } from '../../core/api-error';
import { clickedRowControl } from '../../shared/row-click';

// A deal with the rank the server gave it. Held separately from the row order so re-sorting the
// table cannot renumber the ranking - #1 is the biggest discount whichever column you click.
export interface RankedDeal {
  rank: number;
  deal: TopDealResponse;
}

// The columns you can sort the table by.
type DealSort = 'rank' | 'location' | 'type' | 'condition' | 'area' | 'price' | 'pricePerM2' | 'median' | 'discount';
type SortDirection = 'asc' | 'desc';

// Top deals — the active listings asking the most below their own town's median €/m².
// Ranked on the server (it needs the market area stats table). That ranking is the finding, so
// the # column keeps it whatever you sort by - the columns re-order the same set of deals to
// answer "the cheapest of them" or "the biggest of them", they do not re-rank it.
@Component({
  selector: 'app-top-deals',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, AreaScopePickerComponent],
  templateUrl: './top-deals.component.html',
  styleUrl: './top-deals.component.css'
})
export class TopDealsComponent implements OnInit {
  readonly conditions = PROPERTY_CONDITION_OPTIONS;

  // The deals as ranked by the server, each carrying its rank.
  deals = signal<RankedDeal[]>([]);

  // Sorting. The table is at most 50 rows that the server already chose, so this sorts what is
  // on screen - there is no page two to be wrong about, unlike the listing browser.
  sortColumn = signal<DealSort>('rank');
  sortDirection = signal<SortDirection>('asc');

  sortedDeals = computed(() => {
    const rows = [...this.deals()];
    const column = this.sortColumn();
    const descending = this.sortDirection() === 'desc';

    rows.sort((a, b) => {
      const result = this.compare(a, b, column);

      return descending ? -result : result;
    });

    return rows;
  });
  calculatedAtUtc = signal<string | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  scope = signal<AreaScope>(emptyScope());
  count = signal(10);

  // Renovation projects and move-in-ready homes are different markets - a fixer-upper's low
  // €/m² reflects the work it needs, not a bargain. The API already grades each listing against
  // its own bucket's median, but this narrows the list itself to one condition when set.
  condition = signal('');

  // Every control on the toolbar reloads, and picking a distrito then a município is two of them
  // in a second. Two requests then sit open over a table of thousands of listings and whichever
  // answers last wins — which can be the older one, showing deals for the distrito you have
  // already narrowed away. Cancelling the open one keeps exactly one in flight.
  private inFlight?: Subscription;

  constructor(
    private readonly service: TopDealsService,
    private readonly router: Router
  ) {}

  // A click anywhere on a deal row opens that listing, not only its location link.
  openListing(id: number, event: MouseEvent): void {
    if (clickedRowControl(event)) {
      return;
    }

    this.router.navigate(['/listings', id]);
  }

  ngOnInit(): void {
    this.load();
  }

  changeScope(scope: AreaScope): void {
    this.scope.set(scope);
    this.load();
  }

  changeCount(count: number): void {
    this.count.set(Number(count));
    this.load();
  }

  changeCondition(condition: string): void {
    this.condition.set(condition);
    this.load();
  }

  // Click a header to sort by it; click the same one again to flip. The interesting end first:
  // biggest discount, cheapest price, largest floor — and A to Z for the text columns.
  toggleSort(column: DealSort): void {
    if (this.sortColumn() === column) {
      this.sortDirection.set(this.sortDirection() === 'asc' ? 'desc' : 'asc');

      return;
    }

    this.sortColumn.set(column);
    this.sortDirection.set(this.startsAscending(column) ? 'asc' : 'desc');
  }

  // The little arrow shown next to the active column header.
  arrow(column: DealSort): string {
    if (this.sortColumn() !== column) {
      return '';
    }

    return this.sortDirection() === 'asc' ? ' ▲' : ' ▼';
  }

  // Which columns read best low-to-high on the first click: the three text ones, and the rank,
  // where ascending is the ranking itself.
  private startsAscending(column: DealSort): boolean {
    return column === 'rank' || column === 'location' || column === 'type' || column === 'condition';
  }

  // Two rows on one column. Text compares in Portuguese collation so accented names sort where
  // a reader expects them; a missing price sinks rather than counting as zero.
  private compare(first: RankedDeal, second: RankedDeal, column: DealSort): number {
    switch (column) {
      case 'location':
        return this.placeName(first).localeCompare(this.placeName(second), 'pt');

      case 'type':
        return this.typeName(first).localeCompare(this.typeName(second), 'pt');

      case 'condition':
        return String(first.deal.listing.condition ?? '').localeCompare(
          String(second.deal.listing.condition ?? ''),
          'pt'
        );

      case 'area':
        return first.deal.listing.areaM2 - second.deal.listing.areaM2;

      case 'price':
        return this.missingLast(
          first.deal.listing.listingSnapshot?.price,
          second.deal.listing.listingSnapshot?.price
        );

      case 'pricePerM2':
        return this.missingLast(
          first.deal.listing.listingSnapshot?.pricePerM2,
          second.deal.listing.listingSnapshot?.pricePerM2
        );

      case 'median':
        return first.deal.townMedianPricePerM2 - second.deal.townMedianPricePerM2;

      case 'discount':
        return first.deal.discountPercent - second.deal.discountPercent;

      default:
        return first.rank - second.rank;
    }
  }

  // What the Location column reads, so the sort and the cell can never disagree.
  private placeName(row: RankedDeal): string {
    return row.deal.listing.marketAreaTown || row.deal.listing.marketAreaMunicipality || '';
  }

  // Same, for the Type column, which prints the property type and the typology together.
  private typeName(row: RankedDeal): string {
    return `${row.deal.listing.propertyType} ${row.deal.listing.typology}`;
  }

  // A listing with no snapshot has no price. Sorting it as 0 would park it at the top of
  // "cheapest first" as the best bargain on the board, which it is not — it is unknown.
  // Returned against the current direction so it sinks either way round.
  private missingLast(first: number | undefined, second: number | undefined): number {
    if (first == null && second == null) {
      return 0;
    }

    const sinks = this.sortDirection() === 'desc' ? -1 : 1;

    if (first == null) {
      return sinks;
    }

    if (second == null) {
      return -sinks;
    }

    return first - second;
  }

  private load(): void {
    this.inFlight?.unsubscribe();
    this.loading.set(true);
    this.error.set(null);

    this.inFlight = this.service
      .getTopDeals({
        district: this.scope().district || undefined,
        municipality: this.scope().municipality || undefined,
        condition: (this.condition() || undefined) as any,
        count: this.count()
      })
      .subscribe({
        next: response => {
          // Rank stamped here, in the order the server ranked them, before any sorting.
          this.deals.set(response.items.map((deal, index) => ({ rank: index + 1, deal })));
          this.calculatedAtUtc.set(response.calculatedAtUtc);
          this.loading.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.deals.set([]);
          this.calculatedAtUtc.set(null);
          this.error.set(apiErrorMessage(err, 'Could not load the top deals.'));
          this.loading.set(false);
        }
      });
  }
}
