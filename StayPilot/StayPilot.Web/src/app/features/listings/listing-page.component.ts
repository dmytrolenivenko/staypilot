import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { PropertyListingService } from '../../core/services/property-listing.service';
import { RecentListingsService } from '../../core/services/recent-listings.service';
import { PropertyListingResponse } from '../../core/models/property-listing';
import { ListingDetailComponent } from './listing-detail.component';
import { InvestmentAnalysisComponent } from './investment-analysis.component';
import { PageHeaderComponent } from '../../shared/page-header.component';

/**
 * One listing, in full, at /listings/:id.
 *
 * This replaces two screens that both existed for the same reason: there was nowhere to put a
 * listing. "Look up by id" showed the facts and "Investment analysis" showed the numbers, and
 * both opened by asking you to type a listing id — a number nobody has, for a listing you can
 * only have found by browsing. Browse linked to the first of them, so the click-through was
 * there; what was missing was a page for it to land on.
 *
 * The id box is not gone, it has moved to the empty state: arrive without an id and the page
 * asks for one, which is exactly what the old lookup screen was.
 */
@Component({
  selector: 'app-listing-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    PageHeaderComponent,
    ListingDetailComponent,
    InvestmentAnalysisComponent
  ],
  templateUrl: './listing-page.component.html',
  styleUrl: './listing-page.component.css'
})
export class ListingPageComponent implements OnInit {
  idInput = signal<number | null>(null);
  listing = signal<PropertyListingResponse | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);

  // The id the analysis below is running for. Set only once the listing itself has loaded, so a
  // bad id shows one "no listing found" rather than that plus a second failure underneath it.
  analysisId = signal<number | null>(null);

  readonly emptyTitle = $localize`:@@listings.page.emptyTitle:Open a listing`;
  readonly listingSub = $localize`:@@listings.page.listingSub:Everything held about this listing, and what the numbers say about buying it.`;
  readonly emptySub = $localize`:@@listings.page.emptySub:Every listing has a page of its own. Browse to find one, or open it by id if you have one.`;
  readonly openLabel = $localize`:@@listings.page.open:Open`;
  readonly openingLabel = $localize`:@@listings.page.opening:Opening…`;

  constructor(
    private readonly propertyListingService: PropertyListingService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    readonly recentListings: RecentListingsService
  ) {}

  ngOnInit(): void {
    // The id is a route parameter now (/listings/1042), but ?id= is still honoured: it is what
    // the old lookup screen used, and links to it are out there in bookmarks and notes.
    this.route.paramMap.subscribe(params => {
      const id = Number(params.get('id') ?? this.route.snapshot.queryParamMap.get('id') ?? 0);

      if (id > 0) {
        this.load(id);
      }
    });
  }

  /** Typed an id into the empty state. Navigating keeps the URL and the page in step. */
  goToTyped(): void {
    const id = this.idInput();

    if (!id || id <= 0) {
      this.error.set($localize`:@@listings.page.error.invalidId:Enter a valid listing id.`);

      return;
    }

    this.router.navigate(['/listings', id]);
  }

  open(id: number): void {
    this.router.navigate(['/listings', id]);
  }

  private load(id: number): void {
    this.idInput.set(id);
    this.loading.set(true);
    this.error.set(null);
    this.listing.set(null);
    this.analysisId.set(null);

    this.propertyListingService.getById(id).subscribe({
      next: listing => {
        this.listing.set(listing);
        this.analysisId.set(listing.id);
        this.loading.set(false);
        this.recentListings.remember(listing.id);
      },
      error: err => {
        this.error.set(
          err.status === 404
            ? $localize`:@@listings.page.error.notFound:No listing found with id ${id}:id:.`
            : $localize`:@@listings.page.error.api:Could not reach the API.`
        );
        this.loading.set(false);
      }
    });
  }
}
