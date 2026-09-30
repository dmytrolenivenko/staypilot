import { Component, OnInit, computed, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { PageHeaderComponent } from '../../shared/page-header.component';
import { SegmentedComponent, SegmentedOption } from '../../shared/segmented.component';
import { PremiumFeaturesComponent } from '../premium-features/premium-features.component';
import { BuildCostComponent } from '../build-cost/build-cost.component';

/** Which tool is on screen. Also the `?ask=` value, so it can be linked to. */
export type ToolsLens = 'features' | 'build';

const LENSES: SegmentedOption<ToolsLens>[] = [
  // Labels share the footer's ids (nav-groups.ts) so the two can never disagree.
  { value: 'features', label: $localize`:@@core.nav.ask.features:Feature impact`, hint: $localize`:@@hub.tools.features.hint:What a garage, lift or sea view is worth` },
  { value: 'build', label: $localize`:@@core.nav.ask.build:Build cost`, hint: $localize`:@@hub.tools.build.hint:Shell, pool, fees and VAT, projected` }
];

const LENS_SUBS: Record<ToolsLens, string> = {
  features:
    $localize`:@@hub.tools.features.sub:What each feature is worth as a price premium, holding size, rooms, condition, beach distance and location still.`,
  build: $localize`:@@hub.tools.build.sub:What it would cost to build this house from scratch, and whether that beats buying one.`
};

/**
 * Tools — the two calculators.
 *
 * These are genuinely different questions from each other, unlike the four Places lenses; they
 * are together because each is one screen's worth of work and two menu entries for two
 * calculators is two more things to learn before you find either.
 */
@Component({
  selector: 'app-tools',
  standalone: true,
  imports: [PageHeaderComponent, SegmentedComponent, PremiumFeaturesComponent, BuildCostComponent],
  templateUrl: './tools.component.html'
})
export class ToolsComponent implements OnInit {
  readonly lenses = LENSES;

  lens = signal<ToolsLens>('features');

  sub = computed(() => LENS_SUBS[this.lens()]);

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    const asked = this.route.snapshot.queryParamMap.get('ask') as ToolsLens | null;

    if (asked && LENSES.some(lens => lens.value === asked)) {
      this.lens.set(asked);
    }
  }

  pickLens(lens: ToolsLens): void {
    this.lens.set(lens);

    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { ask: lens },
      replaceUrl: true
    });
  }
}
