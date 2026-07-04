# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

StayPilot Comps — a personal real-estate comparable-listings tool for the Algarve (Portugal).
It stores scraped Idealista apartment listings with price history, and will eventually support
market analysis and valuation of owned properties against comparables. This is an early-stage,
single-developer project (see commit history) — a lot of the design is intentionally minimal
(no AutoMapper, no repository abstraction over EF Core, no auth) and should stay that way unless
a milestone specifically calls for more.

The project has two independent .NET solutions in this repo:

- **`StayPilot/`** — the Web API (`StayPilot.slnx`). This is where almost all work happens.
- **`AddProperty/`** — a standalone console app + Playwright scraper (`AddProperty.slnx`) that
  feeds the API. Not part of the API solution; build/run it separately.

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

```bash
# AddProperty scraper (separate solution, run from AddProperty/AddProperty/)
dotnet run                      # upload existing JSON files in the vault's Idealista folder to the API
dotnet run scrape               # scrape 1 page via Playwright, save JSON, then upload
dotnet run scrape --pages 3     # scrape 3 pages, save, then upload
dotnet run scrape-only          # scrape without uploading
dotnet run scrape-only --pages 3
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
StayPilot.Infrastructure  EF Core DbContext + migrations + entity configurations, AND the service implementations
StayPilot.UnitTests       xUnit
```

**Note the non-standard part:** service implementations (`PropertyListingService`, `MarketAreaService`,
etc.) live in `StayPilot.Infrastructure/Services/`, not in `StayPilot.Application`. `Application`
only holds the DTOs and interfaces. Follow this existing split rather than "fixing" it by moving
services — it's a deliberate simplification for a single-developer project, not an oversight.

Controller → Service (interface, injected) → `StayPilotDbContext` directly (no repository layer).
Mapping between entities and DTOs is manual (see `MapToResponse`/`MapToEntity` in
`PropertyListingService`) — there is no AutoMapper.

### Domain model

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
- `OwnedProperty` — the developer's own apartments, for future valuation against comparables.

Known scaling caveats (documented, not yet fixed — see the vault's Session-08 log for full
reasoning before touching these): `GetMarketId` and the nearest-beach lookup both do a full
in-memory table scan with C#-side string normalization / Haversine math instead of pushing the
filter into SQL. Deliberately deferred until data volume or an observed slow response justifies
the work — don't "fix" this speculatively.

### Scraper (`AddProperty/`)

`IdealScraper.cs` drives a headed Playwright Chromium session against Idealista, human-paced
(random multi-second delays between actions, persistent browser profile for staying logged in,
manual CAPTCHA-solving prompts via `Console.ReadLine()`). It scrapes one search-results page at a
time, extracts each listing via in-page `EvaluateAsync` JS, applies exclusion rules
(`CheckExclusion` — non-apartments, timeshares, rentals, auctions, tenanted properties, etc.),
strips phone numbers/emails from descriptions (`StripContactInfo`), and writes results as JSON to
the vault (`vault/StayPilot/Project/Adds/Idealista/YYYY-MM-DD-pageNN.json`). `Program.cs` then
POSTs each listing in that JSON to the running API and writes response/error logs to
`vault/StayPilot/Project/Logs/Idealista/`.

The scraper has its **own copies** of `PropertyListingRequest`/`ListingSnapshotRequest` and the
enums (`PropertyType`, `Typology`, `PropertyCondition`, `ListingStatus`) defined inline in
`Program.cs` — it does not reference `StayPilot.Application` or `StayPilot.Domain`. If you change
a contract or enum values in the API, update the matching copy here too, and double check enum
**numeric values** match exactly (JSON serializes/deserializes enums by the API's
`JsonStringEnumConverter`, but a numeric mismatch between the two independent enum definitions
would silently produce the wrong value).

Because Idealista scraping this way is against their Terms of Service and this is explicitly a
low-volume, personal-use tool, don't parallelize it, remove the human-paced delays, or otherwise
make it faster/more aggressive.
