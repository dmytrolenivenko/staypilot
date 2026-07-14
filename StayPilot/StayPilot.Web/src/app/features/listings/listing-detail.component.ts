import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { PropertyListingResponse } from '../../core/models/property-listing';

@Component({
  selector: 'app-listing-detail',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './listing-detail.component.html',
  styleUrl: './listing-detail.component.css'
})
export class ListingDetailComponent {
  @Input({ required: true }) listing!: PropertyListingResponse;

  yesNo(value: boolean | null | undefined): string {
    if (value === null || value === undefined) {
      return 'Unknown';
    }
    return value ? 'Yes' : 'No';
  }
}
