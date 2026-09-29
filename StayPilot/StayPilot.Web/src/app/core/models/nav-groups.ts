// The app's navigation, in one place.
//
// Four destinations plus Home: what used to be fourteen screens behind four hover menus is four
// things the app is FOR, and the question you are asking within one of them rides along in ?ask=.
//
// The questions below are listed in the FOOTER only. The header deliberately stops at the four
// destinations: every screen that answers several questions already carries a segmented control
// for them under its own title, and a second copy in the header said the same thing twice with
// two active states that could disagree.
//
// Each question's `title` must match the label its screen gives that choice — those segmented
// options are the source of truth, and a footer that calls the same screen something else is the
// app disagreeing with itself.
//
// The old paths all still resolve — see app.routes.ts.

/** One of the questions a destination can answer, listed in the footer. */
export interface NavQuestion {
  /** The ?ask= value the screen reads. */
  ask: string;
  /** The label that screen's own segmented control gives this choice. */
  title: string;
}

export interface NavLink {
  title: string;
  path: string;
  desc: string;
  planned?: boolean;

  /** Which question to land on within a merged screen, e.g. { ask: 'deals' }. */
  query?: Record<string, string>;

  /** The questions this destination answers, listed under it in the footer. */
  questions?: NavQuestion[];
}

export const NAV_LINKS: NavLink[] = [
  {
    title: 'Places',
    path: '/places',
    desc: 'Compare towns and districts on what they actually ask per square metre.',
    questions: [
      { ask: 'value', title: 'Where value sits' },
      { ask: 'budget', title: 'What money buys' },
      { ask: 'neighbours', title: 'Neighbour gaps' },
      { ask: 'renovation', title: 'Renovation upside' }
    ]
  },
  {
    title: 'Listings',
    path: '/listings',
    desc: 'Every advert collected, and the ones asking furthest below their own local median.',
    questions: [
      { ask: 'browse', title: 'Browse' },
      { ask: 'deals', title: 'Top deals' }
    ]
  },
  {
    // "Portfolio" is a finance word for "the flats you own". The screen is about your own
    // properties, so it is now called that.
    title: 'My properties',
    path: '/portfolio',
    desc: 'What you own, and what each would be advertised at today.',
    questions: [
      { ask: 'properties', title: 'My properties' },
      { ask: 'valuation', title: 'Valuation' }
    ]
  },
  {
    // "Tools" said nothing about what was inside. These two both work a number out for you.
    title: 'Calculators',
    path: '/tools',
    desc: 'What a feature adds to a price, and what building one would cost.',
    questions: [
      { ask: 'features', title: 'Feature impact' },
      { ask: 'build', title: 'Build cost' }
    ]
  }
];
