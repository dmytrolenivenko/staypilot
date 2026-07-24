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
      title: 'Listing Browser',
      description: 'Filterable table of all listings — city, typology, price, area, garage, beach distance.',
      link: '/listing-browser',
      live: true
    },
    {
      title: 'Price Snapshots',
      description: 'View the current price/status snapshot for a listing and record a new observation.',
      link: '/price-history',
      live: true
    },
    {
      title: 'My Properties',
      description: 'Add, edit and delete the apartments you own (the base for future valuation).',
      link: '/valuation',
      live: true
    },
    {
      title: 'Feature Impact',
      description: 'Price delta with/without a feature (garage, elevator, sea view, ...). Recalculable.',
      link: '/feature-impact',
      live: true
    },
    {
      title: 'Market Overview',
      description: 'Median/avg/min/max price and price/m² by area + typology. Needs a stats endpoint.',
      link: '/market-overview',
      live: false
    },
    {
      title: 'Beach Proximity',
      description: 'Price premium by distance-to-beach band. Needs a stats-by-band endpoint.',
      link: '/beach-proximity',
      live: false
    }
  ];
}
