import { CommonModule } from '@angular/common';
import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ListingSnapshotService } from '../../core/services/listing-snapshot.service';
import { ListingSnapshotRequest, ListingSnapshotResponse } from '../../core/models/property-listing';
import { LISTING_STATUSES, ListingStatus } from '../../core/models/enums';

// Price/status snapshots for a listing. The API stores one snapshot per observation,
// but only exposes "get the snapshot for a property" (single) + "create a snapshot".
// So this screen looks up the current snapshot for a property id and lets you record a new one.
@Component({
  selector: 'app-snapshots',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './snapshots.component.html',
  styleUrl: './snapshots.component.css'
})
export class SnapshotsComponent {
  readonly statuses = LISTING_STATUSES;

  propertyIdInput = signal<number | null>(null);
  current = signal<ListingSnapshotResponse | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);
  saved = signal(false);

  // The "record a new snapshot" form.
  form: {
    price: number | null;
    pricePerM2: number | null;
    status: ListingStatus;
    snapshotDate: string;
  } = { price: null, pricePerM2: null, status: 'Active', snapshotDate: this.today() };

  constructor(private readonly snapshots: ListingSnapshotService) {}

  private today(): string {
    return new Date().toISOString().slice(0, 10);
  }

  load(): void {
    const id = this.propertyIdInput();
    if (!id || id <= 0) {
      this.error.set('Enter a valid property id.');
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    this.current.set(null);
    this.saved.set(false);

    this.snapshots.getByPropertyId(id).subscribe({
      next: snap => {
        this.current.set(snap);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(
          err.status === 404 || err.status === 400
            ? `No snapshot found for property #${id} yet.`
            : 'Could not reach the API.'
        );
        this.loading.set(false);
      }
    });
  }

  save(): void {
    const id = this.propertyIdInput();
    if (!id || id <= 0) {
      this.error.set('Enter the property id this snapshot belongs to.');
      return;
    }
    if (this.form.price == null || this.form.pricePerM2 == null) {
      this.error.set('Price and price per m² are required.');
      return;
    }

    const request: ListingSnapshotRequest = {
      propertyListingId: id,
      price: this.form.price,
      pricePerM2: this.form.pricePerM2,
      status: this.form.status,
      snapshotDateUtc: new Date(this.form.snapshotDate).toISOString()
    };

    this.loading.set(true);
    this.error.set(null);
    this.saved.set(false);

    this.snapshots.create(request).subscribe({
      next: snap => {
        this.current.set(snap);
        this.saved.set(true);
        this.loading.set(false);
        this.form.price = null;
        this.form.pricePerM2 = null;
      },
      error: () => {
        this.error.set('Could not save the snapshot.');
        this.loading.set(false);
      }
    });
  }
}
