import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PropertyListingResponse } from '../../core/models/property-listing';
import { LISTING_STATUS_OPTIONS, PROPERTY_CONDITION_OPTIONS, PropertyType } from '../../core/models/enums';

// What the reader sees for each property type; the wire value is what the API sends.
const PROPERTY_TYPE_LABELS: Record<PropertyType, string> = {
  Apartment: $localize`:@@listings.detail.type.apartment:Apartment`,
  Villa: $localize`:@@listings.detail.type.villa:Villa`,
  House: $localize`:@@listings.detail.type.house:House`,
  Land: $localize`:@@listings.detail.type.land:Land`
};

@Component({
  selector: 'app-listing-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './listing-detail.component.html',
  styleUrl: './listing-detail.component.css'
})
export class ListingDetailComponent {
  @Input({ required: true }) listing!: PropertyListingResponse;

  readonly sourceFallback = $localize`:@@listings.detail.source:Source`;

  statusLabel(status: string): string {
    return LISTING_STATUS_OPTIONS.find(option => option.value === status)?.label ?? status;
  }

  get propertyFacts(): { label: string; value: string }[] {
    const l = this.listing;
    const facts: { label: string; value: string }[] = [];
    if (l.propertyType) {
      facts.push({ label: $localize`:@@listings.detail.fact.type:Type`, value: PROPERTY_TYPE_LABELS[l.propertyType] ?? l.propertyType });
    }
    if (l.typology) facts.push({ label: $localize`:@@listings.detail.fact.typology:Typology`, value: l.typology });
    if (l.areaM2) facts.push({ label: $localize`:@@listings.detail.fact.area:Area`, value: `${l.areaM2} m²` });
    if (l.bathrooms) facts.push({ label: $localize`:@@listings.detail.fact.bathrooms:Bathrooms`, value: `${l.bathrooms}` });
    if (l.floor || l.totalFloors) {
      facts.push({ label: $localize`:@@listings.detail.fact.floor:Floor`, value: `${l.floor ?? '—'} / ${l.totalFloors ?? '—'}` });
    }
    if (l.condition) {
      const condition = PROPERTY_CONDITION_OPTIONS.find(option => option.value === l.condition)?.label ?? l.condition;
      facts.push({ label: $localize`:@@listings.detail.fact.condition:Condition`, value: condition });
    }
    if (l.constructionYear) {
      facts.push({ label: $localize`:@@listings.detail.fact.constructionYear:Construction year`, value: `${l.constructionYear}` });
    }
    if (l.renovationYear) {
      facts.push({ label: $localize`:@@listings.detail.fact.renovationYear:Renovation year`, value: `${l.renovationYear}` });
    }
    if (l.energyCertificate) {
      facts.push({ label: $localize`:@@listings.detail.fact.energyCertificate:Energy certificate`, value: l.energyCertificate });
    }
    return facts;
  }

  get presentFeatures(): string[] {
    const l = this.listing;
    const features: string[] = [];
    if (l.hasElevator) features.push($localize`:@@listings.detail.feature.elevator:Elevator`);
    if (l.hasAirConditioning) features.push($localize`:@@listings.detail.feature.airConditioning:Air conditioning`);
    if (l.hasGarage) features.push($localize`:@@listings.detail.feature.garage:Garage`);
    if (l.hasParking) features.push($localize`:@@listings.detail.feature.parking:Parking`);
    if (l.hasTerrace) features.push($localize`:@@listings.detail.feature.terrace:Terrace`);
    if (l.balconyCount) {
      features.push(
        l.balconyCount === 1
          ? $localize`:@@listings.detail.feature.balcony:1 balcony`
          : $localize`:@@listings.detail.feature.balconies:${l.balconyCount}:count: balconies`
      );
    }
    if (l.hasSwimmingPool) features.push($localize`:@@listings.detail.feature.pool:Swimming pool`);
    if (l.isFurnished) features.push($localize`:@@listings.detail.feature.furnished:Furnished`);
    if (l.hasSeaView) features.push($localize`:@@listings.detail.feature.seaView:Sea view`);
    if (l.hasCityView) features.push($localize`:@@listings.detail.feature.cityView:City view`);
    return features;
  }
}
