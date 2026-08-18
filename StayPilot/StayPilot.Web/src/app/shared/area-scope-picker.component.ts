import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnInit, Output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MarketAreaService } from '../core/services/market-area.service';

/** Where a screen has been narrowed to. Empty strings mean "not narrowed". */
export interface AreaScope {
  district: string;
  municipality: string;
}

export function emptyScope(): AreaScope {
  return { district: '', municipality: '' };
}

/**
 * Distrito then município, each loading the next as you pick it.
 *
 * Every market-areas screen used to answer for the whole country at once, which is one question
 * you ask once. The question you come back for is "and around where I am actually looking" — so
 * the same two dropdowns now sit on all of them, and they all mean the same thing.
 *
 * Freguesia is deliberately not offered: these screens rank places against each other, and
 * narrowing to a single freguesia leaves nothing to rank. Market Overview, which measures one
 * place rather than comparing several, has its own three-level picker for that reason.
 *
 *   <app-area-scope-picker [scope]="scope()" (scopeChange)="rescope($event)" />
 */
@Component({
  selector: 'app-area-scope-picker',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <label class="control">
      Distrito
      <select [ngModel]="scope.district" (ngModelChange)="pickDistrict($event)">
        <option value="">All of Portugal</option>
        @for (option of districts(); track option) {
          <option [value]="option">{{ option }}</option>
        }
      </select>
    </label>

    <label class="control">
      Município
      <select [ngModel]="scope.municipality" (ngModelChange)="pickMunicipality($event)" [disabled]="!scope.district">
        <option value="">All</option>
        @for (option of municipalities(); track option) {
          <option [value]="option">{{ option }}</option>
        }
      </select>
    </label>
  `
})
export class AreaScopePickerComponent implements OnInit {
  @Input({ required: true }) scope: AreaScope = emptyScope();

  @Output() scopeChange = new EventEmitter<AreaScope>();

  districts = signal<string[]>([]);
  municipalities = signal<string[]>([]);

  constructor(private readonly marketAreas: MarketAreaService) {}

  ngOnInit(): void {
    this.marketAreas.getOptions().subscribe({
      next: districts => this.districts.set(districts),
      error: () => this.districts.set([])
    });

    // Arriving with a district already chosen (a screen restoring its own state) still needs the
    // municípios underneath it, or the second dropdown opens empty on a scope that is not.
    if (this.scope.district) {
      this.loadMunicipalities(this.scope.district);
    }
  }

  pickDistrict(district: string): void {
    // A município only means something inside its own district, so changing the district drops it
    // rather than carrying a mismatched pair into the next request.
    this.municipalities.set([]);
    this.scopeChange.emit({ district, municipality: '' });

    if (district) {
      this.loadMunicipalities(district);
    }
  }

  pickMunicipality(municipality: string): void {
    this.scopeChange.emit({ district: this.scope.district, municipality });
  }

  private loadMunicipalities(district: string): void {
    this.marketAreas.getOptions(district).subscribe({
      next: municipalities => this.municipalities.set(municipalities),
      error: () => this.municipalities.set([])
    });
  }
}
