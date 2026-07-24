import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { PremiumFeatureService } from '../../core/services/premium-feature.service';
import { PremiumFeatureResponse } from '../../core/models/premium-feature';

// Feature Impact — how much each tracked feature (sea view, garage, ...) adds to price,
// as a percentage. Correlation, not causation. Values are precomputed on the server;
// "Recalculate" reruns the math over all current listings.
@Component({
  selector: 'app-premium-features',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './premium-features.component.html',
  styleUrl: './premium-features.component.css'
})
export class PremiumFeaturesComponent implements OnInit {
  features = signal<PremiumFeatureResponse[]>([]);
  loading = signal(true);
  recalculating = signal(false);
  error = signal<string | null>(null);

  constructor(private readonly service: PremiumFeatureService) {}

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
}
