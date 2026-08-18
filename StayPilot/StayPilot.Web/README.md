# StayPilot.Web

Angular front end for StayPilot Comps. Plain CSS, no UI framework, no charting
library, standalone components, signals for local state. Talks to `StayPilot.Api`
through the dev-server proxy (`proxy.conf.json`) — no CORS setup needed.

## Run

```bash
# Terminal 1 — API (from the StayPilot/ folder)
dotnet run --project StayPilot.Api

# Terminal 2 — this app
npm install
ng serve
```

Open `http://localhost:4200`. Requests to `/api/*` are proxied to
`http://localhost:5278` (see `proxy.conf.json`) — if the API listens on a
different port, update that file.

## What's actually wired up

Important: the API routes as `api/[controller]/[action]`, so **the action name is
part of the URL** (and ASP.NET drops the `Async` suffix). Every service in
`core/services/` targets the full path — e.g. `GET /api/MarketArea/GetAll`,
`GET /api/PropertyListing/GetById/{id}`, `POST /api/PropertyListing/AddPropertyListing`.
Confirm paths against the API's Swagger (`/swagger/v1/swagger.json`) before adding calls.

Real screens (all backed by a live endpoint):

- **Market Overview** — one slice of the market (place + property type + typology): listing count,
  median/average/lowest/highest price, €/m² and floor area, a price distribution and a per-typology
  table. `GET /api/MarketOverview/GetMarketOverview`, plus `GET /api/MarketArea/GetOptions/options`
  for the place picker. Worked out live from the listings on every call — no recalculation step.
  This screen absorbed the old **Market Areas** list: the zone table is reference data, useful to
  filter by and pointless to read as a list, so it is no longer in the nav or the routes
  (`market-area-list.component.*` is still on disk but unreferenced).
- **Listing Lookup** — one listing by id. `GET /api/PropertyListing/GetById/{id}`.
- **Add Listing** — create a listing with its initial price snapshot.
  `POST /api/PropertyListing/AddPropertyListing`. Note: the API requires
  `latitude`/`longitude` on create (used to compute nearest-beach distance) even
  though they're optional on the DTO — a service-level rule, not in the contract.
- **Listing Browser** — filter/sort/page over listings. `POST /api/PropertyListing/FilterProperty`.
- **Price Snapshots** — view the current snapshot for a property and record a new one.
  `GET /api/ListingSnapshot/GetListingSnapshotByPropertyId/{id}` + `POST /api/ListingSnapshot/CreateListingSnapshot`.
- **My Properties** — CRUD over owned apartments. `GET/POST/PUT/DELETE /api/OwnedProperty/...`.
- **Feature Impact** — per-feature price premium %, with a Recalculate button.
  `GET /api/PremiumFeature/GetAllPremiumFeatures` + `POST /api/PremiumFeature/ReCalculatePremiumFeaturesValue`.

Still placeholders (no backend endpoint yet) — a `ComingSoonComponent` stating
which endpoint is missing (see `app.routes.ts`):

- **Beach Proximity** — needs a stats-by-distance-band endpoint.

- **Valuation** — what one of your properties would be advertised at today, with the comps and
  feature adjustments behind it. `POST /api/OwnedProperty/EstimateEvaluationsOwnedproperty`.
- **Leaderboard / What Money Buys / Neighbour Gaps / Renovation Upside** — four readings of the
  same server-side stats. `GET /api/MarketArea/GetLeaderboard`, `GetBudgetRanking`,
  `GetNeighbourGaps`, plus `POST /api/MarketArea/RecalculateMarketAreaStats`. These read a
  precomputed table, so they are only as fresh as the last recalculation — unlike Market Overview.
- **Build Cost** — client-side only, over static €/m² baselines in `core/models/build-cost.ts`.

## Structure

```
src/app/
  core/
    models/       DTOs + enums, mirrored 1:1 from StayPilot.Application.Contracts
    services/     One HttpClient service per controller, plus a session-only
                   RecentListingsService (there's no list endpoint to browse from)
  shared/
    page-header.component.ts  Title + subtitle + actions — the top of every screen
    explainer.component.ts    Collapsed "How to read this" block
  features/
    home/            Module dashboard, grouped the same way the sidebar is
    market-overview/  Real screen (also the place picker that replaced the area list)
    market-areas/     Real screens (leaderboard, budget, neighbours, renovation)
    listings/         Real screens (browser, lookup, create, + shared detail view)
    owned/ valuation/ snapshots/ premium-features/ build-cost/   Real screens
    coming-soon/      One reusable placeholder, driven by route `data`
  app.routes.ts     All routes + the "needs" text for each placeholder
  app.component.*   Sidebar nav + router-outlet shell
```

No NgRx, no AutoMapper-equivalent, no component library — mirrors the
minimalist style of the API project. Keep it that way unless a real need
shows up.

## Styling

`src/styles.css` is the whole design system: theme tokens (colour, spacing on a
4px grid, radius, type scale) for light and dark, plus every shared primitive —
`.page` / `.page-head`, `.card` (`card-head` / `card-title` / `card-body`),
`.btn` variants, `.field` / `.form-grid` / `.toolbar` / `.control` /
`.check-grid`, `.data-table` (+ `.sortable`, `.num`, `.rank`), `.pager`,
`.stat-grid`, `.badge` / `.tag`, `.note` / `.error` / `.success` / `.empty`,
and `.kv`.

**A component's own `.css` should only hold what is genuinely local to that
screen** — a field width, one bespoke bar chart. Before adding a rule, check it
isn't already a token or a primitive; the previous version of this app had ten
different `.data-table` overrides and six copies of the pager.

Every screen follows the same shape:

```html
<div class="page">
  <app-page-header title="…" sub="one line, no more" />
  <app-explainer>…background reading, collapsed…</app-explainer>
  <section class="card">
    <div class="card-head"><span class="card-title">…</span><span class="meta">…</span></div>
    <div class="card-body">…</div>
  </section>
</div>
```

Long explanatory prose goes in `<app-explainer>`, never between the title and
the controls — that is what made the analysis screens unreadable.
