import { Routes } from '@angular/router';
import { MsalGuard } from '@azure/msal-angular';
import { HomeComponent } from './features/home/home.component';
import { MarketOverviewComponent } from './features/market-overview/market-overview.component';
import { MarketAreaLeaderboardComponent } from './features/market-areas/market-area-leaderboard.component';
import { MarketAreaBudgetComponent } from './features/market-areas/market-area-budget.component';
import { MarketAreaNeighboursComponent } from './features/market-areas/market-area-neighbours.component';
import { MarketAreaRenovationComponent } from './features/market-areas/market-area-renovation.component';
import { ListingLookupComponent } from './features/listings/listing-lookup.component';
import { ListingBrowserComponent } from './features/listings/listing-browser.component';
import { TopDealsComponent } from './features/listings/top-deals.component';
import { InvestmentAnalysisComponent } from './features/listings/investment-analysis.component';
import { PremiumFeaturesComponent } from './features/premium-features/premium-features.component';
import { OwnedPropertiesComponent } from './features/owned/owned-properties.component';
import { ValuationComponent } from './features/valuation/valuation.component';
import { BuildCostComponent } from './features/build-cost/build-cost.component';
import { ComingSoonComponent } from './features/coming-soon/coming-soon.component';
import { ComingSoonInfo } from './core/models/coming-soon-info';

function comingSoon(info: ComingSoonInfo) {
  return { info };
}

// Pathless parent wrapping every route so canActivateChild runs on every
// navigation, not just the first one - MsalGuard redirects to the hosted
// login page (per MSAL_GUARD_CONFIG's InteractionType.Redirect) whenever
// nobody is signed in yet. The whole app is behind sign-in now, not just
// My Properties.
export const routes: Routes = [
  {
    path: '',
    canActivateChild: [MsalGuard],
    children: [
      { path: '', component: HomeComponent },
      { path: 'market-overview', component: MarketOverviewComponent },
      { path: 'market-areas/leaderboard', component: MarketAreaLeaderboardComponent },
      { path: 'market-areas/budget', component: MarketAreaBudgetComponent },
      { path: 'market-areas/neighbours', component: MarketAreaNeighboursComponent },
      { path: 'market-areas/renovation', component: MarketAreaRenovationComponent },
      { path: 'listings/lookup', component: ListingLookupComponent },
      { path: 'listings/investment-analysis', component: InvestmentAnalysisComponent },
      { path: 'listings/top-deals', component: TopDealsComponent },
      { path: 'listing-browser', component: ListingBrowserComponent },
      { path: 'feature-impact', component: PremiumFeaturesComponent },
      { path: 'my-properties', component: OwnedPropertiesComponent },
      { path: 'valuation', component: ValuationComponent },
      { path: 'build-cost', component: BuildCostComponent },
      {
        path: 'beach-proximity',
        component: ComingSoonComponent,
        data: comingSoon({
          title: 'Beach Proximity View',
          description: 'Price premium by distance-to-beach band, possibly on a simple map.',
          needs: 'A listing list/filter + stats endpoint grouped by beach-distance band on the API.'
        })
      },
      { path: '**', redirectTo: '' }
    ]
  }
];
