import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { InvestmentAnalysisService } from '../../core/services/investment-analysis.service';
import { RecentListingsService } from '../../core/services/recent-listings.service';
import { InvestmentAnalysisResponse } from '../../core/models/investment-analysis';
import { BuildCostOption } from '../../core/models/build-cost';
import { PageHeaderComponent } from '../../shared/page-header.component';

export type InvestmentAnalysisMode = 'listing' | 'owned';

// Investment Analysis — "is this worth investing in?", entirely from numbers this API
// already computed, for either a scraped listing or one of your own properties. Narrative
// is the only thing an AI ever writes here, and only to describe the numbers on screen,
// never to invent one of its own.
//
// Backed by InvestmentAnalysisController: GET api/InvestmentAnalysis/Analyze/{id} for a
// listing, GET api/InvestmentAnalysis/AnalyzeOwnedProperty/{id} for an owned property.
@Component({
  selector: 'app-investment-analysis',
  standalone: true,
  imports: [CommonModule, FormsModule, PageHeaderComponent],
  templateUrl: './investment-analysis.component.html',
  styleUrl: './investment-analysis.component.css'
})
export class InvestmentAnalysisComponent implements OnInit {
  mode = signal<InvestmentAnalysisMode>('listing');
  idInput = signal<number | null>(null);
  result = signal<InvestmentAnalysisResponse | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);

  // Prefilled from the calculated estimate whenever a fresh result arrives, so the user always
  // starts from our number but can freely replace it — real repair costs vary too much
  // (self-sourced materials, no labor hired) for one build-rate formula to fit everyone.
  renovationCostInput = signal<number | null>(null);

  constructor(
    private readonly service: InvestmentAnalysisService,
    private readonly route: ActivatedRoute,
    readonly recentListings: RecentListingsService
  ) {}

  ngOnInit(): void {
    const ownedIdParam = this.route.snapshot.queryParamMap.get('ownedId');
    if (ownedIdParam) {
      const id = Number(ownedIdParam);
      if (id > 0) {
        this.mode.set('owned');
        this.analyze(id);
        return;
      }
    }

    const idParam = this.route.snapshot.queryParamMap.get('id');
    if (idParam) {
      const id = Number(idParam);
      if (id > 0) {
        this.analyze(id);
      }
    }
  }

  setMode(mode: InvestmentAnalysisMode): void {
    if (this.mode() === mode) {
      return;
    }
    this.mode.set(mode);
    this.idInput.set(null);
    this.result.set(null);
    this.error.set(null);
  }

  analyze(id?: number, renovationCostOverride?: number): void {
    const targetId = id ?? this.idInput();
    if (!targetId || targetId <= 0) {
      this.error.set(this.mode() === 'owned' ? 'Enter a valid property id.' : 'Enter a valid listing id.');
      return;
    }
    this.idInput.set(targetId);

    this.loading.set(true);
    this.error.set(null);
    this.result.set(null);

    const call = this.mode() === 'owned'
      ? this.service.analyzeOwnedProperty(targetId, renovationCostOverride)
      : this.service.analyze(targetId, renovationCostOverride);

    call.subscribe({
      next: result => {
        this.result.set(result);
        this.renovationCostInput.set(result.estimatedRenovationCost);
        this.loading.set(false);
        if (this.mode() === 'listing') {
          this.recentListings.remember(targetId);
        }
      },
      error: err => {
        this.error.set(this.messageFor(err.status, targetId));
        this.loading.set(false);
      }
    });
  }

  /** Re-runs the analysis with the user's own renovation cost instead of the calculated one. */
  applyRenovationCost(): void {
    const targetId = this.idInput();
    const value = this.renovationCostInput();
    if (!targetId || value === null || value < 0) {
      return;
    }
    this.analyze(targetId, value);
  }

  /** Picks one of the fixed-price renovation scopes (Cosmetic / Full renovation / Full rebuild). */
  selectRenovationOption(option: BuildCostOption): void {
    const targetId = this.idInput();
    if (!targetId || option.cost === undefined) {
      return;
    }
    this.renovationCostInput.set(option.cost);
    this.analyze(targetId, option.cost);
  }

  /** Drops the user's override and goes back to the calculated renovation cost. */
  resetRenovationCost(): void {
    const targetId = this.idInput();
    if (!targetId) {
      return;
    }
    this.analyze(targetId);
  }

  private messageFor(status: number, id: number): string {
    const subject = this.mode() === 'owned' ? 'property' : 'listing';

    if (status === 404) {
      return `No ${subject} found with id ${id}.`;
    }

    if (status === 400) {
      return `This ${subject}'s town doesn't have enough move-in-ready comps to estimate a resale value against.`;
    }

    return 'Could not reach the API.';
  }
}
