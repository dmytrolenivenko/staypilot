import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, switchMap } from 'rxjs';
import { MarketAreaStatsService } from '../../core/services/market-area-stats.service';
import { AreaLevel, MarketAreaBudgetItemResponse } from '../../core/models/market-area-stats';

// The columns you can sort by. Client-side only — the API never sees these.
type SortColumn = 'place' | 'typology' | 'area' | 'price' | 'pricePerM2';
type SortDirection = 'asc' | 'desc';

// What your money buys — enter a budget and see what it reaches in each place, rather than
// filtering places by price. Every portal makes you filter BY price; this inverts it.
//
// The server decides what is affordable (it holds the per-typology medians) and leaves out
// places where the budget reaches nothing. Sorting is done here, same as the leaderboard.
@Component({
  selector: 'app-market-area-budget',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './market-area-budget.component.html',
  styleUrl: './market-area-budget.component.css'
})
export class MarketAreaBudgetComponent implements OnInit {
  readonly levels: AreaLevel[] = ['District', 'Municipality', 'Town'];

  areas = signal<MarketAreaBudgetItemResponse[]>([]);
  calculatedAtUtc = signal<string | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  budget = signal(300000);
  level = signal<AreaLevel>('Municipality');
  minListings = signal(5);

  // Most space for the money first — that is the question this screen answers.
  sortColumn = signal<SortColumn>('area');
  sortDirection = signal<SortDirection>('desc');

  sortedAreas = computed(() => {
    const rows = [...this.areas()];
    const column = this.sortColumn();
    const direction = this.sortDirection();

    rows.sort((a, b) => {
      let result = 0;

      switch (column) {
        case 'place':
          result = a.displayName.localeCompare(b.displayName, 'pt');
          break;

        case 'typology':
          // Compare the number after the T, so T10 sorts above T9 instead of next to T1.
          result = typologyRooms(a.bestTypology) - typologyRooms(b.bestTypology);
          break;

        case 'area':
          result = a.medianAreaM2 - b.medianAreaM2;
          break;

        case 'price':
          result = a.medianPrice - b.medianPrice;
          break;

        case 'pricePerM2':
          result = a.medianPricePerM2 - b.medianPricePerM2;
          break;
      }

      return direction === 'desc' ? -result : result;
    });

    return rows;
  });

  // Typing in the budget box fires a request per keystroke, so it goes through here first.
  private readonly budgetChanges = new Subject<void>();

  // Every load goes through here, so a slow answer for an old budget cannot overwrite a newer one.
  private readonly loads = new Subject<void>();

  constructor(private readonly service: MarketAreaStatsService) {}

  ngOnInit(): void {
    this.budgetChanges.pipe(debounceTime(400)).subscribe(() => this.loads.next());

    this.loads
      .pipe(
        switchMap(() => {
          this.loading.set(true);
          this.error.set(null);

          return this.service.getBudgetRanking({
            budget: this.budget(),
            level: this.level(),
            minListings: this.minListings()
          });
        })
      )
      .subscribe({
        next: response => {
          this.areas.set(response.items);
          this.calculatedAtUtc.set(response.calculatedAtUtc);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Could not work out what the budget buys. Check the API is running.');
          this.loading.set(false);
        }
      });

    this.loads.next();
  }

  changeBudget(budget: number): void {
    this.budget.set(Number(budget));
    this.budgetChanges.next();
  }

  changeLevel(level: AreaLevel): void {
    this.level.set(level);
    this.loads.next();
  }

  changeMinListings(minListings: number): void {
    this.minListings.set(Number(minListings));
    this.loads.next();
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
}

// How many bedrooms a typology means, so "T10" sorts above "T9".
function typologyRooms(typology: string): number {
  return Number(typology.replace('T', ''));
}
