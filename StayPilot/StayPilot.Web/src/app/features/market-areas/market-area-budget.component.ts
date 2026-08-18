import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, switchMap } from 'rxjs';
import { MarketAreaStatsService } from '../../core/services/market-area-stats.service';
import { AreaLevel, MarketAreaBudgetItemResponse } from '../../core/models/market-area-stats';
import { TYPOLOGIES, Typology } from '../../core/models/enums';
import { PageHeaderComponent } from '../../shared/page-header.component';
import { ExplainerComponent } from '../../shared/explainer.component';
import { PlaceNameComponent, placeLevelLabel, placeOwnName } from '../../shared/place-name.component';
import { AreaScope, AreaScopePickerComponent, emptyScope } from '../../shared/area-scope-picker.component';

// The columns you can sort by. Client-side only — the API never sees these.
type SortColumn = 'place' | 'typology' | 'area' | 'price' | 'pricePerM2' | 'listings';
type SortDirection = 'asc' | 'desc';

// How far past the budget you can ask it to stretch. Offered as a few sensible steps rather than
// a free number: the question is "and what would a bit more get me", not "what would 7% get me".
const STRETCH_CHOICES = [0, 5, 10, 20];

// What your money buys — enter a budget and see what it reaches in each place, rather than
// filtering places by price. Every portal makes you filter BY price; this inverts it.
//
// The server decides what is affordable (it holds the per-typology medians) and leaves out
// places where the budget reaches nothing. Sorting is done here, same as the leaderboard.
@Component({
  selector: 'app-market-area-budget',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    PageHeaderComponent,
    ExplainerComponent,
    PlaceNameComponent,
    AreaScopePickerComponent
  ],
  templateUrl: './market-area-budget.component.html',
  styleUrl: './market-area-budget.component.css'
})
export class MarketAreaBudgetComponent implements OnInit {
  readonly levels: AreaLevel[] = ['District', 'Municipality', 'Town'];
  readonly typologies = TYPOLOGIES;
  readonly stretchChoices = STRETCH_CHOICES;

  // The dropdown reads in the same words the table does — "Town" on its own never said whether
  // it meant a freguesia or a município.
  levelName = placeLevelLabel;

  areas = signal<MarketAreaBudgetItemResponse[]>([]);
  reach = signal(0);
  calculatedAtUtc = signal<string | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  budget = signal(300000);
  level = signal<AreaLevel>('Municipality');
  minListings = signal(5);

  // Narrowed to one distrito, and inside it one município. Empty = the whole country.
  scope = signal<AreaScope>(emptyScope());

  // "Where does my money buy at least a T3" rather than "where does it buy something".
  // Empty string means no floor, which is what the API treats as no filter.
  minTypology = signal<Typology | ''>('');

  // How far past the budget it may reach. Zero keeps the board strictly affordable.
  stretchPercent = signal(0);

  // Which row is expanded to show the other typologies its budget also reaches.
  expandedPlace = signal<string | null>(null);

  // Most space for the money first — that is the question this screen answers.
  sortColumn = signal<SortColumn>('area');
  sortDirection = signal<SortDirection>('desc');

  // How many places are only in reach because the budget was stretched.
  stretchedCount = computed(() => this.areas().filter(area => area.needsStretch).length);

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

        case 'listings':
          // On the typology count, which is the number the column leads with — how much
          // evidence there is for THIS row, not how big the place is overall.
          result = a.typologyListingCount - b.typologyListingCount;
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
            minListings: this.minListings(),
            district: this.scope().district || undefined,
            municipality: this.scope().municipality || undefined,
            minTypology: this.minTypology() || undefined,
            stretchPercent: this.stretchPercent()
          });
        })
      )
      .subscribe({
        next: response => {
          this.areas.set(response.items);
          this.reach.set(response.reach);
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
    this.reload();
  }

  changeMinListings(minListings: number): void {
    this.minListings.set(Number(minListings));
    this.reload();
  }

  changeScope(scope: AreaScope): void {
    this.scope.set(scope);
    this.reload();
  }

  changeMinTypology(minTypology: Typology | ''): void {
    this.minTypology.set(minTypology);
    this.reload();
  }

  changeStretch(stretchPercent: number): void {
    this.stretchPercent.set(Number(stretchPercent));
    this.reload();
  }

  // One row's alternatives. Clicking the open row closes it, so the table never traps you in
  // an expanded state you have to hunt for a way out of.
  toggleExpanded(place: string): void {
    this.expandedPlace.set(this.expandedPlace() === place ? null : place);
  }

  isExpanded(place: string): boolean {
    return this.expandedPlace() === place;
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

  // A new filter means a new set of places, so nothing stays expanded across it — the open row
  // would otherwise be a place that is no longer in the table.
  private reload(): void {
    this.expandedPlace.set(null);
    this.loads.next();
  }
}

// How many bedrooms a typology means, so "T10" sorts above "T9".
function typologyRooms(typology: string): number {
  return Number(typology.replace('T', ''));
}
