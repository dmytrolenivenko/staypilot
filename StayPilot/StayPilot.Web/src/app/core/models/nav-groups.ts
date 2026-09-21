// The header's nav groups: what drives both the hover dropdown (a fast single pick) and each
// group's hub page (every option in that group, laid out as real page content). One list feeds
// both, so a link added here shows up in both places without being kept in sync by hand.

export interface NavLink {
  title: string;
  path: string;
  desc: string;
  planned?: boolean;
}

export interface NavGroup {
  title: string;
  // Where clicking the trigger itself (not a link inside the dropdown) navigates to.
  hubPath: string;
  links: NavLink[];
}

export const NAV_GROUPS: NavGroup[] = [
  {
    title: 'Listings',
    hubPath: '/listings',
    links: [
      { title: 'Browse', path: '/listing-browser', desc: 'Filter and sort every listing collected.' },
      { title: 'Top deals', path: '/listings/top-deals', desc: 'Asking the most below their own median.' },
      { title: 'Look up by id', path: '/listings/lookup', desc: 'One listing in full, with its latest snapshot.' },
      { title: 'Investment analysis', path: '/listings/investment-analysis', desc: 'Renovation cost, resale value, profit.' }
    ]
  },
  {
    title: 'Market areas',
    hubPath: '/market-areas',
    links: [
      { title: 'Market overview', path: '/market-overview', desc: 'One place, one typology, every price cut.' },
      { title: 'Leaderboard', path: '/market-areas/leaderboard', desc: 'Places ranked on median €/m².' },
      { title: 'What money buys', path: '/market-areas/budget', desc: 'The biggest typology a budget reaches.' },
      { title: 'Neighbour gaps', path: '/market-areas/neighbours', desc: 'Nearby places priced far apart.' },
      { title: 'Renovation upside', path: '/market-areas/renovation', desc: 'Where a fixer-upper is worth fixing.' },
      { title: 'Beach proximity', path: '/beach-proximity', desc: 'Price premium by distance to beach.', planned: true }
    ]
  },
  {
    title: 'Portfolio',
    hubPath: '/portfolio',
    links: [
      { title: 'My properties', path: '/my-properties', desc: 'Add, edit and delete what you own.' },
      { title: 'Valuation', path: '/valuation', desc: 'What it would be advertised at today.' }
    ]
  },
  {
    title: 'Tools',
    hubPath: '/tools',
    links: [
      { title: 'Feature impact', path: '/feature-impact', desc: 'What a garage, lift or sea view is worth.' },
      { title: 'Build cost', path: '/build-cost', desc: 'Shell, pool, fees and VAT, projected.' }
    ]
  }
];
