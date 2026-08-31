import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { OwnedPropertyService } from '../../core/services/owned-property.service';
import {
  DemandLevel,
  GrowthScenarioResponse,
  OwnedPropertyAnalysisResponse,
  OwnedPropertyPortfolioItemResponse,
  OwnedPropertyPortfolioResponse
} from '../../core/models/owned-property';
import { PageHeaderComponent } from '../../shared/page-header.component';
import { ExplainerComponent } from '../../shared/explainer.component';

// Columns of the list. Sorted in the browser — a portfolio is a handful of rows.
type PortfolioSort = 'name' | 'place' | 'value' | 'pricePerM2' | 'spread' | 'demand' | 'projected';
type SortDirection = 'asc' | 'desc';

// Columns of the two tables inside an expanded property.
type AdjustmentSort = 'label' | 'detail' | 'amount';
type CompSort = 'typology' | 'area' | 'pricePerM2' | 'beach' | 'snapshot';

// Where each band sits on the 0-100 scale, so the badge and the meter agree.
const DEMAND_ORDER: Record<DemandLevel, number> = {
  Cold: 1,
  Soft: 2,
  Balanced: 3,
  Firm: 4,
  Hot: 5
};

// Compare on the number after the T, so T10 sorts above T9 rather than next to T1.
function typologyRooms(typology: string): number {
  return Number(String(typology ?? '').replace(/^T/i, '')) || 0;
}

// The Base path is what the list column and the portfolio total quote. Named rather than
// indexed so a reordering on the server cannot silently swap it for the optimistic one.
function baseScenario(item: OwnedPropertyPortfolioItemResponse): GrowthScenarioResponse | null {
  return item.forecast.scenarios.find(s => s.name === 'Base') ?? null;
}

// Valuation — every property you own, priced in one pass, each row opening into the
// evidence behind its number: what the neighbours ask, what the features contribute,
// how keen buyers are around it, and where the value goes from here.
//
// Backed by OwnedPropertyController.ListValuationsOwnedproperty for the list, and by
// EstimateEvaluationsOwnedproperty for the comps and adjustments of whichever row is open —
// those two are per-property and large, so they are fetched only when a row is expanded.
@Component({
  selector: 'app-valuation',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, PageHeaderComponent, ExplainerComponent],
  templateUrl: './valuation.component.html',
  styleUrl: './valuation.component.css'
})
export class ValuationComponent implements OnInit {
  portfolio = signal<OwnedPropertyPortfolioResponse | null>(null);

  loading = signal(true);
  error = signal<string | null>(null);

  // Recalculating refits the pricing model and reprices every property - the expensive path,
  // only run on demand. Loading a page is always the cheap read above.
  recalculating = signal(false);
  recalculateError = signal<string | null>(null);

  // Which single row is being recalculated, if any.
  recalculatingId = signal<number | null>(null);
  recalculateOneError = signal<string | null>(null);

  // How far back a comparable advert may have last been seen, and how far out one still counts.
  // These now only take effect when Recalculate runs - the list itself is a plain read of
  // whatever was stored at the last recalculation.
  months = signal(12);
  radiusMeters = signal(2000);

  // How far the projections run. Ten by default because that is the horizon the seeded growth
  // rates are pitched at; one year on its own reads as a prediction rather than a direction.
  years = signal(10);

  // The property whose panel is open. Only one at a time — the panel is a page of its own.
  expandedId = signal<number | null>(null);

  // The comps and adjustments for the open property, fetched on expand and kept per id so
  // re-opening a row you already looked at costs nothing.
  details = signal<Record<number, OwnedPropertyAnalysisResponse>>({});
  detailLoadingId = signal<number | null>(null);
  detailError = signal<string | null>(null);

  sortColumn = signal<PortfolioSort>('value');
  sortDirection = signal<SortDirection>('desc');

  items = computed(() => this.portfolio()?.items ?? []);

  sortedItems = computed(() => {
    const rows = [...this.items()];
    const column = this.sortColumn();
    const direction = this.sortDirection();

    rows.sort((a, b) => {
      let result = 0;

      switch (column) {
        case 'name':
          result = a.name.localeCompare(b.name, 'pt');
          break;

        case 'place':
          result = this.placeLabel(a).localeCompare(this.placeLabel(b), 'pt');
          break;

        case 'value':
          result = a.midPrice - b.midPrice;
          break;

        case 'pricePerM2':
          result = a.pricePerM2 - b.pricePerM2;
          break;

        case 'spread':
          // On the percentage, not the amount: otherwise this just re-sorts by property value.
          result = a.askSpread.spreadPercent - b.askSpread.spreadPercent;
          break;

        case 'demand':
          // Places that could not be measured sink to the bottom either way — an unmeasured
          // place is not a Balanced one, and sorting them together would say it was.
          if (a.demand.isMeasurable !== b.demand.isMeasurable) {
            return a.demand.isMeasurable ? -1 : 1;
          }

          result = a.demand.score - b.demand.score;
          break;

        case 'projected':
          result = (baseScenario(a)?.finalYearValue ?? 0) - (baseScenario(b)?.finalYearValue ?? 0);
          break;
      }

      return direction === 'desc' ? -result : result;
    });

    return rows;
  });

