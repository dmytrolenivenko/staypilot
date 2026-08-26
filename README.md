# StayPilot Comps

A personal real-estate comparable-listings tool for the Algarve (Portugal). It stores scraped
Idealista apartment listings with price history, and turns them into market analysis and
valuation for a small set of owned properties compared against those listings.

Single-developer, early-stage project. The design is intentionally minimal (no AutoMapper, no
repository abstraction was there for a long time, no per-user auth on read endpoints) — see
[`CLAUDE.md`](CLAUDE.md) before assuming a "missing" pattern is an oversight.

## What it does

- **Ingests listings** — scraped Idealista adverts (property + price snapshot), deduplicated by
  source URL, geocoded to the nearest beach and to a seeded Portuguese market-area hierarchy
  (District → Municipality → Town → Zone).
- **Market analysis** — median/average price, €/m², price distribution and per-typology
  breakdowns for any place + property-type + typology slice; a leaderboard, a "what money buys"
  budget ranking, neighbour-gap comparisons and a renovation-upside view, all built from a
  precomputed market-area stats table.
- **Feature pricing** — the average price premium of individual features (sea view, garage,
  pool, …), fitted once across the whole listing table.
- **Valuation** — prices every owned property in one pass against comparable listings, with the
  comps, feature adjustments, an area demand score and a 10-year growth projection.
- **Build cost estimator** — what building a house (plus pool, garage, garden, automation, solar…)
  costs today, anchored to 2021 rates and escalated live by Portugal's public INE construction
  cost index — no stored price list to go stale.

## Repository layout

```
StayPilot/
  StayPilot.Api             ASP.NET Core 10 Web API — controllers, Program.cs, Swagger
  StayPilot.Application     Request/response contracts + service interfaces + implementations
  StayPilot.Domain          Entities and enums, no dependencies
  StayPilot.Infrastructure  EF Core DbContext, migrations, entity configs, repositories
  StayPilot.UnitTests       xUnit
  StayPilot.Web             Angular 18 front end (own README — see below)
docs/                       Longer-form runbooks (e.g. rebuilding the MarketArea seed)
infra/                      Bicep — Azure App Service, SQL, Static Web App
tools/                      Ad-hoc/one-off tooling (currently empty of committed source)
adds/                       Local drop folder for scraper output (gitignored, not part of the repo)
```

The scraper that feeds this API (`AddProperty`) used to live in this repo and now has its own:
https://github.com/dmytrolenivenko/AddProperty.

## Tech stack

| Layer | Technology |
|---|---|
| API | ASP.NET Core 10 (Web API), Azure AD auth (`Microsoft.Identity.Web`) |
| Data | EF Core, SQL Server (LocalDB locally, Azure SQL serverless in the cloud) |
| Front end | Angular 18, standalone components, plain CSS (no UI framework, no NgRx) |
| Testing | xUnit |
| Hosting | Azure App Service (Linux) + Azure Static Web Apps, provisioned via Bicep |

## Getting started

Prerequisites: .NET 10 SDK, Node.js + npm, SQL Server LocalDB (installed with Visual Studio, or
standalone).

```bash
# Terminal 1 — API, from the StayPilot/ folder
dotnet build StayPilot.slnx
dotnet run --project StayPilot.Api
# Swagger UI at http://localhost:5278/swagger (see StayPilot.Api/Properties/launchSettings.json)

# Terminal 2 — Angular front end, from StayPilot/StayPilot.Web/
npm install
ng serve
# http://localhost:4200 — proxies /api/* to the API, see proxy.conf.json
```

```bash
# Tests
dotnet test StayPilot.slnx

# EF Core migrations (from StayPilot/)
dotnet ef migrations add <Name> --project StayPilot.Infrastructure --startup-project StayPilot.Api
dotnet ef database update --project StayPilot.Infrastructure --startup-project StayPilot.Api
```

CI builds+tests both projects on every PR (`.github/workflows/ci.yml`); merges to
`master`/`stable`/`release` auto-deploy to dev/qa/prod respectively. No lint tooling beyond the
C# compiler/analyzers. Full details in [`CLAUDE.md`](CLAUDE.md).

## API surface

Routes follow `api/{Controller}/{Action}` (the `Async` suffix is dropped). Every response shares
one shape — `{ ...data, errors?: [{ errorCode, errorMessage }] }` — and a negative `errorCode` is
stable to key on. Most read endpoints are open; a handful of write/recalculate actions require the
`Api.Write` role via Azure AD. Full inventory, error-code ranges and the authorization details are
in [`CLAUDE.md`](CLAUDE.md).

## Deployment

`infra/main.bicep` provisions, per environment (`dev`/`qa`/`prod`), an App Service (Linux, .NET 10,
Free tier) for the API, an Azure SQL serverless database (auto-pause, free-tier limit), and an
Azure Static Web App for the Angular build. Connection strings and CORS are wired from the Bicep
outputs into the App Service's app settings.

## Where things are documented

- [`CLAUDE.md`](CLAUDE.md) — the full technical reference for this repo (architecture, domain
  model, API conventions, known caveats) — read this before making non-trivial changes.
- [`StayPilot/StayPilot.Web/README.md`](StayPilot/StayPilot.Web/README.md) — front-end structure,
  screens and which backend endpoint each one calls.
- [`docs/idealista-market-areas-runbook.md`](docs/idealista-market-areas-runbook.md) — how the
  Portuguese market-area seed data was (re)built from Idealista.
- `c:\repos\EngineeringVault\vault\StayPilot\` (separate repo) — the living architecture doc,
  milestone plan and dated session logs. This is where day-to-day design decisions get recorded;
  check the latest session log there before making architectural changes.
