# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

StayPilot Comps — a personal real-estate comparable-listings tool for the Algarve (Portugal).
It stores scraped Idealista apartment listings with price history, and will eventually support
market analysis and valuation of owned properties against comparables. This is an early-stage,
single-developer project (see commit history) — a lot of the design is intentionally minimal
(no AutoMapper, no repository abstraction over EF Core) and should stay that way unless a
milestone specifically calls for more. There is Azure AD auth (`Microsoft.Identity.Web`), but it
only gates a handful of write/recalculate actions — see *API conventions* below; most read
endpoints have no `[Authorize]` at all.

This repo holds the Web API (`StayPilot/`, `StayPilot.slnx`) and, as of 2026-07-14, a minimal
Angular front end at `StayPilot/StayPilot.Web/` — see its own `README.md` for how to run it. It's
plain CSS/standalone-components, no UI framework, and only wires up screens the API actually
supports. Most of the product pitch is now real (Market Overview, Leaderboard, What Money Buys,
Neighbour Gaps, Renovation Upside, Listing Browser, Listing Lookup,
My Properties, Valuation, Feature Impact, Build Cost); Beach Proximity is the last placeholder
screen stating which backend endpoint it's waiting on — check `app.routes.ts` there before
assuming a module is real. The plain Market Areas list screen was folded into Market Overview
(the zone table is a filter, not something to read as a list), so `/market-areas` no longer
routes anywhere. **Add Listing and Price Snapshots were removed from the front end** (2026-08-18):
they are data-entry screens for the scraper's job, not something a customer of this tool should
see. The `ListingSnapshot` and `PropertyListing` write endpoints on the API are untouched — the
scraper still uses them; only the UI for hand-entry is gone, so do not re-add those screens.
The scraper that feeds the API (`AddProperty`) used to
live here as a second solution but now has its own repo:
https://github.com/dmytrolenivenko/AddProperty — clone it separately if you need to work on
scraping/ingestion.

The living architecture doc, milestone plan, and design rationale for this project are tracked
outside this repo, in the Obsidian vault at `c:\repos\EngineeringVault\vault\StayPilot\Project\StayPilot.md`
(plan) and `vault\StayPilot\Development\Session-NN_*.md` (dated session logs of what was actually
built/decided). Check the latest session log there for the most recent state and any open
decisions before making architectural changes — the vault, not this file, is where day-to-day
design decisions get recorded.

## Commands

All commands below assume the `StayPilot/` folder as working directory unless noted.

```bash
# Build / run the API
dotnet build StayPilot.slnx
dotnet run --project StayPilot.Api           # Swagger UI at the launch URL (see StayPilot.Api/Properties/launchSettings.json)

# Tests (xUnit)
dotnet test StayPilot.slnx
dotnet test --filter "FullyQualifiedName~ClassName.MethodName"   # run a single test

# EF Core migrations (run from StayPilot/ folder)
dotnet ef migrations add <Name> --project StayPilot.Infrastructure --startup-project StayPilot.Api
dotnet ef database update --project StayPilot.Infrastructure --startup-project StayPilot.Api
```

Local DB is SQL Server **LocalDB** (`(localdb)\MSSQLLocalDB`), configured in
`StayPilot.Api/appsettings.Development.json`. No Docker/Azure SQL setup exists yet.

There is no CI configured (`.github/workflows/` is empty) and no lint tooling beyond the C#
compiler/analyzers.

## Architecture

Layered solution, dependency direction `Api → Application → Infrastructure`, `Infrastructure → Application + Domain`, `Domain` has zero dependencies:

```
StayPilot.Api             Controllers (thin — no business logic), Program.cs composition root, Swagger
StayPilot.Application     Request/Response DTOs (Contracts/) and service interfaces (Interfaces/) only — no implementations
StayPilot.Domain          Entities and enums, plain C# classes, no NuGet deps
StayPilot.Infrastructure  EF Core DbContext + migrations + entity configurations + repositories
StayPilot.UnitTests       xUnit
```

Service implementations (`PropertyListingService`, `MarketAreaService`, `MarketOverviewService`,
etc.) live in `StayPilot.Application/Services/`, alongside the DTOs in `Contracts/` and the
interfaces in `Interfaces/`. The pure calculation is pulled out one level further into
`StayPilot.Application/Helpers/Calculators/` — a service loads, delegates the maths to a static
calculator, and maps; that split is what makes the calculators unit-testable without a DbContext.
`StayPilot.Infrastructure` holds the EF Core side and the repositories.

> **Note on history:** these services used to live in `StayPilot.Infrastructure/Services/` and this
> file used to describe that as deliberate. They have since moved to `Application`; if you find a
> doc or comment still pointing at `Infrastructure/Services`, it is stale.

Controller → Service (interface, injected) → Repository (interface, injected) → `StayPilotDbContext`.
Repositories live in `StayPilot.Infrastructure/Repositories/` and own all LINQ/EF query logic
(`.Where()`, `.Include()`, `.ToListAsync()`, etc.) against entities — they return entities, never
DTOs. Services hold business rules and do entity↔DTO mapping (see `MapToResponse`/`MapToEntity` in
`PropertyListingService`) — there is no AutoMapper. Register each repository in `Program.cs` DI
alongside its service.

