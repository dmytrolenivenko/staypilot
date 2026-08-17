import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MarketAreaStatsService } from '../../core/services/market-area-stats.service';
import { AreaLevel, MarketAreaStatsResponse } from '../../core/models/market-area-stats';

type SortColumn = 'place' | 'discount' | 'margin' | 'projects';
type SortDirection = 'asc' | 'desc';

// A default renovation rate per m², well under the €1,000+/m² of a new build in build-cost.ts:
// a refurbishment reuses the structure. Deliberately editable — it is the one number on this
// screen we have not measured, and pretending otherwise is how an estimate turns into a fact.
const DEFAULT_RENOVATION_COST_PER_M2 = 650;

// Renovation upside — where the market pays you enough for taking the work on.
//
// One half is measured (what project stock actually sells for against finished stock, from real
// adverts) and the other half is estimated (what the work costs). They are kept in separate
// columns on purpose, never quietly subtracted into one score, because they are not the same
// kind of number.
@Component({
  selector: 'app-market-area-renovation',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './market-area-renovation.component.html',
  styleUrl: './market-area-renovation.component.css'
})
export class MarketAreaRenovationComponent implements OnInit {
  readonly levels: AreaLevel[] = ['District', 'Municipality', 'Town'];

  areas = signal<MarketAreaStatsResponse[]>([]);
  calculatedAtUtc = signal<string | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  level = signal<AreaLevel>('Municipality');
  minListings = signal(5);
  renovationCostPerM2 = signal(DEFAULT_RENOVATION_COST_PER_M2);

  sortColumn = signal<SortColumn>('margin');
  sortDirection = signal<SortDirection>('desc');

  // Only places where both sides were measured can be ranked — a discount needs a project price
  // AND a finished price to be a discount at all.
  measuredAreas = computed(() => this.areas().filter(area => area.renovationDiscountPerM2 !== null));

  sortedAreas = computed(() => {
    const rows = [...this.measuredAreas()];
    const column = this.sortColumn();
    const direction = this.sortDirection();

    rows.sort((a, b) => {
      let result = 0;

      switch (column) {
        case 'place':
          result = a.displayName.localeCompare(b.displayName, 'pt');
          break;

        case 'discount':
          result = (a.renovationDiscountPerM2 ?? 0) - (b.renovationDiscountPerM2 ?? 0);
          break;

        case 'margin':
          result = this.margin(a) - this.margin(b);
          break;

        case 'projects':
          result = a.projectCount - b.projectCount;
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
    this.load();
  }

  changeMinListings(minListings: number): void {
    this.minListings.set(Number(minListings));
    this.load();
  }

  // Costs are applied here, not on the server, so changing the rate is instant.
  changeRenovationCost(costPerM2: number): void {
    this.renovationCostPerM2.set(Number(costPerM2));
  }

  // What the market pays you per m² for the work, minus what the work costs.
  // Positive means the discount more than covers the renovation.
  margin(area: MarketAreaStatsResponse): number {
    return (area.renovationDiscountPerM2 ?? 0) - this.renovationCostPerM2();
  }

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
      return 'good';
    }

    return margin >= -100 ? 'even' : 'bad';
  }

  // Too few projects to lean on, even though a median was taken. Marked, not hidden.
  isThin(area: MarketAreaStatsResponse): boolean {
    return area.projectCount < 10;
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

    // Same read as the leaderboard: the renovation numbers ride on the stats row, so this needs
    // no endpoint of its own.
    this.service.getLeaderboard({ level: this.level(), minListings: this.minListings() }).subscribe({
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
