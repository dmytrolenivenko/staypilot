import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MarketAreaService } from '../../core/services/market-area.service';
import { PropertyListingService } from '../../core/services/property-listing.service';
import { RecentListingsService } from '../../core/services/recent-listings.service';
import { MarketArea } from '../../core/models/market-area';
import { PropertyListingRequest } from '../../core/models/property-listing';
import {
  LISTING_STATUSES,
  PROPERTY_CONDITIONS,
  PROPERTY_TYPES,
  TYPOLOGIES
} from '../../core/models/enums';

type TriState = 'unknown' | 'yes' | 'no';

function triStateToBoolean(value: TriState): boolean | null {
  if (value === 'yes') return true;
  if (value === 'no') return false;
  return null;
}

function todayIsoDate(): string {
  return new Date().toISOString().slice(0, 10);
}

@Component({
  selector: 'app-listing-create',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './listing-create.component.html',
  styleUrl: './listing-create.component.css'
})
export class ListingCreateComponent implements OnInit {
  propertyTypes = PROPERTY_TYPES;
  typologies = TYPOLOGIES;
  conditions = PROPERTY_CONDITIONS;
  statuses = LISTING_STATUSES;

  marketAreas = signal<MarketArea[]>([]);
  submitting = signal(false);
  error = signal<string | null>(null);
  createdId = signal<number | null>(null);

  private readonly fb = inject(FormBuilder);

  form = this.fb.group({
    marketAreaId: [null as number | null, Validators.required],
    propertyType: ['Apartment', Validators.required],
    typology: ['T2', Validators.required],
    sourceName: ['Idealista'],
    sourceUrl: ['', Validators.required],
    areaM2: [80, [Validators.required, Validators.min(1)]],
    bathrooms: [1, [Validators.required, Validators.min(0)]],
    floor: [null as number | null],
    totalFloors: [null as number | null],
    hasElevator: ['unknown' as TriState],
    hasAirConditioning: ['unknown' as TriState],
    condition: ['Used', Validators.required],
    constructionYear: [null as number | null],
    renovationYear: [null as number | null],
    balconyCount: [0],
    hasTerrace: [false],
    hasGarage: [false],
    hasParking: [false],
    hasSwimmingPool: [false],
    isFurnished: [false],
    hasSeaView: [false],
    hasCityView: [false],
    // The API requires both (it computes nearest-beach distance from them) even though the DTO
    // itself marks them optional — a service-level business rule, not a DTO validation attribute.
    latitude: [null as number | null, Validators.required],
    longitude: [null as number | null, Validators.required],
    energyCertificate: [''],
    notes: [''],
    price: [0, [Validators.required, Validators.min(1)]],
    pricePerM2: [0],
    status: ['Active', Validators.required],
    snapshotDate: [todayIsoDate(), Validators.required]
  });

  constructor(
    private readonly marketAreaService: MarketAreaService,
    private readonly propertyListingService: PropertyListingService,
    private readonly recentListings: RecentListingsService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.marketAreaService.getAll().subscribe(areas => this.marketAreas.set(areas));
  }

  recalculatePricePerM2(): void {
    const price = this.form.value.price ?? 0;
    const area = this.form.value.areaM2 ?? 0;
    if (price > 0 && area > 0) {
      this.form.patchValue({ pricePerM2: Math.round((price / area) * 100) / 100 });
    }
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.error.set('Please fill in the required fields (marked *).');
      return;
    }

    const v = this.form.getRawValue();
    const request: PropertyListingRequest = {
      marketAreaId: v.marketAreaId,
      propertyType: v.propertyType as PropertyListingRequest['propertyType'],
      typology: v.typology as PropertyListingRequest['typology'],
      sourceName: v.sourceName ?? '',
      sourceUrl: v.sourceUrl ?? '',
      areaM2: v.areaM2 ?? 0,
      bathrooms: v.bathrooms ?? 0,
      floor: v.floor,
      totalFloors: v.totalFloors,
      hasElevator: triStateToBoolean(v.hasElevator as TriState),
      hasAirConditioning: triStateToBoolean(v.hasAirConditioning as TriState),
      condition: v.condition as PropertyListingRequest['condition'],
      constructionYear: v.constructionYear,
      renovationYear: v.renovationYear,
      balconyCount: v.balconyCount ?? 0,
      hasTerrace: !!v.hasTerrace,
      hasGarage: !!v.hasGarage,
      hasParking: !!v.hasParking,
      hasSwimmingPool: !!v.hasSwimmingPool,
      isFurnished: !!v.isFurnished,
      hasSeaView: !!v.hasSeaView,
      hasCityView: !!v.hasCityView,
      latitude: v.latitude,
      longitude: v.longitude,
      energyCertificate: v.energyCertificate || null,
      notes: v.notes || null,
      listingSnapshot: {
        propertyListingId: 0,
        price: v.price ?? 0,
        pricePerM2: v.pricePerM2 ?? 0,
        status: v.status as PropertyListingRequest['listingSnapshot']['status'],
        snapshotDateUtc: new Date(v.snapshotDate + 'T00:00:00Z').toISOString()
      }
    };

    this.submitting.set(true);
    this.error.set(null);

    this.propertyListingService.create(request).subscribe({
      next: created => {
        this.submitting.set(false);
        this.createdId.set(created.id);
        this.recentListings.remember(created.id);
      },
      error: err => {
        this.submitting.set(false);
        this.error.set(
          err.status === 400
            ? 'The API rejected this listing (bad market area id, or a validation error) — check the fields and try again.'
            : 'Could not reach the API.'
        );
      }
    });
  }

  viewCreated(): void {
    const id = this.createdId();
    if (id) {
      this.router.navigate(['/listings/lookup'], { queryParams: { id } });
    }
  }
}
