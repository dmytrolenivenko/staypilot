# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

StayPilot Comps — a personal real-estate comparable-listings tool for the Algarve (Portugal).
It stores scraped Idealista apartment listings with price history, and will eventually support
market analysis and valuation of owned properties against comparables. This is an early-stage,
single-developer project (see commit history) — a lot of the design is intentionally minimal
(no AutoMapper, no repository abstraction over EF Core, no auth) and should stay that way unless
a milestone specifically calls for more.

This repo holds the Web API only (`StayPilot/`, `StayPilot.slnx`). The scraper that feeds the API
(`AddProperty`) used to live here as a second solution but now has its own repo:
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
StayPilot.Infrastructure  EF Core DbContext + migrations + entity configurations, AND the service implementations
StayPilot.UnitTests       xUnit
```

**Note the non-standard part:** service implementations (`PropertyListingService`, `MarketAreaService`,
etc.) live in `StayPilot.Infrastructure/Services/`, not in `StayPilot.Application`. `Application`
only holds the DTOs and interfaces. Follow this existing split rather than "fixing" it by moving
services — it's a deliberate simplification for a single-developer project, not an oversight.

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
