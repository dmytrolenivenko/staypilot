import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ListingFilterService } from '../../core/services/listing-filter.service';
import { MarketAreaService } from '../../core/services/market-area.service';
import { FilterPropertyListingRequest } from '../../core/models/filter-property-listing';
import { PropertyListingResponse } from '../../core/models/property-listing';
import {
  LISTING_STATUSES,
  PROPERTY_CONDITIONS,
  PROPERTY_TYPES,
  TYPOLOGIES
} from '../../core/models/enums';

// The columns you can click to sort by.
type SortColumn =
  | 'id' | 'location' | 'type' | 'typology'
  | 'area' | 'price' | 'pricePerM2' | 'beach' | 'status';

// The on-screen filter form. '' = "Any" for dropdowns, null = empty for number boxes.
// We strip those out before sending, so the API only filters on what you actually typed.
interface FilterForm {
  district: string;
  municipality: string;
  town: string;
  zone: string;
  propertyType: string;
  typology: string;
  condition: string;
  listingStatus: string;
  minPrice: number | null;
  maxPrice: number | null;
  minAreaM2: number | null;
  maxAreaM2: number | null;
  maxPricePerM2: number | null;
  distanceToBeachMeters: number | null;
  hasGarage: boolean;
  hasSwimmingPool: boolean;
  hasSeaView: boolean;
  hasElevator: boolean;
}

function emptyForm(): FilterForm {
  return {
    district: '',
    municipality: '',
    town: '',
    zone: '',
    propertyType: '',
    typology: '',
    condition: '',
    listingStatus: '',
    minPrice: null,
    maxPrice: null,
    minAreaM2: null,
    maxAreaM2: null,
    maxPricePerM2: null,
    distanceToBeachMeters: null,
    hasGarage: false,
    hasSwimmingPool: false,
    hasSeaView: false,
    hasElevator: false
  };
}

@Component({
  selector: 'app-listing-browser',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './listing-browser.component.html',
  styleUrl: './listing-browser.component.css'
})
export class ListingBrowserComponent implements OnInit {
  // Dropdown options (reused from the enums file).
  readonly propertyTypes = PROPERTY_TYPES;
  readonly typologies = TYPOLOGIES;
  readonly conditions = PROPERTY_CONDITIONS;
  readonly listingStatuses = LISTING_STATUSES;

  // The dropdown choices, each level loaded from the backend as you pick the one above.
  districtOptions = signal<string[]>([]);
  municipalityOptions = signal<string[]>([]);
  townOptions = signal<string[]>([]);
  zoneOptions = signal<string[]>([]);

  // The editable filter form.
  form: FilterForm = emptyForm();

  // Everything the last Search downloaded (the full matching set).
  private allRows = signal<PropertyListingResponse[]>([]);

  // Request state.
  loading = signal(false);
  error = signal<string | null>(null);
  hasSearched = signal(false);
  capped = signal(false); // true when there were more than 1000 matches on the server

  // Client-side sorting (set by clicking a column header).
  sortColumn = signal<SortColumn | null>(null);
  sortDir = signal<'asc' | 'desc'>('asc');

  // Client-side paging.
  page = signal(1);
  pageSize = signal(20);

  // Rows after sorting — recomputed instantly whenever the sort changes.
  private sortedRows = computed(() => {
    const rows = [...this.allRows()];
    const col = this.sortColumn();
    if (!col) {
      return rows;
    }
    const dir = this.sortDir();
    return rows.sort((a, b) => this.compare(a, b, col, dir));
  });

  totalRecords = computed(() => this.sortedRows().length);

  totalPages = computed(() => Math.max(1, Math.ceil(this.totalRecords() / this.pageSize())));

  // Just the slice of rows shown on the current page.
  pagedRows = computed(() => {
    const start = (this.page() - 1) * this.pageSize();
    return this.sortedRows().slice(start, start + this.pageSize());
  });

  // The numbered page buttons to show: a sliding window of up to 7 around the current page
  // (e.g. current 5 of 20 -> 2 3 4 5 6 7 8). First/Last buttons jump to the ends.
  visiblePages = computed(() => {
    const total = this.totalPages();
    const current = this.page();
    const windowSize = 7;

    let start = Math.max(1, current - Math.floor(windowSize / 2));
    let end = Math.min(total, start + windowSize - 1);
    start = Math.max(1, end - windowSize + 1); // pull the window back if we hit the end

    const pages: number[] = [];
    for (let p = start; p <= end; p++) {
      pages.push(p);
    }
    return pages;
  });

  constructor(
    private readonly listingFilter: ListingFilterService,
    private readonly marketAreas: MarketAreaService
  ) {}

  // Load the area names once, when the page opens, for the location autocomplete.
    ngOnInit(): void {
    // Load the top dropdown (distritos). Nothing picked yet → backend returns districts.
    this.marketAreas.getOptions().subscribe({
      next: d => this.districtOptions.set(d),
      error: () => this.districtOptions.set([])
    });
  }

    // Distrito changed → wipe the child pickers and load this distrito's municípios.
  onDistrictChange(): void {
    this.form.municipality = '';
    this.form.town = '';
    this.form.zone = '';
    this.municipalityOptions.set([]);
    this.townOptions.set([]);
    this.zoneOptions.set([]);
    if (this.form.district) {
      this.marketAreas.getOptions(this.form.district).subscribe({
        next: m => this.municipalityOptions.set(m),
        error: () => this.municipalityOptions.set([])
      });
    }
  }

