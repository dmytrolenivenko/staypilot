import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MarketAreaService } from '../../core/services/market-area.service';
import { MarketOverviewService } from '../../core/services/market-overview.service';
import { MarketOverviewBreakdownItem, MarketOverviewResponse } from '../../core/models/market-overview';
import { PROPERTY_TYPES, PropertyType, TYPOLOGIES, Typology } from '../../core/models/enums';
import { AreaLevel } from '../../core/models/market-area-stats';
import { PageHeaderComponent } from '../../shared/page-header.component';
import { ExplainerComponent } from '../../shared/explainer.component';
import { PlaceNameComponent, placeLevelLabel } from '../../shared/place-name.component';
import { Subscription } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { apiErrorMessage } from '../../core/api-error';

// Bars offered for the distribution. Ten reads well on a normal window; the API caps at 20.
const BUCKET_CHOICES = [6, 10, 14, 20];

// Every column of the by-typology table is sortable.
type TypologySort = 'typology' | 'listings' | 'price' | 'area' | 'pricePerM2';
type SortDirection = 'asc' | 'desc';

// Same for the breakdown, which is the table you actually read on a broad slice.
type BreakdownSort = 'place' | 'listings' | 'price' | 'area' | 'pricePerM2' | 'vsSlice';

// How many places the breakdown shows before it stops. A whole-country slice breaks into 18
// districts, but a município breaks into 40+ freguesias, and a table that long buries the top of
// itself. Collapsed to this with a button to see the rest — trimmed, never silently.
const BREAKDOWN_PREVIEW_ROWS = 12;

// Stands in for a figure the response did not carry. A dash reads as "no number here"; the
// alternative, "€NaN", reads as a number we worked out and got wrong.
const UNREADABLE = '—';

// One price written out in full, for the tooltip that backs up the short label on a bar.
function fullPrice(price: number): string {
  return Number.isFinite(price) ? `€${Math.round(price).toLocaleString('en-GB')}` : UNREADABLE;
}

// Above this the price labels on the distribution are written short (€1.2M rather than
// €1,200,000), because at full length they are wider than the column that holds them.
const COMPACT_PRICE_FROM = 1_000_000;

// Compare on the number after the T, so T10 sorts above T9 rather than next to T1.
function typologyRooms(typology: string): number {
  return Number(String(typology ?? '').replace(/^T/i, '')) || 0;
}

// Distrito/Distritos, Município/Municípios, Freguesia/Freguesias — all three take a plain -s.
function plural(label: string): string {
  return `${label}s`;
}

// Under this many listings the medians are being taken from a handful of adverts. Shown with a
// warning rather than hidden — "there are only four T5s in Guia" is a real finding.
const THIN_SLICE = 10;

// How far the average has to sit above the median before we point it out, as a share of the
// median. Below this the two numbers are just rounding apart.
const SKEW_NOTICE = 0.15;

// The picker. Empty string = "not narrowed", which is what the API treats as "no filter".
interface OverviewForm {
  district: string;
  municipality: string;
  town: string;
  propertyType: string;
  typology: string;
  buckets: number;
}

function emptyForm(): OverviewForm {
  return {
    district: '',
    municipality: '',
    town: '',
    propertyType: '',
    typology: '',
    buckets: 10
  };
}

// Market overview — what one slice of the market is asking.
//
// The place picker here is the whole of what the old Market Areas screen was for: the area table
// is reference data, useful to filter by and pointless to read as a list.
@Component({
  selector: 'app-market-overview',
  standalone: true,
  imports: [CommonModule, FormsModule, PageHeaderComponent, ExplainerComponent, PlaceNameComponent],
  templateUrl: './market-overview.component.html',
  styleUrl: './market-overview.component.css'
})
export class MarketOverviewComponent implements OnInit {
  readonly propertyTypes = PROPERTY_TYPES;
  readonly typologies = TYPOLOGIES;
  readonly bucketChoices = BUCKET_CHOICES;

  // Each level of the place picker, loaded from the API as you pick the one above it.
  districtOptions = signal<string[]>([]);
  municipalityOptions = signal<string[]>([]);
  townOptions = signal<string[]>([]);

