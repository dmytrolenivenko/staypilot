import { Component, OnInit, computed, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { PageHeaderComponent } from '../../shared/page-header.component';
import { SegmentedComponent, SegmentedOption } from '../../shared/segmented.component';
import { ListingBrowserComponent } from './listing-browser.component';
import { TopDealsComponent } from './top-deals.component';

/** Which question the screen is answering. Also the `?ask=` value, so a view can be linked to. */
export type ListingsLens = 'browse' | 'deals';

const LENSES: SegmentedOption<ListingsLens>[] = [
  // Labels share the footer's ids (nav-groups.ts) so the two can never disagree.
  { value: 'browse', label: $localize`:@@core.nav.ask.browse:Browse`, hint: $localize`:@@hub.listings.browse.hint:Filter and sort every listing collected` },
  { value: 'deals', label: $localize`:@@core.nav.ask.deals:Top deals`, hint: $localize`:@@hub.listings.deals.hint:Asking the most below their own median` }
];

const LENS_SUBS: Record<ListingsLens, string> = {
  browse:
    $localize`:@@hub.listings.browse.sub:Set the filters, then Search. Sorting and paging happen on the server — leave a box empty to ignore that filter.`,
  deals:
    $localize`:@@hub.listings.deals.sub:Active listings asking the most below their own typology's median €/m² in the same town — under-priced, not merely cheap.`
};

/**
 * Listings — the adverts themselves, browsed or ranked.
 *
 * Top deals was its own destination in the menu, which made it look like a different kind of
 * thing from Browse. It is the same listings under one particular ranking, so it is a question
 * asked here rather than a place to navigate to. Both keep their own filters: they genuinely
 * differ (one filters the whole table, the other scopes a ranking), unlike the four Places
 * lenses, which were four copies of the same three controls.
 */
@Component({
  selector: 'app-listings',
  standalone: true,
  imports: [PageHeaderComponent, SegmentedComponent, ListingBrowserComponent, TopDealsComponent],
  templateUrl: './listings.component.html'
})
export class ListingsComponent implements OnInit {
  readonly lenses = LENSES;

  lens = signal<ListingsLens>('browse');

  sub = computed(() => LENS_SUBS[this.lens()]);

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    const asked = this.route.snapshot.queryParamMap.get('ask') as ListingsLens | null;

    if (asked && LENSES.some(lens => lens.value === asked)) {
      this.lens.set(asked);
    }
  }

  pickLens(lens: ListingsLens): void {
    this.lens.set(lens);

    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { ask: lens },
      replaceUrl: true
    });
  }
}
