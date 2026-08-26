import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { OwnedPropertyService } from '../../core/services/owned-property.service';
import { MarketAreaService } from '../../core/services/market-area.service';
import { MarketArea } from '../../core/models/market-area';
import { OwnedPropertyRequest, OwnedPropertyResponse } from '../../core/models/owned-property';
import {
  PROPERTY_CONDITION_OPTIONS,
  PROPERTY_TYPES,
  TYPOLOGIES
} from '../../core/models/enums';
import { PageHeaderComponent } from '../../shared/page-header.component';

// Columns the property list can be sorted by.
type SortField = 'id' | 'name' | 'propertyType' | 'typology' | 'areaM2' | 'purchasePrice';
type SortDirection = 'asc' | 'desc';

// My Properties — CRUD over the developer's own apartments (OwnedProperty).
// This screen only manages the data; the "valuation vs. comparables" estimate
// for these properties lives on the separate Valuation screen (/valuation).
@Component({
  selector: 'app-owned-properties',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, PageHeaderComponent],
  templateUrl: './owned-properties.component.html',
  styleUrl: './owned-properties.component.css'
})
export class OwnedPropertiesComponent implements OnInit {
  readonly propertyTypes = PROPERTY_TYPES;
  readonly typologies = TYPOLOGIES;
  readonly conditions = PROPERTY_CONDITION_OPTIONS;

  // Every seeded market area, loaded once. Drives the location cascade below
  // (District → Municipality → Town → Zone) so you pick from real places
  // instead of free-typing — same experience as the Listing Browser filters.
  private areas = signal<MarketArea[]>([]);

  // The cascade choices. Each level is filled from the level above it.
  districtOptions = signal<string[]>([]);
  municipalityOptions = signal<string[]>([]);
  townOptions = signal<string[]>([]);
  zoneOptions = signal<string[]>([]);

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

  constructor(
    private readonly service: OwnedPropertyService,
    private readonly marketAreas: MarketAreaService,
    private readonly route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.loadAll();
    this.loadAreas();
  }