    // Município changed → wipe the child pickers and load this município's freguesias.
  onMunicipalityChange(): void {
    this.form.town = '';
    this.form.zone = '';
    this.townOptions.set([]);
    this.zoneOptions.set([]);
    if (this.form.municipality) {
      this.marketAreas.getOptions(this.form.district, this.form.municipality).subscribe({
        next: t => this.townOptions.set(t),
        error: () => this.townOptions.set([])
      });
    }
  }

    // Freguesia changed → wipe zona and load this freguesia's zonas.
  onTownChange(): void {
    this.form.zone = '';
    this.zoneOptions.set([]);
    if (this.form.town) {
      this.marketAreas.getOptions(this.form.district, this.form.municipality, this.form.town).subscribe({
        next: z => this.zoneOptions.set(z),
        error: () => this.zoneOptions.set([])
      });
    }
  }

  // --- Buttons -------------------------------------------------------------

  // "Search" — the only thing that calls the API. Downloads the whole matching set
  // (across as many pages of 20 as it takes), then we sort + page it in the browser.
  search(): void {
    this.loading.set(true);
    this.error.set(null);
    this.hasSearched.set(true);
    this.page.set(1);

    this.listingFilter.filterAll(this.buildRequest()).subscribe({
      next: result => {
        this.allRows.set(result.items);
        this.capped.set(result.capped);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not reach the API.');
        this.allRows.set([]);
        this.capped.set(false);
        this.loading.set(false);
      }
    });
  }

  reset(): void {
    this.form = emptyForm();
    this.municipalityOptions.set([]);
    this.townOptions.set([]);
    this.zoneOptions.set([]);
    this.allRows.set([]);
    this.sortColumn.set(null);
    this.page.set(1);
    this.hasSearched.set(false);
    this.error.set(null);
    this.capped.set(false);
  }

  // --- Client-side sorting (no API call) -----------------------------------

  // Click a header: first click sorts ascending, click the same one again to flip.
  toggleSort(column: SortColumn): void {
    if (this.sortColumn() === column) {
      this.sortDir.set(this.sortDir() === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortColumn.set(column);
      this.sortDir.set('asc');
    }
    this.page.set(1);
  }

  // The little arrow shown next to the active column header.
  arrow(column: SortColumn): string {
    if (this.sortColumn() !== column) {
      return '';
    }
    return this.sortDir() === 'asc' ? ' ▲' : ' ▼'; // ▲ / ▼
  }

  // --- Client-side paging (no API call) ------------------------------------

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages()) {
      return;
    }
    this.page.set(page);
  }

  changePageSize(size: number): void {
    this.pageSize.set(size);
    this.page.set(1);
  }

  // --- Helpers -------------------------------------------------------------

  // The value a given column sorts on. Returns a number, a string, or null (missing).
  private sortValue(item: PropertyListingResponse, column: SortColumn): number | string | null {
    switch (column) {
      case 'id': return item.id;
      case 'location': return (item.marketAreaTown || item.marketAreaMunicipality || '').toLowerCase();
      case 'type': return item.propertyType.toLowerCase();
      case 'typology': return this.typologies.indexOf(item.typology); // keeps T2 before T10
      case 'area': return item.areaM2;
      case 'price': return item.listingSnapshot?.price ?? null;
      case 'pricePerM2': return item.listingSnapshot?.pricePerM2 ?? null;
      case 'beach': return item.distanceToBeachMeters ?? null;
      case 'status': return (item.listingSnapshot?.status ?? '').toLowerCase();
    }
  }

  private compare(
    a: PropertyListingResponse,
    b: PropertyListingResponse,
    column: SortColumn,
    dir: 'asc' | 'desc'
  ): number {
    const va = this.sortValue(a, column);
    const vb = this.sortValue(b, column);

    // Missing values always sink to the bottom, regardless of direction.
    if (va == null && vb == null) return 0;
    if (va == null) return 1;
    if (vb == null) return -1;

    const cmp = va < vb ? -1 : va > vb ? 1 : 0;
    return dir === 'asc' ? cmp : -cmp;
  }

  // Turns the on-screen form into the API request, dropping any blank field.
  // pageNumber/pageSize here are placeholders — the service overrides them as it walks
  // the pages of 20. Sorting is done in the browser, so sortBy is just a fixed default.
  private buildRequest(): FilterPropertyListingRequest {
    const f = this.form;
    const request: FilterPropertyListingRequest = {
      sortBy: 'Id',
      sortDescending: false,
      pageNumber: 1,
      pageSize: 20
    };

    if (f.district) request.district = f.district;
    if (f.municipality) request.municipality = f.municipality;
    if (f.town) request.town = f.town;
    if (f.zone) request.zone = f.zone;
    if (f.propertyType) request.propertyType = f.propertyType as any;
    if (f.typology) request.typology = f.typology as any;
    if (f.condition) request.condition = f.condition as any;
    if (f.listingStatus) request.listingStatus = f.listingStatus as any;

    if (f.minPrice != null) request.minPrice = f.minPrice;
    if (f.maxPrice != null) request.maxPrice = f.maxPrice;
    if (f.minAreaM2 != null) request.minAreaM2 = f.minAreaM2;
    if (f.maxAreaM2 != null) request.maxAreaM2 = f.maxAreaM2;
    if (f.maxPricePerM2 != null) request.maxPricePerM2 = f.maxPricePerM2;
    if (f.distanceToBeachMeters != null) request.distanceToBeachMeters = f.distanceToBeachMeters;

    // Checkboxes only become a filter when ticked (ticked = "must have it").
    if (f.hasGarage) request.hasGarage = true;
    if (f.hasSwimmingPool) request.hasSwimmingPool = true;
    if (f.hasSeaView) request.hasSeaView = true;
    if (f.hasElevator) request.hasElevator = true;

    return request;
  }
}
