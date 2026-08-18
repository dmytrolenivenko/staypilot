import { CommonModule } from '@angular/common';
import { Component, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  BUILD_EXTRAS,
  BuildExtra,
  QUALITY_TIERS,
  QualityTier,
  Region,
  REGIONS
} from '../../core/models/build-cost';
import { PageHeaderComponent } from '../../shared/page-header.component';

// Build Cost — a "what would it cost to build this from scratch?" calculator.
// Pure client-side math over static €/m² baselines (see build-cost.ts). Reactive:
// the breakdown recomputes as you change any input, no button needed.
@Component({
  selector: 'app-build-cost',
  standalone: true,
  imports: [CommonModule, FormsModule, PageHeaderComponent],
  templateUrl: './build-cost.component.html',
  styleUrl: './build-cost.component.css'
})
export class BuildCostComponent {
  readonly tiers = QUALITY_TIERS;
  readonly regions = REGIONS;
  readonly extras = BUILD_EXTRAS;

  // Inputs.
  areaM2 = signal(150);
  tier = signal<QualityTier>(QUALITY_TIERS[1]); // Standard
  region = signal<Region>(REGIONS[0]); // Lisboa
  contingencyPct = signal(10);
  selectedExtras = signal<Set<string>>(new Set());

  // --- Breakdown (all derived) --------------------------------------------
  baseConstruction = computed(() => {
    const area = Math.max(0, this.areaM2() || 0);
    return this.tier().ratePerM2 * area * this.region().multiplier;
  });

  extrasTotal = computed(() =>
    this.extras
      .filter(e => this.selectedExtras().has(e.key))
      .reduce((sum, e) => sum + e.cost, 0)
  );

  subtotal = computed(() => this.baseConstruction() + this.extrasTotal());

  contingencyAmount = computed(() =>
    this.subtotal() * (Math.max(0, this.contingencyPct() || 0) / 100)
  );

  total = computed(() => this.subtotal() + this.contingencyAmount());

  // Effective all-in €/m² — handy to sanity-check against market rates.
  effectiveRatePerM2 = computed(() => {
    const area = this.areaM2() || 0;
    return area > 0 ? this.total() / area : 0;
  });

  isExtraSelected(key: string): boolean {
    return this.selectedExtras().has(key);
  }

  toggleExtra(key: string): void {
    const next = new Set(this.selectedExtras());
    next.has(key) ? next.delete(key) : next.add(key);
    this.selectedExtras.set(next);
  }
}