  // Load every market area once, then seed the top-level (District) picker.
  private loadAreas(): void {
    this.marketAreas.getAll().subscribe({
      next: rows => {
        this.areas.set(rows ?? []);
        this.districtOptions.set(this.distinct(this.areas().map(a => a.district)));

        // Deep-link from Valuation ("Edit this property") — ?edit=<id> opens it.
        const editId = Number(this.route.snapshot.queryParamMap.get('edit'));
        if (editId > 0) {
          this.idInput.set(editId);
          this.lookup();
        }
      },
      error: () => {
        // Location pickers just stay empty; the rest of the screen still works.
        this.areas.set([]);
      }
    });
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

  // --- Location cascade (District → Municipality → Town → Zone) ------------
  // Sorted, de-duplicated, blanks removed.
  private distinct(values: (string | null | undefined)[]): string[] {
    return [...new Set(values.filter((v): v is string => !!v && v.trim() !== ''))].sort((a, b) =>
      a.localeCompare(b)
    );
  }

  private municipalitiesFor(district: string): string[] {
    return this.distinct(this.areas().filter(a => a.district === district).map(a => a.municipality));
  }

  private townsFor(municipality: string): string[] {
    return this.distinct(this.areas().filter(a => a.municipality === municipality).map(a => a.town));
  }

  private zonesFor(town: string): string[] {
    return this.distinct(this.areas().filter(a => a.town === town).map(a => a.zone));
  }

  // District picked → reset the levels below and reload municipalities.
  onDistrictChange(): void {
    this.form.municipality = '';
    this.form.town = '';
    this.form.zone = null;
    this.townOptions.set([]);
    this.zoneOptions.set([]);
    this.municipalityOptions.set(this.form.district ? this.municipalitiesFor(this.form.district) : []);
  }

  // Municipality picked → reset town/zone and reload towns.
  onMunicipalityChange(): void {
    this.form.town = '';
    this.form.zone = null;
    this.zoneOptions.set([]);
    this.townOptions.set(this.form.municipality ? this.townsFor(this.form.municipality) : []);
  }

  // Town picked → reset zone and reload zones.
  onTownChange(): void {
    this.form.zone = null;
    this.zoneOptions.set(this.form.town ? this.zonesFor(this.form.town) : []);
  }

  // Fill the four pickers (options + current values) from a resolved market area.
  // Used when editing an existing property, whose response only carries a marketAreaId.
  private populateLocationFrom(marketAreaId: number): void {
    const area = this.areas().find(a => a.id === marketAreaId);
    this.form.district = area?.district ?? '';
    this.form.municipality = area?.municipality ?? '';
    this.form.town = area?.town ?? '';
    this.form.zone = area?.zone ?? null;

    this.municipalityOptions.set(this.form.district ? this.municipalitiesFor(this.form.district) : []);
    this.townOptions.set(this.form.municipality ? this.townsFor(this.form.municipality) : []);
    this.zoneOptions.set(this.form.town ? this.zonesFor(this.form.town) : []);
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

  // The little arrow shown next to the active column header.
  arrow(field: SortField): string {
    if (this.sortField() !== field) {
      return '';
    }

    return this.sortDirection() === 'asc' ? ' ▲' : ' ▼';
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
      balconyCount: 0,
      floor: null,
      totalFloors: null,
      condition: 'Unknown',
      constructionYear: null,
      renovationYear: null,
      renovationInvestment: null,
      latitude: 0,
      longitude: 0,
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
      // Location is filled from the resolved market area below (the response
      // only carries marketAreaId, not the address parts).
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
      balconyCount: p.balconyCount ?? 0,
      floor: p.floor ?? null,
      totalFloors: p.totalFloors ?? null,
      condition: p.condition ?? 'Unknown',
      constructionYear: p.constructionYear ?? null,
      renovationYear: p.renovationYear ?? null,
      renovationInvestment: p.renovationInvestment ?? null,
      latitude: p.latitude,
      longitude: p.longitude,
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
    this.populateLocationFrom(p.marketAreaId);
  }

  // --- Create / update -----------------------------------------------------
  newProperty(): void {
    this.form = this.emptyForm();
    this.editingId.set(null);
    this.current.set(null);
    this.error.set(null);
    this.message.set(null);
    this.idInput.set(null);
    // Clear the location cascade back to just the top-level District picker.
    this.municipalityOptions.set([]);
    this.townOptions.set([]);
    this.zoneOptions.set([]);
  }

  // Every problem at once, not just the first: the form is long, and fixing one field only to
  // be told about the next one is three round trips through the same button.
  save(): void {
    const problems: string[] = [];

    if (!this.form.name.trim()) {
      problems.push('Name is required.');
    }

    if (!this.form.areaM2 || this.form.areaM2 < 1) {
      problems.push('Area (m²) must be at least 1.');
    }

    // District + Municipality are what the server matches a property to a market
    // area on, so both must be chosen or the save fails server-side.
    if (!this.form.district || !this.form.municipality) {
      problems.push('Pick at least a District and a Municipality.');
    }

    if (!this.form.latitude || !this.form.longitude) {
      problems.push('Latitude and Longitude are required.');
    }

    if (problems.length > 0) {
      this.error.set(problems.join(' '));

      // The banner lives at the top of the page and Save is at the bottom of a long form, so
      // without this the click looks like it did nothing at all.
      window.scrollTo({ top: 0, behavior: 'smooth' });
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
      error: err => {
        // Show what the API actually said. Two shapes arrive here, both under "errors":
        // ours is a list of { errorCode, errorMessage }, while ASP.NET's [Required]/[Range]
        // model validation sends a map of field -> messages.
        const problem = err.error;

        const apiMessage = Array.isArray(problem?.errors)
          ? problem.errors[0]?.errorMessage
          : null;

        const validationMessage =
          problem?.errors && !Array.isArray(problem.errors)
            ? (Object.values(problem.errors)[0] as string[])?.[0]
            : null;

        this.error.set(
          apiMessage ??
            validationMessage ??
            problem?.detail ??
            'Could not save the property. Check the required fields.'
        );
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