  // --- Adjustments table (inside the open panel) ---------------------------
  adjustmentSort = signal<AdjustmentSort>('amount');
  adjustmentDirection = signal<SortDirection>('desc');

  sortedAdjustments = computed(() => {
    const rows = [...(this.openDetail()?.adjustments ?? [])];
    const column = this.adjustmentSort();
    const direction = this.adjustmentDirection();

    rows.sort((a, b) => {
      // Rows the data cannot speak to sink to the bottom whichever way the column is sorted,
      // the same rule the Feature Impact table uses.
      if (a.isMeasurable !== b.isMeasurable) {
        return a.isMeasurable ? -1 : 1;
      }

      let result = 0;

      switch (column) {
        case 'label':
          result = a.label.localeCompare(b.label);
          break;

        case 'detail':
          result = (a.detail ?? '').localeCompare(b.detail ?? '');
          break;

        case 'amount':
          result = a.amount - b.amount;
          break;
      }

      return direction === 'desc' ? -result : result;
    });

    return rows;
  });

  // --- Comparables table (inside the open panel) ---------------------------
  // Nearest first is how the server hands them over, and how they are weighted, so the
  // default keeps that order rather than imposing one of its own.
  compSort = signal<CompSort | null>(null);
  compDirection = signal<SortDirection>('asc');

  sortedComps = computed(() => {
    const rows = [...(this.openDetail()?.comps ?? [])];
    const column = this.compSort();

    if (column === null) {
      return rows;
    }

    const direction = this.compDirection();

    rows.sort((a, b) => {
      let result = 0;

      switch (column) {
        case 'typology':
          result = typologyRooms(a.typology) - typologyRooms(b.typology);
          break;

        case 'area':
          result = a.areaM2 - b.areaM2;
          break;

        case 'pricePerM2':
          result = a.pricePerM2 - b.pricePerM2;
          break;

        case 'beach':
          result = (a.distanceToBeachMeters ?? 0) - (b.distanceToBeachMeters ?? 0);
          break;

        case 'snapshot':
          result = Date.parse(a.snapshotDateUtc) - Date.parse(b.snapshotDateUtc);
          break;
      }

      return direction === 'desc' ? -result : result;
    });

    return rows;
  });

  constructor(
    private readonly service: OwnedPropertyService,
    private readonly route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.load();
  }

  // Arriving from "My Properties" with ?propertyId=<id> opens that row once the list lands.
  // Reads the cache - no model fit, no comp search - so this is instant even with hundreds of
  // listings collected. Only "Re-price" below pays for a fresh computation.
  private load(): void {
    this.loading.set(true);
    this.error.set(null);

    const requestedId = Number(this.route.snapshot.queryParamMap.get('propertyId'));

    this.service.portfolio().subscribe({
      next: response => {
        this.portfolio.set(response);
        this.loading.set(false);

        if (requestedId > 0 && response.items.some(x => x.id === requestedId)) {
          this.toggle(requestedId);
        }
      },
      error: () => {
        this.error.set('Could not load your cached valuations. Check the API is running.');
        this.loading.set(false);
      }
    });
  }

  // Refits the pricing model and reprices every property with whatever the three settings above
  // are set to, then stores the result - the next plain load() reads it back without refitting
  // anything. The open row closes: its comps were fetched against the old numbers and would
  // otherwise sit there stale.
  recalculateAll(): void {
    this.recalculating.set(true);
    this.recalculateError.set(null);
    this.expandedId.set(null);
    this.details.set({});

    this.service.recalculateAll(this.months(), this.radiusMeters(), this.years()).subscribe({
      next: response => {
        this.portfolio.set(response);
        this.recalculating.set(false);
      },
      error: () => {
        this.recalculateError.set(
          'Could not recalculate. Check the API is running, and that there are enough listings collected to fit the model.'
        );
        this.recalculating.set(false);
      }
    });
  }

  // Reprices just one property, with the current Look back / Comp radius / Project settings.
  // Stops the click reaching the row's own (click), which would otherwise also toggle it open.
  recalculateOne(id: number, event: Event): void {
    event.stopPropagation();

    this.recalculatingId.set(id);
    this.recalculateOneError.set(null);

    this.service.recalculateOne(id, this.months(), this.radiusMeters(), this.years()).subscribe({
      next: response => {
        this.recalculatingId.set(null);

        if (!response.item) {
          this.recalculateOneError.set('Not enough listings collected to price this property yet.');

          return;
        }

        const priced = response.item;

        this.portfolio.update(current =>
          current === null
            ? current
            : { ...current, items: current.items.map(x => (x.id === id ? priced : x)) }
        );

        // The comps shown in the open panel were fetched against the old numbers - drop the
        // cached ones. If this row is open right now, fetch its replacement immediately rather
        // than leaving the panel blank until it is closed and reopened.
        this.details.update(current => {
          const { [id]: _dropped, ...rest } = current;

          return rest;
        });

        if (this.expandedId() === id) {
          this.fetchDetail(id);
        }
      },
      error: () => {
        this.recalculatingId.set(null);
        this.recalculateOneError.set('Could not recalculate this property.');
      }
    });
  }