> **Note on history:** earlier sessions deliberately skipped a repository layer as a simplification
> for a single-developer project. That was revisited in Session 10 — the user wants the repository
> pattern for its own sake (practice), and is retrofitting existing services to use it. Being
> retrofitted incrementally, service by service — check current session logs in the vault for which
> services have been converted before assuming this pattern is uniformly in place everywhere yet.

### API conventions

- **Routing:** `[Route("api/[controller]/[action]")]` on every controller — the action name is
  part of the URL, and ASP.NET's default action-name resolution drops an `Async` suffix (so
  `AddOwnedPropertyAsync` routes as `POST /api/OwnedProperty/AddOwnedProperty`). No route ever
  carries a version segment or a verb attribute beyond `[HttpGet]`/`[HttpPost]`.
- **Response shape:** every response class extends `ResponseBase`
  (`StayPilot.Application/Contracts/Response/Base/ResponseBase.cs`) — a nullable `Errors` list plus
  a computed `Succeeded`. Services call `response.AddError(ErrorCode.X, ...params)` instead of
  throwing; controllers call `this.ToActionResult(response)`
  (`StayPilot.Api/Extensions/ControllerExtensions.cs`), which maps `Succeeded` → 200,
  an error whose `ErrorCode` is `[NotFound]`-tagged → 404, anything else → 400. There is no
  try/catch in controllers or services for expected failures — only `ErrorCode`.
- **Error codes:** one `enum` (`ErrorCode` in the same file as `ResponseBase`), grouped in bands
  per domain (`-1..-99` general, `-100..-199` market areas, `-200..-299` property listings,
  `-300..-399` snapshots, `-400..-499` owned properties, `-500..-599` valuation/premium features).
  Each value carries a `[Display(Description = "...")]` template with `{0}`, `{1}`... placeholders,
  and optionally `[NotFound]` to mark it as a 404. **Append new codes at the end of their band;
  never renumber or reuse one** — callers (including the separate scraper repo) key on the number.
- **Authorization:** no controller has a class-level `[Authorize]`. Individual actions that write
  bulk data or trigger an expensive recalculation carry `[Authorize(Roles = "Api.Write")]`:
  `PropertyListingController.BulkAddPropertyListing`, `ListingSnapshotController.CreateListingSnapshotAsync`,
  `MarketAreaController.RecalculateMarketAreaStats`, `PremiumFeatureController.ReCalculatePremiumFeaturesValue`.
  Everything else — including all of `OwnedPropertyController` (My Properties CRUD) and every
  `GET` — is open. This is a deliberate current state for a personal, single-tenant deployment, not
  an oversight; tighten it before treating this as a multi-user product.
- **Controller inventory** (all under `StayPilot.Api/Controllers/`):

  | Controller | Actions |
  |---|---|
  | `PropertyListingController` | `BulkAddPropertyListing` (POST, `Api.Write`), `GetById`, `FilterPropertyAsync` (POST — browse/filter/page) |
  | `ListingSnapshotController` | `CreateListingSnapshotAsync` (POST, `Api.Write`), `GetListingSnapshotByPropertyIdAsync` |
  | `MarketAreaController` | `GetAll`, `GetOptions` (place picker), `GetLeaderboard`, `GetBudgetRanking`, `GetNeighbourGaps`, `RecalculateMarketAreaStats` (POST, `Api.Write`) |
  | `MarketOverviewController` | `GetMarketOverview` — live-computed slice by place + property type + typology, no recalculation step |
  | `OwnedPropertyController` | `GetOwnedPropertyAsync`, `GetAllOwnedPropertyAsync`, `AddOwnedPropertyAsync` (POST), `DeleteOwnedPropertyAsync`, `UpdateOwnedPropertyAsync`, `EstimateEvaluationsOwnedpropertyAsync` (single-property valuation), `ListValuationsOwnedpropertyAsync` (portfolio valuation — see below) |
  | `PremiumFeatureController` | `GetAllPremiumFeatures`, `ReCalculatePremiumFeaturesValue` (POST, `Api.Write`) |
  | `BuildCostController` | `GetBasis` — build-cost rates, see *Build cost* below |

### Domain model

- `MarketAreaStats` — one precomputed row per place per level, rebuilt wholesale by
  `RecalculateMarketAreaStats`. **Anything added to this entity is null/zero on existing rows until
  a recalculation runs**, and that endpoint is `[Authorize(Roles = "Api.Write")]` — so a new stats
  column cannot be backfilled from a dev machine without a token. Design the read side to degrade
  honestly in the meantime (the renovation confidence treats a missing spread as fully overlapping,
  i.e. "not measurable", rather than as a clean separation). Each row owns a collection of
  `MarketAreaTypologyStats` — the same median/€/m²/area numbers broken down per Typology (T0, T1,
  T2...), which is what makes "what does €300k buy me here" answerable instead of one blended
  median across studios and villas.
