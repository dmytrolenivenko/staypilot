import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

interface ModuleStatus {
  title: string;
  description: string;
  link: string;
  live: boolean;
}

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent {
  modules: ModuleStatus[] = [
    {
      title: 'Market Areas',
      description: 'Browse the 153 fixed Algarve market zones (district / municipality / town / zone).',
      link: '/market-areas',
      live: true
    },
    {
      title: 'Listing Lookup',
      description: 'Look up one property listing by ID, with its latest snapshot.',
      link: '/listings/lookup',
      live: true
    },
    {
      title: 'Add Listing',
      description: 'Manually add a property listing with its initial price snapshot.',
      link: '/listings/new',
      live: true
    },
    {
      title: 'Market Overview',
      description: 'Median/avg/min/max price and price/m² by area + typology, with a price distribution.',
      link: '/market-overview',
      live: false
    },
    {
      title: 'Feature Impact',
      description: 'Price delta with/without a feature (garage, elevator, sea view, ...).',
      link: '/feature-impact',
      live: false
    },
    {
      title: 'Listing Browser',
      description: 'Filterable table of all listings — city, typology, price, area, garage, beach distance.',
      link: '/listing-browser',
      live: true
    },
    {
      title: 'Price History',
      description: 'Price-over-time per listing or area, days-on-market, stale-and-overpriced flags.',
      link: '/price-history',
      live: false
    },
    {
      title: 'My Property Valuation',
      description: 'Conservative / realistic / optimistic value estimate plus ranked comparables.',
      link: '/valuation',
      live: false
    },
    {
      title: 'Beach Proximity',
      description: 'Price premium by distance-to-beach band.',
      link: '/beach-proximity',
      live: false
    }
  ];
}
