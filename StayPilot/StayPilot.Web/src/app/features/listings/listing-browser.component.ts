import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { Subscription } from 'rxjs';
import { ListingFilterService, PAGE_SIZE_CHOICES } from '../../core/services/listing-filter.service';
import { MarketAreaService } from '../../core/services/market-area.service';
import { FilterPropertyListingRequest } from '../../core/models/filter-property-listing';
import { PropertyListingResponse } from '../../core/models/property-listing';
import {
  LISTING_STATUS_OPTIONS,
  PROPERTY_CONDITION_OPTIONS,
  PROPERTY_TYPES,
  SortBy,
  TYPOLOGIES
} from '../../core/models/enums';
import { apiErrorMessage } from '../../core/api-error';
import { clickedRowControl } from '../../shared/row-click';

// The columns you can click to sort by, and the API field each one means. Every column here
// maps to a real SortBy value, so a click reorders the whole matching set on the server — not
// whatever slice of it the browser happens to be holding.
type SortColumn =
  | 'id' | 'location' | 'type' | 'typology'
  | 'area' | 'price' | 'pricePerM2' | 'beach' | 'status';

const SORT_FIELDS: Record<SortColumn, SortBy> = {
  id: 'Id',
  location: 'Location',
  type: 'PropertyType',
  typology: 'Typology',
  area: 'AreaM2',
  price: 'Price',
  pricePerM2: 'PricePerM2',
  beach: 'DistanceToBeachMeters',
  status: 'ListingStatus'
};

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

