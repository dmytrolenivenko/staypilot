import { inject } from '@angular/core';
import { RedirectFunction, Router, Routes } from '@angular/router';
import { MsalGuard } from '@azure/msal-angular';
import { HomeComponent } from './features/home/home.component';
import { PlacesComponent } from './features/places/places.component';
import { MarketOverviewComponent } from './features/market-overview/market-overview.component';
import { ListingsComponent } from './features/listings/listings.component';
import { ListingPageComponent } from './features/listings/listing-page.component';
import { InvestmentAnalysisComponent } from './features/listings/investment-analysis.component';
import { PortfolioComponent } from './features/portfolio/portfolio.component';
import { ToolsComponent } from './features/tools/tools.component';
import { ComingSoonComponent } from './features/coming-soon/coming-soon.component';
import { ComingSoonInfo } from './core/models/coming-soon-info';

function comingSoon(info: ComingSoonInfo) {
  return { info };
}

/**
 * An old screen's path, landing on the question that screen used to be.
 *
 * A plain string redirectTo cannot carry a query string, and the question is a query parameter
 * now — so these are built as UrlTrees instead. Returning one keeps the whole redirect inside
 * the route table rather than making each merged screen read a second source of truth.
 */
function toQuestion(path: string, ask: string): RedirectFunction {
  // Keeps whatever else the old link carried (?propertyId=, ?edit=) - dropping it opened the
  // right screen on the wrong property.
  return ({ queryParams }) => inject(Router).createUrlTree([path], { queryParams: { ...queryParams, ask } });
}

/**
 * The old analysis path. An owned property goes to the analysis screen, a listing to its own
 * page (which runs the same analysis embedded), anything else to Listings.
 */
function toAnalysis(): RedirectFunction {
  return ({ queryParams }) => {
    const router = inject(Router);

    if (queryParams['ownedId']) {
      return router.createUrlTree(['/portfolio/analysis'], { queryParams: { ownedId: queryParams['ownedId'] } });
    }

    if (queryParams['id']) {
      return router.createUrlTree(['/listings', queryParams['id']]);
    }

    return router.createUrlTree(['/listings']);
  };
}

// Browsing is open to everyone. Only Portfolio (your own properties and their valuation) needs
// sign-in: MsalGuard sits on those routes and redirects to the hosted login page (per
// MSAL_GUARD_CONFIG's InteractionType.Redirect). The old /my-properties and /valuation links
// redirect into /portfolio, so the guard covers them too. The AI narrative on a listing is gated
// on the API instead - anonymous visitors get the numbers and a sign-in prompt.
//
// Six destinations, where there used to be fourteen screens behind four hub pages. What was a
// navigation choice ("Leaderboard" or "What money buys") is now a control on the one screen
// that answers both, so the routes below are the six things this app is FOR, and the question
// being asked within one of them rides along in ?ask=.
//
// Every old path still resolves. They were in the menu for months and are in bookmarks and
// notes; a redirect costs a line and a 404 costs a user.
export const routes: Routes = [
  {
    path: '',
    children: [
      { path: '', component: HomeComponent },

      // --- The six ---------------------------------------------------------------
      { path: 'places', component: PlacesComponent },
      { path: 'places/overview', component: MarketOverviewComponent },
      { path: 'listings', component: ListingsComponent },
      { path: 'portfolio', component: PortfolioComponent, canActivate: [MsalGuard] },
      // The investment analysis (numbers + AI thesis) for one owned property, ?ownedId=<id>.
      // Its own screen: a listing gets it embedded on /listings/:id, an owned property has no
      // page of its own to embed it in.
      { path: 'portfolio/analysis', component: InvestmentAnalysisComponent, canActivate: [MsalGuard] },
      { path: 'tools', component: ToolsComponent },

      // --- Where the old menu items went -----------------------------------------
      // These sit above 'listings/:id' on purpose: below it, "lookup" would be read as an id.
      { path: 'listings/top-deals', redirectTo: toQuestion('/listings', 'deals'), pathMatch: 'full' },

      // Both of these opened by asking for a listing id, which /listings/:id now carries in the
      // URL. Sent to Listings rather than to an id-less listing page, because "which listing"
      // is a question Browse answers and an empty id box does not.
      { path: 'listings/lookup', redirectTo: '/listings', pathMatch: 'full' },
      { path: 'listings/investment-analysis', redirectTo: toAnalysis(), pathMatch: 'full' },

      // One listing in full — the page Browse and Top deals now link their rows to.
      { path: 'listings/:id', component: ListingPageComponent },

      // The four Places lenses.
      { path: 'market-areas', redirectTo: '/places', pathMatch: 'full' },
      { path: 'market-areas/leaderboard', redirectTo: toQuestion('/places', 'value'), pathMatch: 'full' },
      { path: 'market-areas/budget', redirectTo: toQuestion('/places', 'budget'), pathMatch: 'full' },
      { path: 'market-areas/neighbours', redirectTo: toQuestion('/places', 'neighbours'), pathMatch: 'full' },
      { path: 'market-areas/renovation', redirectTo: toQuestion('/places', 'renovation'), pathMatch: 'full' },
      { path: 'market-overview', redirectTo: '/places/overview', pathMatch: 'full' },

      { path: 'listing-browser', redirectTo: toQuestion('/listings', 'browse'), pathMatch: 'full' },

      // Portfolio and tools.
      { path: 'my-properties', redirectTo: toQuestion('/portfolio', 'properties'), pathMatch: 'full' },
      { path: 'valuation', redirectTo: toQuestion('/portfolio', 'valuation'), pathMatch: 'full' },
      { path: 'feature-impact', redirectTo: toQuestion('/tools', 'features'), pathMatch: 'full' },
      { path: 'build-cost', redirectTo: toQuestion('/tools', 'build'), pathMatch: 'full' },

      {
        path: 'beach-proximity',
        component: ComingSoonComponent,
        data: comingSoon({
          title: $localize`:@@shell.comingSoon.beach.title:Beach Proximity View`,
          description: $localize`:@@shell.comingSoon.beach.description:Price premium by distance-to-beach band, possibly on a simple map.`,
          needs: $localize`:@@shell.comingSoon.beach.needs:A listing list/filter + stats endpoint grouped by beach-distance band on the API.`
        })
      },
      { path: '**', redirectTo: '' }
    ]
  }
];
