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

- **Market Areas** — full list (153 zones), client-side filter. `GET /api/MarketArea/GetAll`.
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

- **Market Overview** — needs an area/typology stats endpoint.
- **Beach Proximity** — needs a stats-by-distance-band endpoint.

Not yet reachable from the UI: the API has an owned-property valuation
*estimate* at the service layer (`IOwnedPropertyService.EstimateOwnedPropertyValue`,
returning `OwnedPropertyAnalysisResponse`) but **no controller action exposes it**.
When a `GET /api/OwnedProperty/...valuation` action is added, wire it into the
My Properties screen.

## Structure

```
src/app/
  core/
    models/       DTOs + enums, mirrored 1:1 from StayPilot.Application.Contracts
    services/     One HttpClient service per controller, plus a session-only
                   RecentListingsService (there's no list endpoint to browse from)
  features/
    home/          Module status dashboard (what's live vs. planned)
    market-areas/   Real screen
    listings/       Real screens (lookup, create, + shared detail view)
    coming-soon/    One reusable placeholder, driven by route `data`
  app.routes.ts     All routes + the "needs" text for each placeholder
  app.component.*   Sidebar nav + router-outlet shell
```

No NgRx, no AutoMapper-equivalent, no component library — mirrors the
minimalist style of the API project. Keep it that way unless a real need
shows up.
