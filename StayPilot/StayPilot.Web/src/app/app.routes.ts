import { Routes } from '@angular/router';
import { HomeComponent } from './features/home/home.component';
import { MarketAreaListComponent } from './features/market-areas/market-area-list.component';
import { ListingLookupComponent } from './features/listings/listing-lookup.component';
import { ListingCreateComponent } from './features/listings/listing-create.component';
import { ComingSoonComponent } from './features/coming-soon/coming-soon.component';
import { ComingSoonInfo } from './core/models/coming-soon-info';

function comingSoon(info: ComingSoonInfo) {
  return { info };
}

export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'market-areas', component: MarketAreaListComponent },
  { path: 'listings/lookup', component: ListingLookupComponent },
  { path: 'listings/new', component: ListingCreateComponent },
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
  {
    path: 'feature-impact',
    component: ComingSoonComponent,
    data: comingSoon({
      title: 'Feature Impact',
      description:
        'Pick a feature (garage, elevator, sea view, beach-under-500m, renovated...) and see the price delta with/without it. Correlation, not causation.',
      needs: 'A GET /api/analysis/features endpoint on the API.'
    })
  },
  {
    path: 'listing-browser',
    component: ComingSoonComponent,
    data: comingSoon({
      title: 'Listing Browser',
      description:
        'Filterable table of all listings — city, typology, price range, area range, garage, beach distance, sorted by price/m².',
      needs: 'A GET /api/PropertyListing (list + filter + paging) endpoint on the API — today only get-by-id exists.'
    })
  },
  {
    path: 'price-history',
    component: ComingSoonComponent,
    data: comingSoon({
      title: 'Price History / Trend',
      description:
        'Price-over-time per listing or area, days-on-market, and a flag for stale-and-overpriced listings.',
      needs: "A standalone POST/GET /api/ListingSnapshots endpoint — the service layer exists, the controller doesn't yet."
    })
  },
  {
    path: 'valuation',
    component: ComingSoonComponent,
    data: comingSoon({
      title: 'My Property Valuation',
      description:
        "Enter your apartment's specs, get conservative/realistic/optimistic value estimates plus ranked comparable listings.",
      needs: 'OwnedProperty DTOs/service/controller and a GET /api/owned-properties/{id}/valuation endpoint — none exist yet.'
    })
  },
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
