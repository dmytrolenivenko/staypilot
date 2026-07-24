import { CommonModule } from '@angular/common';
import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { OwnedPropertyService } from '../../core/services/owned-property.service';
import { OwnedPropertyRequest, OwnedPropertyResponse } from '../../core/models/owned-property';
import {
  PROPERTY_CONDITIONS,
  PROPERTY_TYPES,
  TYPOLOGIES
} from '../../core/models/enums';

// My Properties — CRUD over the developer's own apartments (OwnedProperty).
// These are the properties the future "valuation vs. comparables" feature will value;
// that estimate endpoint isn't exposed by the API yet, so this screen manages the data only.
@Component({
  selector: 'app-owned-properties',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './owned-properties.component.html',
  styleUrl: './owned-properties.component.css'
})
export class OwnedPropertiesComponent {
  readonly propertyTypes = PROPERTY_TYPES;
  readonly typologies = TYPOLOGIES;
  readonly conditions = PROPERTY_CONDITIONS;

  idInput = signal<number | null>(null);
  current = signal<OwnedPropertyResponse | null>(null);

  loading = signal(false);
  error = signal<string | null>(null);
  message = signal<string | null>(null);

  // null = the form is creating a new property; a number = editing that id.
  editingId = signal<number | null>(null);

  form: OwnedPropertyRequest = this.emptyForm();

  constructor(private readonly service: OwnedPropertyService) {}

  private emptyForm(): OwnedPropertyRequest {
    return {
      name: '',
      country: 'Portugal',
      district: '',
      municipality: '',
      town: '',
      zone: null,
      purchasePrice: null,
      purchaseDate: null,
      propertyType: 'Apartment',
      typology: 'T1',
      areaM2: 0,
      bathrooms: 0,
      floor: null,
      totalFloors: null,
      condition: 'Unknown',
      constructionYear: null,
      renovationYear: null,
      renovationInvestment: null,
      latitude: null,
      longitude: null,
      hasElevator: false,
      hasAirConditioning: false,
      hasGarage: false,
      hasParking: false,
      hasTerrace: false,
      hasSwimmingPool: false,
      isFurnished: false,
      hasSeaView: false,
      hasCityView: false,
      energyCertificate: null,
      notes: null
    };
  }

  // --- Look up an existing property and load it into the form for editing ----
  lookup(): void {
    const id = this.idInput();
    if (!id || id <= 0) {
      this.error.set('Enter a valid property id.');
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    this.message.set(null);

    this.service.get(id).subscribe({
      next: prop => {
        this.current.set(prop);
        this.fillFormFrom(prop);
        this.editingId.set(prop.id);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err.status === 404 ? `No property found with id ${id}.` : 'Could not reach the API.');
        this.current.set(null);
        this.loading.set(false);
      }
    });
  }

  private fillFormFrom(p: OwnedPropertyResponse): void {
    this.form = {
      name: p.name,
      country: 'Portugal',
      district: '',
      municipality: '',
      town: '',
      zone: null,
      purchasePrice: p.purchasePrice ?? null,
      purchaseDate: p.purchaseDate ? p.purchaseDate.slice(0, 10) : null,
      propertyType: p.propertyType,
      typology: p.typology,
      areaM2: p.areaM2,
      bathrooms: p.bathrooms,
      floor: p.floor ?? null,
      totalFloors: p.totalFloors ?? null,
      condition: p.condition ?? 'Unknown',
      constructionYear: p.constructionYear ?? null,
      renovationYear: p.renovationYear ?? null,
      renovationInvestment: p.renovationInvestment ?? null,
      latitude: null,
      longitude: null,
      hasElevator: p.hasElevator ?? false,
      hasAirConditioning: p.hasAirConditioning ?? false,
      hasGarage: p.hasGarage ?? false,
      hasParking: p.hasParking ?? false,
      hasTerrace: p.hasTerrace ?? false,
      hasSwimmingPool: p.hasSwimmingPool ?? false,
      isFurnished: p.isFurnished ?? false,
      hasSeaView: p.hasSeaView ?? false,
      hasCityView: p.hasCityView ?? false,
      energyCertificate: p.energyCertificate ?? null,
      notes: p.notes ?? null
    };
  }

  // --- Create / update -----------------------------------------------------
  newProperty(): void {
    this.form = this.emptyForm();
    this.editingId.set(null);
    this.current.set(null);
    this.error.set(null);
    this.message.set(null);
    this.idInput.set(null);
  }

  save(): void {
    if (!this.form.name.trim()) {
      this.error.set('Name is required.');
      return;
    }
    if (!this.form.areaM2 || this.form.areaM2 < 1) {
      this.error.set('Area (m²) must be at least 1.');
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.message.set(null);

    const editing = this.editingId();
    const call = editing
      ? this.service.update(editing, this.form)
      : this.service.create(this.form);

    call.subscribe({
      next: prop => {
        this.current.set(prop);
        this.editingId.set(prop.id);
        this.idInput.set(prop.id);
        this.message.set(editing ? `Property #${prop.id} updated.` : `Property #${prop.id} created.`);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not save the property. Check the required fields.');
        this.loading.set(false);
      }
    });
  }

  // --- Delete --------------------------------------------------------------
  remove(): void {
    const id = this.editingId();
    if (!id) {
      return;
    }
    if (!confirm(`Delete property #${id}? This cannot be undone.`)) {
      return;
    }
    this.loading.set(true);
    this.error.set(null);

    this.service.delete(id).subscribe({
      next: () => {
        this.message.set(`Property #${id} deleted.`);
        this.newProperty();
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not delete the property.');
        this.loading.set(false);
      }
    });
  }
}
