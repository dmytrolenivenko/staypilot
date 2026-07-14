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

The API today only exposes: `GET /api/MarketArea`, `POST /api/PropertyListing`,
`GET /api/PropertyListing/{id}`. So only three screens are real:

- **Market Areas** — full list (153 zones), client-side filter.
- **Listing Lookup** — look up one listing by id (there's no "list all" endpoint yet).
- **Add Listing** — create a listing with its initial price snapshot.
  Note: the API requires `latitude`/`longitude` on create (it uses them to
  compute nearest-beach distance) even though they're optional on the DTO —
  that's a service-level rule, not something visible in the contract.

Everything else described in the product pitch (Market Overview, Feature
Impact, Listing Browser, Price History, My Valuation, Beach Proximity) has a
nav entry and a page, but renders a "Not available yet" placeholder that
states exactly which backend endpoint is missing — see `app.routes.ts`. Build
those out as the corresponding API endpoints land.

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
