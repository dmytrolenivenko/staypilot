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

  get propertyFacts(): { label: string; value: string }[] {
    const l = this.listing;
    const facts: { label: string; value: string }[] = [];
    if (l.propertyType) facts.push({ label: 'Type', value: l.propertyType });
    if (l.typology) facts.push({ label: 'Typology', value: l.typology });
    if (l.areaM2) facts.push({ label: 'Area', value: `${l.areaM2} m²` });
    if (l.bathrooms) facts.push({ label: 'Bathrooms', value: `${l.bathrooms}` });
    if (l.floor || l.totalFloors) {
      facts.push({ label: 'Floor', value: `${l.floor ?? '—'} / ${l.totalFloors ?? '—'}` });
    }
    if (l.condition) facts.push({ label: 'Condition', value: l.condition });
    if (l.constructionYear) facts.push({ label: 'Construction year', value: `${l.constructionYear}` });
    if (l.renovationYear) facts.push({ label: 'Renovation year', value: `${l.renovationYear}` });
    if (l.energyCertificate) facts.push({ label: 'Energy certificate', value: l.energyCertificate });
    return facts;
  }

  get presentFeatures(): string[] {
    const l = this.listing;
    const features: string[] = [];
    if (l.hasElevator) features.push('Elevator');
    if (l.hasAirConditioning) features.push('Air conditioning');
    if (l.hasGarage) features.push('Garage');
    if (l.hasParking) features.push('Parking');
    if (l.hasTerrace) features.push('Terrace');
    if (l.balconyCount) features.push(l.balconyCount === 1 ? '1 balcony' : `${l.balconyCount} balconies`);
    if (l.hasSwimmingPool) features.push('Swimming pool');
    if (l.isFurnished) features.push('Furnished');
    if (l.hasSeaView) features.push('Sea view');
    if (l.hasCityView) features.push('City view');
    return features;
  }
}
