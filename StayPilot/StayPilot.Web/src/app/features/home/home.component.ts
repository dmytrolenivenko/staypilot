import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PageHeaderComponent } from '../../shared/page-header.component';

interface ModuleStatus {
  title: string;
  description: string;
  link: string;
  live: boolean;
}

interface ModuleSection {
  title: string;
  hint: string;
  modules: ModuleStatus[];
}

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, PageHeaderComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent {
  // Grouped the same way the sidebar is, so the front door and the nav agree.
  sections: ModuleSection[] = [
    {
      title: 'Listings',
      hint: 'The raw data — every advert collected, and what it is asking.',
      modules: [
        {
          title: 'Browse',
          description: 'Filter and sort every listing by area, typology, price, size, beach distance.',
          link: '/listing-browser',
          live: true
        },
        {
          title: 'Look up by id',
          description: 'One listing in full, with its latest price snapshot.',
          link: '/listings/lookup',
          live: true
        }
      ]
    },
    {
      title: 'Market areas',
      hint: 'Where to buy — the same Algarve zones asked five different questions.',
      modules: [
        {
          title: 'Market overview',
          description: 'One place, one typology: median / average / lowest / highest price, €/m², and where the listings sit.',
          link: '/market-overview',
          live: true
        },
        {
          title: 'Leaderboard',
          description: 'Places ranked on median €/m², with how many adverts back each one.',
          link: '/market-areas/leaderboard',
          live: true
        },
        {
          title: 'What money buys',
          description: 'Put in a budget, see the biggest typology it typically reaches in each place.',
          link: '/market-areas/budget',
          live: true
        },
        {
          title: 'Neighbour gaps',
          description: 'Pairs of nearby places priced far apart — a border you cannot see on a price list.',
          link: '/market-areas/neighbours',
          live: true
        },
        {
          title: 'Renovation upside',
          description: 'Where stock needing work is discounted more than the work costs.',
          link: '/market-areas/renovation',
          live: true
        }
      ]
    },
    {
      title: 'My portfolio',
      hint: 'Your own apartments, valued against the comparables above.',
      modules: [
        {
          title: 'My properties',
          description: 'Add, edit and delete the apartments you own.',
          link: '/my-properties',
          live: true
        },
        {
          title: 'Valuation',
          description: 'What one of your properties would be advertised at today, and why.',
          link: '/valuation',
          live: true
        }
      ]
    },
    {
      title: 'Tools',
      hint: 'Reference figures used by the screens above.',
      modules: [
        {
          title: 'Feature impact',
          description: 'What a garage, lift or sea view is worth as a % premium, with confidence ranges.',
          link: '/feature-impact',
          live: true
        },
        {
          title: 'Build cost',
          description: 'Project a build from scratch — shell, pool, garage, fees, VAT — and hold it against local asking prices.',
          link: '/build-cost',
          live: true
        }
      ]
    },
    {
      title: 'Planned',
      hint: 'Designed, waiting on an API endpoint.',
      modules: [
        {
          title: 'Beach proximity',
          description: 'Price premium by distance-to-beach band.',
          link: '/beach-proximity',
          live: false
        }
      ]
    }
  ];
}