// Listing browser — filter, sort and page every listing the API holds.
//
// Sorting and paging are the server's job here. They used to be the browser's: a search pulled
// every matching row (page 1, then pages 2..N in parallel) and sorted the pile locally. The API
// would not serve more than 1,000 rows, so a search of a real distrito silently kept the first
// 1,000 by Id and ranked only those — and the header called that number the total. Asking the
// server for one page of an ordered set is both correct and one request instead of fifty.
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
  readonly conditions = PROPERTY_CONDITION_OPTIONS;
  readonly listingStatuses = LISTING_STATUS_OPTIONS;
  readonly pageSizes = PAGE_SIZE_CHOICES;

  // The dropdown choices, each level loaded from the backend as you pick the one above.
  districtOptions = signal<string[]>([]);
  municipalityOptions = signal<string[]>([]);
  townOptions = signal<string[]>([]);
  zoneOptions = signal<string[]>([]);

  // The editable filter form.
  form: FilterForm = emptyForm();

  // The current page of results, exactly as the server ordered it.
  private rows = signal<PropertyListingResponse[]>([]);

  // Request state.
  loading = signal(false);
  error = signal<string | null>(null);
  hasSearched = signal(false);

  // How many listings match the filters, across every page. The server's own count, taken
  // before paging — so it is the true total, never the size of what we are holding.
  totalRecords = signal(0);

  // Sorting and paging. Changing any of them asks the server again, because all three decide
  // which rows come back rather than how the ones we have are arranged.
  sortColumn = signal<SortColumn>('id');
  sortDir = signal<'asc' | 'desc'>('asc');
  page = signal(1);
  pageSize = signal(20);

  // The filters the rows on screen were actually fetched with. Editing a box changes `form`
  // but not this, so re-sorting or paging keeps answering the search you ran rather than
  // quietly switching to a half-typed one.
  private activeFilters: FilterPropertyListingRequest | null = null;

  // Sorting and paging fire a request each, and clicking twice quickly leaves two open whose
  // answers can land out of order — the older one last, showing the wrong page. Cancelling the
  // open one means the newest request is always the one that renders.
  private inFlight?: Subscription;

  // The rows of the current page. Named as it was when paging was done here, so the template
  // did not have to change with the mechanism behind it.
  pagedRows = computed(() => this.rows());

  totalPages = computed(() => Math.max(1, Math.ceil(this.totalRecords() / this.pageSize())));

  // The numbered page buttons to show: a sliding window of up to 7 around the current page
  // (e.g. current 5 of 20 -> 2 3 4 5 6 7 8). First/Last buttons jump to the ends.
  visiblePages = computed(() => {
    const total = this.totalPages();
    const current = this.page();
    const windowSize = 7;

    let start = Math.max(1, current - Math.floor(windowSize / 2));
    const end = Math.min(total, start + windowSize - 1);
    start = Math.max(1, end - windowSize + 1); // pull the window back if we hit the end

    const pages: number[] = [];
    for (let p = start; p <= end; p++) {
      pages.push(p);
    }
    return pages;
  });

  constructor(
    private readonly listingFilter: ListingFilterService,
    private readonly marketAreas: MarketAreaService,
    private readonly router: Router
  ) {}

  // A click anywhere on a result row opens that listing, not only its #id link.
  openListing(id: number, event: MouseEvent): void {
    if (clickedRowControl(event)) {
      return;
    }

    this.router.navigate(['/listings', id]);
  }

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

  // "Search" — reads the form, freezes it as the active filters, and asks for page 1.
  search(): void {
    this.activeFilters = this.buildFilters();
    this.hasSearched.set(true);
    this.page.set(1);
    this.fetch();
  }

  reset(): void {
    this.inFlight?.unsubscribe();
    this.form = emptyForm();
    this.municipalityOptions.set([]);
    this.townOptions.set([]);
    this.zoneOptions.set([]);
    this.rows.set([]);
    this.activeFilters = null;
    this.sortColumn.set('id');
    this.sortDir.set('asc');
    this.page.set(1);
    this.totalRecords.set(0);
    this.hasSearched.set(false);
    this.error.set(null);
    this.loading.set(false);
  }

  // --- Sorting -------------------------------------------------------------

  // Click a header: first click sorts ascending, click the same one again to flip. Either way
  // it goes back to page 1 and asks the server, because the row that is now first may be on a
  // page we are not holding.
  toggleSort(column: SortColumn): void {
    if (this.sortColumn() === column) {
      this.sortDir.set(this.sortDir() === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortColumn.set(column);
      this.sortDir.set('asc');
    }
    this.page.set(1);
    this.fetch();
  }

  // The little arrow shown next to the active column header.
  arrow(column: SortColumn): string {
    if (this.sortColumn() !== column) {
      return '';
    }
    return this.sortDir() === 'asc' ? ' ▲' : ' ▼'; // ▲ / ▼
  }

  // The status as the reader sees it; the wire value stays for anything that compares it.
  statusLabel(status: string): string {
    return this.listingStatuses.find(option => option.value === status)?.label ?? status;
  }

  // --- Paging --------------------------------------------------------------

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.page()) {
      return;
    }
    this.page.set(page);
    this.fetch();
  }

  changePageSize(size: number): void {
    this.pageSize.set(Number(size));
    this.page.set(1);
    this.fetch();
  }

  // --- Fetching ------------------------------------------------------------

  // One page of the current search, ordered by the current column. Everything that changes
  // what the server should return comes through here.
  private fetch(): void {
    if (!this.activeFilters) {
      return;
    }

    this.inFlight?.unsubscribe();
    this.loading.set(true);
    this.error.set(null);

    const request: FilterPropertyListingRequest = {
      ...this.activeFilters,
      sortBy: SORT_FIELDS[this.sortColumn()],
      sortDescending: this.sortDir() === 'desc',
      pageNumber: this.page(),
      pageSize: this.pageSize()
    };

    this.inFlight = this.listingFilter.filterPage(request).subscribe({
      next: response => {
        this.rows.set(response.items);
        this.totalRecords.set(response.totalRecords);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.rows.set([]);
        this.totalRecords.set(0);
        this.error.set(apiErrorMessage(err, $localize`:@@listings.browser.error.api:Could not reach the API.`));
        this.loading.set(false);
      }
    });
  }

  // Turns the on-screen form into the filter half of the API request, dropping any blank
  // field. Sorting and paging are added per call in fetch(), because they change without the
  // filters changing.
  private buildFilters(): FilterPropertyListingRequest {
    const f = this.form;
    const request: FilterPropertyListingRequest = {
      sortBy: 'Id',
      sortDescending: false,
      pageNumber: 1,
      pageSize: this.pageSize()
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
