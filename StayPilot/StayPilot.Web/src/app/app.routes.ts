import { Routes } from '@angular/router';
import { HomeComponent } from './features/home/home.component';
import { MarketAreaListComponent } from './features/market-areas/market-area-list.component';
import { MarketAreaLeaderboardComponent } from './features/market-areas/market-area-leaderboard.component';
import { MarketAreaBudgetComponent } from './features/market-areas/market-area-budget.component';
import { MarketAreaNeighboursComponent } from './features/market-areas/market-area-neighbours.component';
import { MarketAreaRenovationComponent } from './features/market-areas/market-area-renovation.component';
import { ListingLookupComponent } from './features/listings/listing-lookup.component';
import { ListingCreateComponent } from './features/listings/listing-create.component';
import { ListingBrowserComponent } from './features/listings/listing-browser.component';
import { SnapshotsComponent } from './features/snapshots/snapshots.component';
import { PremiumFeaturesComponent } from './features/premium-features/premium-features.component';
import { OwnedPropertiesComponent } from './features/owned/owned-properties.component';
import { ValuationComponent } from './features/valuation/valuation.component';
import { BuildCostComponent } from './features/build-cost/build-cost.component';
import { ComingSoonComponent } from './features/coming-soon/coming-soon.component';
import { ComingSoonInfo } from './core/models/coming-soon-info';

function comingSoon(info: ComingSoonInfo) {
  return { info };
}

export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'market-areas', component: MarketAreaListComponent },
  { path: 'market-areas/leaderboard', component: MarketAreaLeaderboardComponent },
  { path: 'market-areas/budget', component: MarketAreaBudgetComponent },
  { path: 'market-areas/neighbours', component: MarketAreaNeighboursComponent },
  { path: 'market-areas/renovation', component: MarketAreaRenovationComponent },
  { path: 'listings/lookup', component: ListingLookupComponent },
  { path: 'listings/new', component: ListingCreateComponent },
  { path: 'listing-browser', component: ListingBrowserComponent },
  {
    path: 'market-overview',
    component: ComingSoonComponent,
    data: comingSoon({
      title: 'Market Overview',
      description:
        'Pick an area + typology and see median/avg/min/max price, price/m², listing count, and a price distribution.',
      needs: 'A GET /api/analysis/market-overview endpoint (list/filter listings + stats) on the API.'
    })
  },
  { path: 'feature-impact', component: PremiumFeaturesComponent },
  { path: 'price-history', component: SnapshotsComponent },
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
];
