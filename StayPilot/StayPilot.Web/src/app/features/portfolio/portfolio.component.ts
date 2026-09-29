import { Component, DestroyRef, OnInit, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { PageHeaderComponent } from '../../shared/page-header.component';
import { SegmentedComponent, SegmentedOption } from '../../shared/segmented.component';
import { OwnedPropertiesComponent } from '../owned/owned-properties.component';
import { ValuationComponent } from '../valuation/valuation.component';

/** Which half of the portfolio is on screen. Also the `?ask=` value, so it can be linked to. */
export type PortfolioLens = 'properties' | 'valuation';

const LENSES: SegmentedOption<PortfolioLens>[] = [
  { value: 'properties', label: 'My properties', hint: 'Add, edit and delete what you own' },
  { value: 'valuation', label: 'Valuation', hint: 'What each would be advertised at today' }
];

const LENS_SUBS: Record<PortfolioLens, string> = {
  properties: 'The apartments you own — the base a valuation compares against market listings.',
  valuation:
    'Every property you own, priced against the ASKING prices of the adverts around it. These are advertised prices, not sale prices — nothing here has been checked against a recorded transaction.'
};

/**
 * Portfolio — what you own, and what it is worth.
 *
 * Two screens over one set of objects: one to edit them, one to price them. Which meant the
 * answer to "what is this flat worth" lived somewhere other than the flat, and adding a
 * property and then seeing its valuation was a trip through the menu.
 */
@Component({
  selector: 'app-portfolio',
  standalone: true,
  imports: [PageHeaderComponent, SegmentedComponent, OwnedPropertiesComponent, ValuationComponent],
  templateUrl: './portfolio.component.html'
})
export class PortfolioComponent implements OnInit {
  readonly lenses = LENSES;

  lens = signal<PortfolioLens>('properties');

  sub = computed(() => LENS_SUBS[this.lens()]);

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly destroyRef: DestroyRef
  ) {}

  ngOnInit(): void {
    // Watched, not read once: Evaluate on a My properties row links to ?ask=valuation on this
    // same page, and Angular reuses the component rather than building a new one.
    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
      const asked = params.get('ask') as PortfolioLens | null;

      if (asked && LENSES.some(lens => lens.value === asked)) {
        this.lens.set(asked);
      }
    });
  }

  pickLens(lens: PortfolioLens): void {
    this.lens.set(lens);

    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { ask: lens },
      replaceUrl: true
    });
  }
}
