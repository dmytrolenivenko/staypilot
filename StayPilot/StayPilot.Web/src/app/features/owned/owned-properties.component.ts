import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { OwnedPropertyService } from '../../core/services/owned-property.service';
import { OwnedPropertyRequest, OwnedPropertyResponse } from '../../core/models/owned-property';
import {
  PROPERTY_CONDITIONS,
  PROPERTY_TYPES,
  TYPOLOGIES
} from '../../core/models/enums';

// Columns the property list can be sorted by.
type SortField = 'id' | 'name' | 'propertyType' | 'typology' | 'areaM2' | 'purchasePrice';
type SortDirection = 'asc' | 'desc';

// My Properties — CRUD over the developer's own apartments (OwnedProperty).
// This screen only manages the data; the "valuation vs. comparables" estimate
// for these properties lives on the separate Valuation screen (/valuation).
@Component({
  selector: 'app-owned-properties',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './owned-properties.component.html',
  styleUrl: './owned-properties.component.css'
})
export class OwnedPropertiesComponent implements OnInit {
  readonly propertyTypes = PROPERTY_TYPES;
  readonly typologies = TYPOLOGIES;
  readonly conditions = PROPERTY_CONDITIONS;

  // All owned properties, shown as a list at the top of the screen.
  properties = signal<OwnedPropertyResponse[]>([]);
  listLoading = signal(true);

  // Sorting for the list.
  sortField = signal<SortField>('id');
  sortDirection = signal<SortDirection>('desc');

  // Ids ticked for multi-delete.
  selectedIds = signal<Set<number>>(new Set());

  // The list, sorted by the active column/direction (does not mutate the source).
  sortedProperties = computed(() => {
    const rows = [...this.properties()];
    const field = this.sortField();
    const dir = this.sortDirection() === 'asc' ? 1 : -1;

    rows.sort((a, b) => {
      let result: number;
      switch (field) {
        case 'name':
          result = a.name.localeCompare(b.name);
          break;
        case 'propertyType':
          result = a.propertyType.localeCompare(b.propertyType);
          break;
        case 'typology':
          result = a.typology.localeCompare(b.typology, undefined, { numeric: true });
          break;
        case 'areaM2':
          result = a.areaM2 - b.areaM2;
          break;
        case 'purchasePrice':
          result = (a.purchasePrice ?? 0) - (b.purchasePrice ?? 0);
          break;
        default:
          result = a.id - b.id;
      }
      return result * dir;
    });
    return rows;
  });

  // True only when every listed property is ticked (drives the header checkbox).
  allSelected = computed(() => {
    const rows = this.properties();
    return rows.length > 0 && rows.every(p => this.selectedIds().has(p.id));
  });

  idInput = signal<number | null>(null);
  current = signal<OwnedPropertyResponse | null>(null);

  loading = signal(false);
  error = signal<string | null>(null);
  message = signal<string | null>(null);

  // null = the form is creating a new property; a number = editing that id.
  editingId = signal<number | null>(null);

  form: OwnedPropertyRequest = this.emptyForm();

  constructor(private readonly service: OwnedPropertyService) {}

  ngOnInit(): void {
    this.loadAll();
  }

  // Load (or reload) the full list of owned properties for the top table.
  loadAll(): void {
    this.listLoading.set(true);
    this.service.getAll().subscribe({
      next: rows => {
        this.properties.set(rows ?? []);
        this.listLoading.set(false);
      },
      error: () => {
        this.error.set('Could not load your properties from the API.');
        this.listLoading.set(false);
      }
    });
  }

  // Load a property from the list straight into the form for editing.
  select(p: OwnedPropertyResponse): void {
    this.current.set(p);
    this.fillFormFrom(p);
    this.editingId.set(p.id);
    this.idInput.set(p.id);
    this.error.set(null);
    this.message.set(null);
  }

  // --- Sorting -------------------------------------------------------------
  // Click a header: same column flips direction, a new column starts ascending.
  sortBy(field: SortField): void {
    if (this.sortField() === field) {
      this.sortDirection.set(this.sortDirection() === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortField.set(field);
      this.sortDirection.set('asc');
    }
  }

  // --- Multi-select --------------------------------------------------------
  isSelected(id: number): boolean {
    return this.selectedIds().has(id);
  }

  toggleSelect(id: number): void {
    const next = new Set(this.selectedIds());
    next.has(id) ? next.delete(id) : next.add(id);
    this.selectedIds.set(next);
  }

  // Header checkbox: tick all when not all ticked, clear otherwise.
  toggleSelectAll(): void {
    this.selectedIds.set(
      this.allSelected() ? new Set() : new Set(this.properties().map(p => p.id))
    );
  }

  deleteSelected(): void {
    const ids = [...this.selectedIds()];
    if (ids.length === 0) {
      return;
    }
    if (!confirm(`Delete ${ids.length} propert${ids.length === 1 ? 'y' : 'ies'}? This cannot be undone.`)) {
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    this.message.set(null);

    forkJoin(ids.map(id => this.service.delete(id))).subscribe({
      next: () => {
        this.message.set(`Deleted ${ids.length} propert${ids.length === 1 ? 'y' : 'ies'}.`);
        // If the property open in the form was among them, reset the form.
        if (this.editingId() && ids.includes(this.editingId()!)) {
          this.newProperty();
        }
        this.selectedIds.set(new Set());
        this.loading.set(false);
        this.loadAll();
      },
      error: () => {
        this.error.set('Could not delete one or more properties. The list may be partly out of date — refresh.');
        this.loading.set(false);
        this.loadAll();
      }
    });
  }

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
        this.loadAll();
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
        this.loadAll();
      },
      error: () => {
        this.error.set('Could not delete the property.');
        this.loading.set(false);
      }
    });
  }
}