  form: OverviewForm = emptyForm();

  overview = signal<MarketOverviewResponse | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);

  // The scope selects reload on change and the Show button submits, so one interaction can
  // start a second request while the first is still open - two identical calls over a table of
  // thousands of rows, and whichever answered last won. Cancelling the open one leaves exactly
  // one in flight and makes the newest request the one that renders.
  private inFlight?: Subscription;

  // The busiest bar, so every other bar can be drawn as a share of it. Scaling to the busiest
  // bar rather than to 100% is what makes the shape of a market visible at all.
  private busiestShare = computed(() => {
    const buckets = this.overview()?.distribution ?? [];

    return buckets.reduce(
      (most, bucket) => (Number.isFinite(bucket.sharePercent) ? Math.max(most, bucket.sharePercent) : most),
      0
    );
  });

  // The distribution came back with bars, but not one of them carries a readable share. That only
  // happens when the response is shaped differently from the contract — an API a version behind,
  // typically. Worth saying out loud: the alternative is a row of empty bars and no explanation.
  distributionUnreadable = computed(() => {
    const buckets = this.overview()?.distribution ?? [];

    return buckets.length > 0 && this.busiestShare() <= 0;
  });

  // Too few listings to lean on. Marked, never hidden.
  isThin = computed(() => {
    const overview = this.overview();

    return overview !== null && overview.listingCount > 0 && overview.listingCount < THIN_SLICE;
  });

  // True when a few expensive listings are dragging the average away from the median.
  isSkewed = computed(() => {
    const price = this.overview()?.price;

    if (!price || price.median <= 0) {
      return false;
    }

    return (price.average - price.median) / price.median > SKEW_NOTICE;
  });

  // How much higher the average sits, as a percentage of the median.
  skewPercent = computed(() => {
    const price = this.overview()?.price;

    if (!price || price.median <= 0) {
      return 0;
    }

    return ((price.average - price.median) / price.median) * 100;
  });

  constructor(
    private readonly marketAreas: MarketAreaService,
    private readonly service: MarketOverviewService
  ) {}

  ngOnInit(): void {
    this.marketAreas.getOptions().subscribe({
      next: districts => this.districtOptions.set(districts),
      // An empty dropdown and no message reads as "there are no distritos", which is never true.
      error: () => {
        this.districtOptions.set([]);
        this.error.set('Could not load the list of places. Check the API is running.');
      }
    });
  }

  onDistrictChange(): void {
    this.form.municipality = '';
    this.form.town = '';
    this.municipalityOptions.set([]);
    this.townOptions.set([]);

    if (this.form.district) {
      this.marketAreas.getOptions(this.form.district).subscribe({
        next: municipalities => this.municipalityOptions.set(municipalities),
        error: () => this.municipalityOptions.set([])
      });
    }

    this.load();
  }

  onMunicipalityChange(): void {
    this.form.town = '';
    this.townOptions.set([]);

    if (this.form.municipality) {
      this.marketAreas.getOptions(this.form.district, this.form.municipality).subscribe({
        next: towns => this.townOptions.set(towns),
        error: () => this.townOptions.set([])
      });
    }

    this.load();
  }

  // Every other control just reloads: one call answers the whole screen.
  load(): void {
    this.loading.set(true);
    this.error.set(null);

    // A new slice means a new set of places. Keeping the table expanded across that would open
    // a 40-row freguesia list because a district list was expanded a moment ago.
    this.showAllBreakdown.set(false);

    this.inFlight?.unsubscribe();

    this.inFlight = this.service
      .getMarketOverview({
        district: this.form.district || undefined,
        municipality: this.form.municipality || undefined,
        town: this.form.town || undefined,
        propertyType: (this.form.propertyType as PropertyType) || undefined,
        typology: (this.form.typology as Typology) || undefined,
        bucketCount: Number(this.form.buckets)
      })
      .subscribe({
        next: response => {
          this.overview.set(response);
          this.loading.set(false);
        },
        error: (err: HttpErrorResponse) => {
          // The whole screen reads off this, so a stale slice would sit under the error.
          this.overview.set(null);
          this.error.set(apiErrorMessage(err, 'Could not load the market overview.'));
          this.loading.set(false);
        }
      });
  }

  // A district picked with nothing narrowed under it. Then an empty answer means we have
  // collected nothing there at all - and telling the reader to widen the slice is advice that
  // cannot work, because there is nothing below it to widen into.
  nothingNarrowedBelowDistrict(): boolean {
    return !!this.form.district && !this.form.municipality && !this.form.town
      && !this.form.propertyType && !this.form.typology;
  }

  reset(): void {
    this.form = emptyForm();
    this.municipalityOptions.set([]);
    this.townOptions.set([]);
    this.overview.set(null);
    this.error.set(null);
  }

  // Bar width as a share of the busiest bar, so the tallest bar always fills the row.
  //
  // Anything that is not a real share comes out as 0%, never as a width the browser cannot parse:
  // an unparseable width is dropped, the fill then paints its natural full width, and a bucket we
  // know nothing about ends up drawn as the busiest one on the screen. That is how this screen
  // failed against a response whose buckets it could not read — ten full bars, all confident.
  barWidth(sharePercent: number): string {
    const share = (sharePercent / this.busiestShare()) * 100;

    if (!Number.isFinite(share) || share <= 0) {
      return '0%';
    }

    return `${Math.min(share, 100)}%`;
  }

  // --- Distribution labels -------------------------------------------------
  // On a broad slice the prices run into the millions, and "€1,250,000 – €1,437,500" is wider
  // than the column holding it — it used to spill across the bar and take the row with it.
  // Short form above a million, full number in the tooltip.
  priceLabel(price: number): string {
    if (!Number.isFinite(price)) {
      return UNREADABLE;
    }

    if (price < COMPACT_PRICE_FROM) {
      return `€${Math.round(price).toLocaleString('en-GB')}`;
    }

    // One decimal is enough to keep two neighbouring bars apart at this scale.
    return `€${(price / 1_000_000).toFixed(1)}M`;
  }

  bucketRange(fromPrice: number, toPrice: number): string {
    return `${this.priceLabel(fromPrice)} – ${this.priceLabel(toPrice)}`;
  }

  // The unabbreviated range, for the title attribute — the exact figures stay reachable.
  bucketRangeFull(fromPrice: number, toPrice: number): string {
    return `${fullPrice(fromPrice)} – ${fullPrice(toPrice)}`;
  }

  // --- Breakdown by the places inside the slice ----------------------------
  // The answer on a broad slice. One median over 4,000 listings across 18 districts is a number
  // about nothing; the rows below it are about places you can actually buy in.
  breakdownSort = signal<BreakdownSort>('pricePerM2');
  breakdownDirection = signal<SortDirection>('desc');
  showAllBreakdown = signal(false);

  breakdownLevel = computed<AreaLevel | null>(() => this.overview()?.breakdown?.level ?? null);

  // "18 distritos", "16 municípios" — says what the rows are before you read one.
  breakdownHeading = computed(() => {
    const level = this.breakdownLevel();

    if (level === null) {
      return '';
    }

    const label = placeLevelLabel(level);
    const count = this.overview()?.breakdown?.items.length ?? 0;

    return count === 1 ? `1 ${label.toLowerCase()}` : `${count} ${plural(label)}`;
  });

  sortedBreakdown = computed(() => {
    const rows = [...(this.overview()?.breakdown?.items ?? [])];
    const column = this.breakdownSort();
    const direction = this.breakdownDirection();

    rows.sort((a, b) => {
      let result = 0;

      switch (column) {
        case 'place':
          result = a.displayName.localeCompare(b.displayName, 'pt');
          break;

        case 'listings':
          result = a.listingCount - b.listingCount;
          break;

        case 'price':
          result = a.medianPrice - b.medianPrice;
          break;

        case 'area':
          result = a.medianAreaM2 - b.medianAreaM2;
          break;

        case 'pricePerM2':
          result = a.medianPricePerM2 - b.medianPricePerM2;
          break;

        case 'vsSlice':
          result = a.vsSlicePercent - b.vsSlicePercent;
          break;
      }

      return direction === 'desc' ? -result : result;
    });

    return rows;
  });

  // What the table actually renders: the top rows until you ask for the rest.
  visibleBreakdown = computed(() =>
    this.showAllBreakdown() ? this.sortedBreakdown() : this.sortedBreakdown().slice(0, BREAKDOWN_PREVIEW_ROWS)
  );

  hiddenBreakdownCount = computed(() =>
    Math.max(0, this.sortedBreakdown().length - this.visibleBreakdown().length)
  );

  toggleBreakdownSort(column: BreakdownSort): void {
    if (this.breakdownSort() === column) {
      this.breakdownDirection.set(this.breakdownDirection() === 'asc' ? 'desc' : 'asc');

      return;
    }

    this.breakdownSort.set(column);
    this.breakdownDirection.set(column === 'place' ? 'asc' : 'desc');
  }

  breakdownArrow(column: BreakdownSort): string {
    if (this.breakdownSort() !== column) {
      return '';
    }

    return this.breakdownDirection() === 'asc' ? ' ▲' : ' ▼';
  }

  // Too few listings in one place for its median to say much. Marked, never hidden — "there are
  // only four flats for sale in Alcoutim" is itself worth knowing.
  isThinPlace(row: MarketOverviewBreakdownItem): boolean {
    return row.listingCount < THIN_SLICE;
  }

  // Dearer or cheaper than the slice it belongs to. Neither is good or bad on its own, so this
  // only picks a direction — the colour says which way, not whether to buy.
  vsSliceClass(row: MarketOverviewBreakdownItem): string {
    if (row.vsSlicePercent > 5) {
      return 'above';
    }

    return row.vsSlicePercent < -5 ? 'below' : '';
  }

  // Jump straight from a breakdown row into that place, so the table doubles as navigation.
  drillInto(row: MarketOverviewBreakdownItem): void {
    const level = this.breakdownLevel();

    if (level === 'District') {
      this.form.district = row.district;
      this.onDistrictChange();

      return;
    }

    if (level === 'Municipality') {
      this.form.municipality = row.municipality;
      this.onMunicipalityChange();

      return;
    }

    this.form.town = row.town;
    this.load();
  }

  // Too few listings in one layout row for its median to mean much.
  isThinRow(listingCount: number): boolean {
    return listingCount < 3;
  }

  // --- By-typology table sorting -------------------------------------------
  // Client-side: one slice is a handful of rows, so re-sorting costs no request.
  typologySort = signal<TypologySort>('typology');
  typologyDirection = signal<SortDirection>('asc');

  sortedTypologies = computed(() => {
    const rows = [...(this.overview()?.typologies ?? [])];
    const column = this.typologySort();
    const direction = this.typologyDirection();

    rows.sort((a, b) => {
      let result = 0;

      switch (column) {
        case 'typology':
          result = typologyRooms(a.typology) - typologyRooms(b.typology);
          break;

        case 'listings':
          result = a.listingCount - b.listingCount;
          break;

        case 'price':
          result = a.medianPrice - b.medianPrice;
          break;

        case 'area':
          result = a.medianAreaM2 - b.medianAreaM2;
          break;

        case 'pricePerM2':
          result = a.medianPricePerM2 - b.medianPricePerM2;
          break;
      }

      return direction === 'desc' ? -result : result;
    });

    return rows;
  });

  toggleTypologySort(column: TypologySort): void {
    if (this.typologySort() === column) {
      this.typologyDirection.set(this.typologyDirection() === 'asc' ? 'desc' : 'asc');

      return;
    }

    this.typologySort.set(column);
    // Layouts read naturally smallest-first; the numbers start at their interesting end.
    this.typologyDirection.set(column === 'typology' ? 'asc' : 'desc');
  }

  typologyArrow(column: TypologySort): string {
    if (this.typologySort() !== column) {
      return '';
    }

    return this.typologyDirection() === 'asc' ? ' ▲' : ' ▼';
  }
}