- `MarketArea` — a geographic zone (District/Municipality/Town/Zone). Seeded via EF migrations
  from real-world Algarve data; matched by exact normalized-string lookup (`GetMarketId` in
  `PropertyListingService`), not by ID, when a scraped listing doesn't supply a `MarketAreaId`.
- `PropertyListing` — one row per property, uniquely identified by `SourceUrl` (unique index;
  `AddPropertyListingAsync` is idempotent on `SourceUrl` — re-posting the same URL returns the
  existing entity rather than duplicating it). On create, the service also resolves the nearest
  `BeachMarker` via a plain in-memory Haversine distance calculation over all beach markers (no
  spatial SQL types) and stamps `NearestBeachName`/`DistanceToBeachMeters` onto the listing.
- `ListingSnapshot` — price history, one row per observation date, linked to a `PropertyListing`.
- `BeachMarker` — reference points used purely for nearest-beach distance calculation.
- `OwnedProperty` — the developer's own apartments, priced against comparables by the
  Valuation screen. That screen is a **portfolio** view: one call to
  `OwnedProperty/ListValuationsOwnedproperty` prices every property in one pass (the model is
  fitted once over the whole listing table — never loop the single-property estimate
  endpoint), and each row expands into comps, feature contributions, an area demand score
  and a growth projection.
- `HousePriceGrowth` — **seeded reference data**, one row per Portuguese district plus a
  national fallback (`District = ""`). The percentages are *planning assumptions*, not a
  measured index: nobody scraped INE into this table, and every screen that quotes them also
  prints their `Source` string, which says so. Correct them by editing
  `Persistence/Configurations/AllHousePriceGrowth.cs` and adding a migration — nothing
  computes off them at write time. They exist because this database holds a few months of
  adverts in one region, and a ten year projection built on that alone would call one season
  of the Algarve "Portugal". `GrowthForecastCalculator` blends the seeded rate with the trend
  measured from local snapshots, capping the local half at 50% of the blend however long the
  series gets, and returns both halves separately so a forecast can always be taken apart.
- Demand (`DemandCalculator`) scores a place out of 100 on exactly two inputs — median days
  on market and whether new supply is arriving faster than it was. Both guards matter: days
  on market switches itself off while the median approaches the collection window (early on,
  every listing looks young because collection is young), and supply needs two full 90 day
  windows of history. An unmeasurable place reports `IsMeasurable = false`, never the middle
  of the scale — "not measured" and "average" must not render the same.
- `PremiumFeature` — the average price premium of one feature (sea view, garage, …), fitted once
  across the whole listing table by `FeaturePremiumCalculator` and reused until the next
  `ReCalculatePremiumFeaturesValue` call. Carries a 95% confidence range (`LowerBoundPercent`/
  `UpperBoundPercent` — if these straddle zero, the headline `PremiumPercent` is not a finding) and
  `ListingsWithFeature`, which is *not* the same as `SampleSize`: the latter is the same for every
  feature, so on its own it made a rarely-measured feature look as solid as a common one. A couple
  of features (sea view, lift) also carry a conditional `MaximumPercent` + `MaximumBasis` — the
  best case under a stated condition — because their flat average hides a huge spread (a sea view
  bought inland vs. beachfront) that one number would otherwise misrepresent.
- Build cost (`BuildCostService`, `IIneRepository`) has **no entity or migration** — it is priced
  live from one external number (Portugal's INE construction cost index) rather than a stored
  price list. A 2021 €/m² anchor plus a set of dimensionless ratios (a concrete pool is 0.82× the
  house rate per m² of water, a garage bay 0.55×, …) are escalated by the latest published index;
  a few machine-like extras (lift, KNX bus, borehole) are anchored in 2021 euros and escalated by
  the materials-only half of the index instead of the blended one, because labour and materials
  drift apart over time. If INE is unreachable, everything degrades to plain 2021 prices with an
  empty `IndexPeriod` rather than a 500 — see `BuildCostService`'s class doc for the full rationale
  and the quotes it was cross-checked against. `IneRepository` exists purely because INE's site
  sends no CORS headers, so the browser cannot call it directly — this API is a proxy.

Known scaling caveats (documented, not yet fixed — see the vault's Session-08 log for full
reasoning before touching these): `GetMarketId` and the nearest-beach lookup both do a full
in-memory table scan with C#-side string normalization / Haversine math instead of pushing the
filter into SQL. Deliberately deferred until data volume or an observed slow response justifies
the work — don't "fix" this speculatively.

### Scraper (now a separate repo)

The `AddProperty` scraper that feeds this API lives in its own repo now:
https://github.com/dmytrolenivenko/AddProperty. It POSTs to this API's `PropertyListing`
endpoints and keeps its **own copies** of `PropertyListingRequest`/`ListingSnapshotRequest` and
the enums (`PropertyType`, `Typology`, `PropertyCondition`, `ListingStatus`) — it does not
reference `StayPilot.Application` or `StayPilot.Domain`. If you change a contract or enum values
in this API, update the matching copy in that repo too, and double check enum **numeric values**
match exactly (JSON serializes/deserializes enums by the API's `JsonStringEnumConverter`, but a
numeric mismatch between the two independent enum definitions would silently produce the wrong
value).
