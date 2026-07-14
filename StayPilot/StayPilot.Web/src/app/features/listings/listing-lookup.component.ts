import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { PropertyListingService } from '../../core/services/property-listing.service';
import { RecentListingsService } from '../../core/services/recent-listings.service';
import { PropertyListingResponse } from '../../core/models/property-listing';
import { ListingDetailComponent } from './listing-detail.component';

@Component({
  selector: 'app-listing-lookup',
  standalone: true,
  imports: [FormsModule, ListingDetailComponent],
  templateUrl: './listing-lookup.component.html',
  styleUrl: './listing-lookup.component.css'
})
export class ListingLookupComponent implements OnInit {
  idInput = signal<number | null>(null);
  listing = signal<PropertyListingResponse | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);

  constructor(
    private readonly propertyListingService: PropertyListingService,
    private readonly route: ActivatedRoute,
    readonly recentListings: RecentListingsService
  ) {}

  ngOnInit(): void {
    const idParam = this.route.snapshot.queryParamMap.get('id');
    if (idParam) {
      const id = Number(idParam);
      if (id > 0) {
        this.lookup(id);
      }
    }
  }

  lookup(id?: number): void {
    const targetId = id ?? this.idInput();
    if (!targetId || targetId <= 0) {
      this.error.set('Enter a valid listing id.');
      return;
    }
    this.idInput.set(targetId);

    this.loading.set(true);
    this.error.set(null);
    this.listing.set(null);

    this.propertyListingService.getById(targetId).subscribe({
      next: listing => {
        this.listing.set(listing);
        this.loading.set(false);
        this.recentListings.remember(listing.id);
      },
      error: err => {
        this.error.set(err.status === 404 ? `No listing found with id ${targetId}.` : 'Could not reach the API.');
        this.loading.set(false);
      }
    });
  }
}
