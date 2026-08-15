import { CommonModule } from '@angular/common';
import { Component, OnInit, signal, computed } from '@angular/core';
import { PremiumFeatureService } from '../../core/services/premium-feature.service';
import { PremiumFeatureResponse } from '../../core/models/premium-feature';

// We are going to sort these fields
type SortField = 'featureName' | 'premiumPercentage' | 'sampleSize';
type SortDirection = 'asc' | 'desc';

// Feature Impact — how much each tracked feature (sea view, garage, ...) adds to price,
// as a percentage. Correlation, not causation. Values are precomputed on the server;
// "Recalculate" reruns the math over all current listings.
@Component({
  selector: 'app-premium-features',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './premium-features.component.html',
  styleUrl: './premium-features.component.css',
})
export class PremiumFeaturesComponent implements OnInit {
  features = signal<PremiumFeatureResponse[]>([]);
  loading = signal(true);
  recalculating = signal(false);
  error = signal<string | null>(null);
  sortField = signal<SortField>('premiumPercentage');
  sortDirection = signal<SortDirection>('desc');
  sortedFeatures = computed(() => {
    const rows = [...this.features()];

    const field = this.sortField();
    const direction = this.sortDirection();

    rows.sort((a, b) => {
      // Features we cannot measure always sink to the bottom, whichever way the column is
      // sorted. Ranking them alongside real findings is what made a -0.2% noise reading look
      // like "furnishing makes it cheaper".
      if (a.isMeasurable !== b.isMeasurable) {
        return a.isMeasurable ? -1 : 1;
      }

      let result = 0;
      switch(field) {
      case "premiumPercentage":
        result = a.premiumPercent - b.premiumPercent;
        break;

      case "featureName":
        result = a.feature.localeCompare(b.feature);
        break;
      }

      if (direction === "desc") {
        return -result;
      }
      return result;
    })
    return rows;
  })

  constructor(private readonly service: PremiumFeatureService) {}

  // "HasSeaView" -> "Sea View", "CloseToBeach" -> "Close To Beach". One rule for every row:
  // the beach used to need a label of its own because its percentage meant something different
  // from all the others ("per halving of the distance"). Now it is a plain yes/no like a garage.
  label(feature: string): string {
    return feature.replace(/^(Has|Is)/, '').replace(/([a-z])([A-Z])/g, '$1 $2');
  }

  // The server explains any feature whose percentage is not a plain "if present" premium —
  // "per bathroom", "within 500m of the beach".
  basis(row: PremiumFeatureResponse): string {
    return row.basis ?? 'if present';
  }

  // Spells out the "N of M" column, which is easy to misread as a sample the row was fitted on
  // separately. It wasn't — every row comes out of the same comparison; what differs is how many
  // listings actually carried the feature AND had something to be compared against.
  evidenceHint(row: PremiumFeatureResponse): string {
    const listings = row.listingsWithFeature.toLocaleString();
    const total = row.sampleSize.toLocaleString();

    return `${listings} of the ${total} compared listings have this feature. Fewer means a wider confidence range.`;
  }

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.service.getAll().subscribe({
      next: rows => {
        this.features.set(rows);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load feature premiums from the API.');
        this.loading.set(false);
      }
    });
  }

  recalculate(): void {
    this.recalculating.set(true);
    this.error.set(null);
    this.service.recalculate().subscribe({
      next: () => {
        this.recalculating.set(false);
        this.reload();
      },
      error: () => {
        this.error.set('Could not recalculate feature premiums.');
        this.recalculating.set(false);
      }
    });
  }

  sortBy(field: SortField): void {
    if (this.sortField() === field) {
      this.sortDirection.set(
        this.sortDirection() === "asc" ? "desc" :"asc"
      );
    } else {
      this.sortField.set(field);
      this.sortDirection.set("desc");
    }
  }

}
