import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, switchMap } from 'rxjs';
import { MarketAreaService } from '../../core/services/market-area.service';
import { MarketArea } from '../../core/models/market-area';
import { PageHeaderComponent } from '../../shared/page-header.component';

// Page sizes offered in the dropdown. 200 is the biggest the API accepts.
const PAGE_SIZES = [20, 50, 100, 200];

@Component({
  selector: 'app-market-area-list',
  standalone: true,
  imports: [FormsModule, PageHeaderComponent],
  templateUrl: './market-area-list.component.html',
  styleUrl: './market-area-list.component.css'
})
export class MarketAreaListComponent implements OnInit {
  readonly pageSizes = PAGE_SIZES;

  areas = signal<MarketArea[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  // Filtering and paging both happen on the server now — there are thousands of
  // market areas, so we never hold the whole table in the browser.
  filterText = signal('');
  page = signal(1);
  pageSize = signal(20);
  totalRecords = signal(0);

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

  // First and last row number shown, for the "1–20 of 4472" hint.
  firstRow = computed(() => (this.totalRecords() === 0 ? 0 : (this.page() - 1) * this.pageSize() + 1));
  lastRow = computed(() => Math.min(this.page() * this.pageSize(), this.totalRecords()));

  // Every load goes through here, so a slow response for an old search can never
  // overwrite a newer one (switchMap drops the earlier request).
  private readonly loads = new Subject<void>();

  // Typing fires a load per keystroke, so the filter box goes through here first.
  // Page and page-size changes skip it — they should feel instant.
  private readonly searches = new Subject<void>();

  constructor(private readonly marketAreaService: MarketAreaService) {}

  ngOnInit(): void {
    this.searches.pipe(debounceTime(300)).subscribe(() => this.loads.next());

    this.loads
      .pipe(
        switchMap(() => {
          this.loading.set(true);
          this.error.set(null);
          return this.marketAreaService.getPage({
            search: this.filterText().trim() || undefined,
            pageNumber: this.page(),
            pageSize: this.pageSize()
          });
        })
      )
      .subscribe({
        next: response => {
          this.areas.set(response.items);
          this.totalRecords.set(response.totalRecords);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Could not load market areas — the API request failed. Check the Network tab for the actual URL and status.');
          this.loading.set(false);
        }
      });

    this.load();
  }

  // A new search always starts back on page 1, otherwise you can land past the last page.
  changeFilter(text: string): void {
    this.filterText.set(text);
    this.page.set(1);
    this.searches.next();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.page()) {
      return;
    }
    this.page.set(page);
    this.load();
  }

  changePageSize(size: number): void {
    this.pageSize.set(Number(size));
    this.page.set(1);
    this.load();
  }

  private load(): void {
    this.loads.next();
  }
}