  toggleSort(column: PortfolioSort): void {
    if (this.sortColumn() === column) {
      this.sortDirection.set(this.sortDirection() === 'asc' ? 'desc' : 'asc');

      return;
    }

    this.sortColumn.set(column);
    this.sortDirection.set(column === 'name' || column === 'place' ? 'asc' : 'desc');
  }

  arrow(column: PortfolioSort): string {
    if (this.sortColumn() !== column) {
      return '';
    }

    return this.sortDirection() === 'asc' ? ' ▲' : ' ▼';
  }

  // Open a row, closing whichever was open. The comps and adjustments are fetched the first
  // time only — they do not change until a setting does, and that closes everything anyway.
  toggle(id: number): void {
    if (this.expandedId() === id) {
      this.expandedId.set(null);

      return;
    }

    this.expandedId.set(id);
    this.detailError.set(null);

    if (this.details()[id]) {
      return;
    }

    this.fetchDetail(id);
  }

  private fetchDetail(id: number): void {
    this.detailLoadingId.set(id);

    this.service.estimate(id, this.months(), this.radiusMeters()).subscribe({
      next: detail => {
        this.details.update(current => ({ ...current, [id]: detail }));
        this.detailLoadingId.set(null);
      },
      error: () => {
        this.detailError.set('Could not load the comparables for this property.');
        this.detailLoadingId.set(null);
      }
    });
  }

  // The comps/adjustments of the open row, once they have arrived.
  openDetail = computed(() => {
    const id = this.expandedId();

    return id === null ? null : this.details()[id] ?? null;
  });

  openItem = computed(() => {
    const id = this.expandedId();

    return id === null ? null : this.items().find(x => x.id === id) ?? null;
  });

  // --- Formatting helpers --------------------------------------------------

  // "Quarteira · Loulé, Faro" — narrowest first, then what it sits inside, same order the
  // market screens use so a place reads the same wherever it appears.
  placeLabel(item: OwnedPropertyPortfolioItemResponse): string {
    return [item.town, item.municipality, item.district].filter(part => part).join(' · ');
  }

  // False for a property added since the last Recalculate - it has no price yet, not a €0 one.
  isPriced(item: OwnedPropertyPortfolioItemResponse): boolean {
    return item.valuatedAtUtc !== null;
  }

  // Where the demand needle sits, 0-100, for the little meter in the panel.
  demandOffset(score: number): number {
    return Math.max(0, Math.min(100, score));
  }

  demandRank(level: DemandLevel): number {
    return DEMAND_ORDER[level] ?? 0;
  }

  // Compact euros for the table cells: €1.2M rather than €1,234,567, which is what pushed the
  // Market Overview bars off their own rows. The exact figure stays in the title attribute.
  money(value: number): string {
    const absolute = Math.abs(value);

    if (absolute >= 1_000_000) {
      return `€${(value / 1_000_000).toFixed(1)}M`;
    }

    if (absolute >= 10_000) {
      return `€${Math.round(value / 1000)}k`;
    }

    return `€${Math.round(value).toLocaleString('pt-PT')}`;
  }

  scenario(item: OwnedPropertyPortfolioItemResponse, name: string): GrowthScenarioResponse | null {
    return item.forecast.scenarios.find(s => s.name === name) ?? null;
  }

  base(item: OwnedPropertyPortfolioItemResponse): GrowthScenarioResponse | null {
    return baseScenario(item);
  }

  // The years to print in the projection table. Ten rows of a ten year path is a wall, so the
  // milestones people actually plan against: next year, five out, and the end of the horizon.
  milestones = computed(() => {
    const total = this.portfolio()?.projectionYears ?? 0;

    return [1, 5, total].filter((year, index, all) => year > 0 && year <= total && all.indexOf(year) === index);
  });


  // --- Adjustment / comp table sorting -------------------------------------

  toggleAdjustmentSort(column: AdjustmentSort): void {
    if (this.adjustmentSort() === column) {
      this.adjustmentDirection.set(this.adjustmentDirection() === 'asc' ? 'desc' : 'asc');

      return;
    }

    this.adjustmentSort.set(column);
    this.adjustmentDirection.set(column === 'amount' ? 'desc' : 'asc');
  }

  adjustmentArrow(column: AdjustmentSort): string {
    if (this.adjustmentSort() !== column) {
      return '';
    }

    return this.adjustmentDirection() === 'asc' ? ' ▲' : ' ▼';
  }

  toggleCompSort(column: CompSort): void {
    if (this.compSort() === column) {
      this.compDirection.set(this.compDirection() === 'asc' ? 'desc' : 'asc');

      return;
    }

    this.compSort.set(column);
    this.compDirection.set(column === 'typology' ? 'asc' : 'desc');
  }

  compArrow(column: CompSort): string {
    if (this.compSort() !== column) {
      return '';
    }

    return this.compDirection() === 'asc' ? ' ▲' : ' ▼';
  }
}
