import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { OwnedPropertyService } from '../../core/services/owned-property.service';
import {
  OwnedPropertyAnalysisResponse,
  OwnedPropertyResponse
} from '../../core/models/owned-property';

// Valuation — pick one of your own properties, pick how far back to look for
// comparable sales, and get a price estimate (range + confidence + the comps
// and adjustments behind it, and how much you've gained since you bought it).
// Backed by OwnedPropertyController.EstimateEvaluationsOwnedproperty.
@Component({
  selector: 'app-valuation',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './valuation.component.html',
  styleUrl: './valuation.component.css'
})
export class ValuationComponent implements OnInit {
  properties = signal<OwnedPropertyResponse[]>([]);
  selectedId = signal<number | null>(null);
  months = signal(12);

  result = signal<OwnedPropertyAnalysisResponse | null>(null);

  loadingProps = signal(true);
  estimating = signal(false);
  error = signal<string | null>(null);

  // The property currently chosen in the dropdown (for the header line).
  selected = computed(() =>
    this.properties().find(p => p.id === this.selectedId()) ?? null
  );

  constructor(
    private readonly service: OwnedPropertyService,
    private readonly route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    // Arriving from "My Properties" with ?propertyId=<id> pre-selects that
    // property and runs the estimate straight away.
    const requestedId = Number(this.route.snapshot.queryParamMap.get('propertyId'));

    this.service.getAll().subscribe({
      next: rows => {
        this.properties.set(rows ?? []);
        if (rows?.length) {
          const preselect = rows.some(p => p.id === requestedId) ? requestedId : rows[0].id;
          this.selectedId.set(preselect);
        }
        this.loadingProps.set(false);

        // Only auto-estimate when we were sent here for a specific property.
        if (requestedId > 0 && this.properties().some(p => p.id === requestedId)) {
          this.estimate();
        }
      },
      error: () => {
        this.error.set('Could not load your properties from the API.');
        this.loadingProps.set(false);
      }
    });
  }

  estimate(): void {
    const id = this.selectedId();
    if (!id) {
      this.error.set('Pick a property first.');
      return;
    }

    this.estimating.set(true);
    this.error.set(null);
    this.result.set(null);

    this.service.estimate(id, this.months()).subscribe({
      next: res => {
        this.result.set(res);
        this.estimating.set(false);
      },
      error: () => {
        this.error.set('Could not estimate this property. It may have no comparable listings in the chosen window.');
        this.estimating.set(false);
      }
    });
  }

  // True when there's a real purchase to compare the estimate against.
  hasEquity = computed(() => (this.result()?.equity?.purchasePrice ?? 0) > 0);
}
